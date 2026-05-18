using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using InterfaceFactory;
using Newtonsoft.Json;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.StratuxGPS
{
    /// <summary>
    /// Plugin that connects to a Stratux device to obtain GPS ownship position
    /// and uses it to set the current location in Virtual Radar Server's web UI.
    /// </summary>
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebServer _WebServer;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        private System.Threading.Timer _PollTimer;
        private readonly object _PositionLock = new object();
        private double _Latitude;
        private double _Longitude;
        private double _GroundSpeedKnots;
        private double _TrueCourse;
        private double _AltitudeFeet;
        private int _FixQuality;
        private bool _HasPosition;
        private DateTime _LastPositionUtc;

        private readonly object _OwnshipLock = new object();
        private string _OwnshipIcao;
        private DateTime _OwnshipFetchedUtc;
        private int _PollCountSinceOwnshipFetch;

        /// <summary>
        /// Gets the last initialised instance of the plugin object.
        /// </summary>
        public static Plugin Singleton { get; private set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Id { get { return "VirtualRadarServer.Plugin.StratuxGPS"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string PluginFolder { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Name { get { return "Stratux GPS"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Version { get { return "1.0.0"; } }

        private string _Status;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Status
        {
            get { return _Status; }
            internal set {
                if(value != _Status) {
                    _Status = value;
                    OnStatusChanged(EventArgs.Empty);
                }
            }
        }

        private string _StatusDescription;
        /// <summary>
        /// See interface docs.
        /// </summary>
        public string StatusDescription
        {
            get { return _StatusDescription; }
            internal set {
                if(value != _StatusDescription) {
                    _StatusDescription = value;
                    OnStatusChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public bool HasOptions { get { return true; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public event EventHandler StatusChanged;

        /// <summary>
        /// Raises <see cref="StatusChanged"/>.
        /// </summary>
        protected virtual void OnStatusChanged(EventArgs args)
        {
            EventHelper.Raise(StatusChanged, this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void RegisterImplementations(IClassFactory classFactory)
        {
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Startup(PluginStartupParameters parameters)
        {
            Singleton = this;
            _Options = OptionsStorage.Load(this);
            _WebSite = parameters.WebSite;

            var autoConfigWebServer = Factory.ResolveSingleton<IAutoConfigWebServer>();
            _WebServer = autoConfigWebServer.WebServer;
            _WebServer.BeforeRequestReceived += WebServer_BeforeRequestReceived;

            var pages = new[] { "/desktop.html", "/mobile.html" };
            foreach(var page in pages) {
                _Injectors.Add(new HtmlContentInjector() {
                    Content     = () => BuildInjectScript(),
                    Element     = "BODY",
                    AtStart     = false,
                    PathAndFile = page,
                    Priority    = 100,
                });
            }

            ApplyOptions(_Options);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "StratuxGPSPluginOptions.html",
                    "Stratux GPS",
                    () => new WebAdmin.OptionsView(),
                    null
                ) {
                    Plugin = this,
                });
                webAdminViewManager.RegisterWebAdminViewFolder(PluginFolder, "Web");
            } catch {
                // WebAdmin plugin may not be installed
            }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void Shutdown()
        {
            StopPolling();

            if(_WebServer != null) {
                _WebServer.BeforeRequestReceived -= WebServer_BeforeRequestReceived;
            }

            if(_WebSite != null && _InjectorsActive) {
                foreach(var injector in _Injectors) {
                    _WebSite.RemoveHtmlContentInjector(injector);
                }
                _InjectorsActive = false;
            }
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public void ShowWinFormsOptionsUI()
        {
            using(var dialog = new WinForms.OptionsView()) {
                dialog.Options = OptionsStorage.Load(this);
                dialog.DetectedAddress = DetectStratuxAddress();

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    ApplyOptions(dialog.Options);
                }
            }
        }

        /// <summary>
        /// Applies the settings from the currently loaded options.
        /// </summary>
        internal void ApplyOptions(Options options)
        {
            _Options = options;
            StopPolling();

            if(!options.Enabled) {
                if(_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.RemoveHtmlContentInjector(injector);
                    }
                    _InjectorsActive = false;
                }
                lock(_PositionLock) {
                    _HasPosition = false;
                }
                Status = "Disabled";
                StatusDescription = null;
            } else {
                if(!_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.AddHtmlContentInjector(injector);
                    }
                    _InjectorsActive = true;
                }
                StartPolling();
                Status = "Enabled";
                StatusDescription = $"Polling {options.StratuxAddress}:{options.StratuxPort} every {options.PollIntervalMilliseconds}ms";
            }
        }

        /// <summary>
        /// Starts the background timer that polls the Stratux device.
        /// </summary>
        private void StartPolling()
        {
            _PollTimer = new System.Threading.Timer(PollStratux, null, 0, _Options.PollIntervalMilliseconds);
        }

        /// <summary>
        /// Stops the background polling timer.
        /// </summary>
        private void StopPolling()
        {
            if(_PollTimer != null) {
                _PollTimer.Dispose();
                _PollTimer = null;
            }
        }

        /// <summary>
        /// Timer callback that fetches GPS data from the Stratux device.
        /// </summary>
        private void PollStratux(object state)
        {
            try {
                var address = _Options.StratuxAddress;
                var port = _Options.StratuxPort;
                var url = $"http://{address}:{port}/getSituation";

                var request = WebRequest.CreateHttp(url);
                request.Method = "GET";
                request.Timeout = Math.Max(500, _Options.PollIntervalMilliseconds - 100);

                using(var response = (HttpWebResponse)request.GetResponse())
                using(var stream = response.GetResponseStream())
                using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                    var json = reader.ReadToEnd();
                    var situation = JsonConvert.DeserializeObject<StratuxSituation>(json);

                    if(situation != null && situation.GPSFixQuality > 0) {
                        lock(_PositionLock) {
                            _Latitude = situation.GPSLatitude;
                            _Longitude = situation.GPSLongitude;
                            _GroundSpeedKnots = situation.GPSGroundSpeed;
                            _TrueCourse = situation.GPSTrueCourse;
                            _AltitudeFeet = situation.GPSAltitudeMSL;
                            _FixQuality = situation.GPSFixQuality;
                            _HasPosition = true;
                            _LastPositionUtc = DateTime.UtcNow;
                        }
                        Status = "Enabled";
                        StatusDescription = $"GPS fix ({_FixQuality}): {_Latitude:F5}, {_Longitude:F5} @ {_GroundSpeedKnots:F0}kts";
                    } else {
                        lock(_PositionLock) {
                            _HasPosition = false;
                        }
                        Status = "Enabled";
                        StatusDescription = "No GPS fix";
                    }
                }
            } catch(Exception ex) {
                lock(_PositionLock) {
                    _HasPosition = false;
                }
                Status = "Enabled";
                StatusDescription = $"Error: {ex.Message}";
            }

            // Refresh OwnshipModeS roughly once a minute. The Stratux setting changes rarely,
            // but checking periodically picks up edits the pilot makes in the Stratux UI.
            var pollsPerMinute = Math.Max(1, 60000 / Math.Max(250, _Options.PollIntervalMilliseconds));
            if(_OwnshipIcao == null || ++_PollCountSinceOwnshipFetch >= pollsPerMinute) {
                _PollCountSinceOwnshipFetch = 0;
                FetchOwnshipIcao();
            }
        }

        /// <summary>
        /// Fetches the configured OwnshipModeS hex code from the Stratux device's /getSettings endpoint
        /// and caches it. Stratux exposes it as a 6-character hex string (e.g. "F00000").
        /// Reference: cyoung/stratux notes/app-vendor-integration.md
        /// </summary>
        private void FetchOwnshipIcao()
        {
            try {
                var url = $"http://{_Options.StratuxAddress}:{_Options.StratuxPort}/getSettings";
                var request = WebRequest.CreateHttp(url);
                request.Method = "GET";
                request.Timeout = Math.Max(500, _Options.PollIntervalMilliseconds - 100);

                using(var response = (HttpWebResponse)request.GetResponse())
                using(var stream = response.GetResponseStream())
                using(var reader = new StreamReader(stream, Encoding.UTF8)) {
                    var json = reader.ReadToEnd();
                    var settings = JsonConvert.DeserializeObject<StratuxSettings>(json);
                    var icao = settings?.OwnshipModeS?.Trim();
                    if(!string.IsNullOrEmpty(icao) && icao != "000000") {
                        lock(_OwnshipLock) {
                            _OwnshipIcao = icao.ToUpperInvariant();
                            _OwnshipFetchedUtc = DateTime.UtcNow;
                        }
                    }
                }
            } catch {
                // Transient failure — keep the previously cached value (if any).
            }
        }

        /// <summary>
        /// Returns the ownship Mode-S / ICAO hex code last read from the Stratux device, or null
        /// if it has not yet been successfully fetched.
        /// </summary>
        public string GetOwnshipIcao()
        {
            lock(_OwnshipLock) {
                return _OwnshipIcao;
            }
        }

        /// <summary>
        /// Returns a snapshot of the most recent GPS position polled from the Stratux device.
        /// </summary>
        public PositionSnapshot GetCurrentPosition()
        {
            lock(_PositionLock) {
                return new PositionSnapshot {
                    HasPosition      = _HasPosition,
                    Latitude         = _Latitude,
                    Longitude        = _Longitude,
                    AltitudeFeet     = _AltitudeFeet,
                    GroundSpeedKnots = _GroundSpeedKnots,
                    TrueCourse       = _TrueCourse,
                    FixQuality       = _FixQuality,
                    AgeSeconds       = _HasPosition ? (DateTime.UtcNow - _LastPositionUtc).TotalSeconds : 0,
                };
            }
        }

        /// <summary>
        /// Intercepts web requests for our custom endpoint.
        /// </summary>
        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;

            if(string.Equals(args.PathAndFile, "/Stratux/Location.json", StringComparison.OrdinalIgnoreCase)) {
                HandleLocationRequest(args);
            } else if(string.Equals(args.PathAndFile, "/Stratux/Ownship.json", StringComparison.OrdinalIgnoreCase)) {
                HandleOwnshipRequest(args);
            }
        }

        /// <summary>
        /// Returns the auto-detected ownship Mode-S / ICAO hex code as JSON for other plugins
        /// or scripts that want to consume it.
        /// </summary>
        private void HandleOwnshipRequest(RequestReceivedEventArgs args)
        {
            string icao;
            lock(_OwnshipLock) {
                icao = _OwnshipIcao;
            }
            var json = string.IsNullOrEmpty(icao)
                ? "{\"has\":false}"
                : JsonConvert.SerializeObject(new { has = true, icao = icao });

            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        /// <summary>
        /// Returns the current GPS position as JSON.
        /// </summary>
        private void HandleLocationRequest(RequestReceivedEventArgs args)
        {
            string json;
            lock(_PositionLock) {
                if(_HasPosition) {
                    json = JsonConvert.SerializeObject(new {
                        has  = true,
                        lat  = _Latitude,
                        lng  = _Longitude,
                        spd  = _GroundSpeedKnots,
                        trk  = _TrueCourse,
                        alt  = _AltitudeFeet,
                        fix  = _FixQuality,
                        age  = (DateTime.UtcNow - _LastPositionUtc).TotalSeconds,
                    });
                } else {
                    json = "{\"has\":false}";
                }
            }

            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        /// <summary>
        /// Attempts to detect the Stratux device's IP address by finding
        /// a network interface on the 192.168.10.x subnet.
        /// </summary>
        internal static string DetectStratuxAddress()
        {
            try {
                foreach(var iface in NetworkInterface.GetAllNetworkInterfaces()) {
                    if(iface.OperationalStatus != OperationalStatus.Up) continue;
                    var ipProps = iface.GetIPProperties();
                    foreach(var addr in ipProps.UnicastAddresses) {
                        if(addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var bytes = addr.Address.GetAddressBytes();
                        // Stratux default subnet is 192.168.10.x
                        if(bytes[0] == 192 && bytes[1] == 168 && bytes[2] == 10) {
                            return "192.168.10.1";
                        }
                    }
                }
            } catch {
                // Can't enumerate interfaces, fall back
            }
            return null;
        }

        /// <summary>
        /// Builds the inline JavaScript that polls for GPS position
        /// and updates VRS's current location.
        /// </summary>
        private string BuildInjectScript()
        {
            var interval = _Options.PollIntervalMilliseconds;

            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.currentLocation) return;");
            sb.AppendLine();
            sb.AppendLine("  var _pollInterval = " + interval.ToString(CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  var _hasGps = false;");
            sb.AppendLine();
            sb.AppendLine("  function pollLocation() {");
            sb.AppendLine("    try {");
            sb.AppendLine("      var xhr = new XMLHttpRequest();");
            sb.AppendLine("      xhr.open('GET', 'Stratux/Location.json', true);");
            sb.AppendLine("      xhr.timeout = _pollInterval;");
            sb.AppendLine("      xhr.onreadystatechange = function() {");
            sb.AppendLine("        if(xhr.readyState === 4 && xhr.status === 200) {");
            sb.AppendLine("          try {");
            sb.AppendLine("            var data = JSON.parse(xhr.responseText);");
            sb.AppendLine("            if(data.has && data.lat !== undefined && data.lng !== undefined) {");
            sb.AppendLine("              VRS.currentLocation.setCurrentLocation({ lat: data.lat, lng: data.lng });");
            sb.AppendLine("              _hasGps = true;");
            sb.AppendLine("            } else {");
            sb.AppendLine("              _hasGps = false;");
            sb.AppendLine("            }");
            sb.AppendLine("          } catch(e) { _hasGps = false; }");
            sb.AppendLine("        }");
            sb.AppendLine("      };");
            sb.AppendLine("      xhr.send();");
            sb.AppendLine("    } catch(e) { _hasGps = false; }");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  setInterval(pollLocation, _pollInterval);");
            sb.AppendLine("  pollLocation();");
            sb.AppendLine();
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Snapshot of the most recent GPS position the plugin has polled from the Stratux device.
    /// </summary>
    public class PositionSnapshot
    {
        public bool HasPosition { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double AltitudeFeet { get; set; }
        public double GroundSpeedKnots { get; set; }
        public double TrueCourse { get; set; }
        public int FixQuality { get; set; }
        public double AgeSeconds { get; set; }
    }

    /// <summary>
    /// Subset of the Stratux /getSettings JSON response — we only care about OwnshipModeS,
    /// the user-configured ICAO hex of their transponder.
    /// </summary>
    internal class StratuxSettings
    {
        public string OwnshipModeS { get; set; }
    }

    /// <summary>
    /// Represents the JSON response from the Stratux /getSituation endpoint.
    /// </summary>
    internal class StratuxSituation
    {
        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double GPSAltitudeMSL { get; set; }
        public double GPSGroundSpeed { get; set; }
        public double GPSTrueCourse { get; set; }
        public double GPSVerticalSpeed { get; set; }
        public int GPSFixQuality { get; set; }
        public double GPSHorizontalAccuracy { get; set; }
        public int GPSSatellites { get; set; }
        public int GPSSatellitesTracked { get; set; }
        public int GPSSatellitesSeen { get; set; }
        public int GPSNACp { get; set; }
    }
}
