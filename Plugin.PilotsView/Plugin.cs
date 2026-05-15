using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.PilotsView
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebSite _WebSite;
        private IWebServer _WebServer;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        // Background-sampled position queue, one per aircraft. The sampler timer appends
        // a Sample every second; each Camera.kml fetch dequeues the oldest one. Since
        // consecutive served positions are always different real samples (aircraft moved),
        // the camera always has somewhere new to fly — onStop keeps firing, no lockup —
        // and the animation is always between two known-real points, not predicted.
        private class Sample { public double Lat; public double Lng; public double AltFeet; public double Heading; }
        private class PositionQueue {
            public readonly object Lock = new object();
            public readonly LinkedList<Sample> Samples = new LinkedList<Sample>();
            public DateTime LastAccessed = DateTime.UtcNow;
        }
        private readonly ConcurrentDictionary<string, PositionQueue> _Queues =
            new ConcurrentDictionary<string, PositionQueue>(StringComparer.OrdinalIgnoreCase);
        private System.Threading.Timer _Sampler;
        private const int SamplerPeriodMs = 1000;
        private const int MaxQueueDepth = 10;


        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.PilotsView"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Pilot's View"; } }
        public string Version { get { return "1.0.0"; } }

        private string _Status;
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

        public bool HasOptions { get { return true; } }

        public event EventHandler StatusChanged;

        protected virtual void OnStatusChanged(EventArgs args)
        {
            EventHelper.Raise(StatusChanged, this, args);
        }

        public void RegisterImplementations(InterfaceFactory.IClassFactory classFactory)
        {
        }

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

            _Sampler = new System.Threading.Timer(SampleTick, null, SamplerPeriodMs, SamplerPeriodMs);
        }

        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "PilotsViewPluginOptions.html",
                    "Pilot's View",
                    () => new WebAdmin.OptionsView(),
                    null
                ) {
                    Plugin = this,
                });
                webAdminViewManager.RegisterWebAdminViewFolder(PluginFolder, "Web");
            } catch {
            }
        }

        public void Shutdown()
        {
            _Sampler?.Dispose();
            _Sampler = null;

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

        private void SampleTick(object state)
        {
            try {
                var now = DateTime.UtcNow;
                foreach(var icao in _Queues.Keys) {
                    PositionQueue q;
                    if(!_Queues.TryGetValue(icao, out q)) continue;

                    if((now - q.LastAccessed).TotalMinutes > 2) {
                        _Queues.TryRemove(icao, out q);
                        continue;
                    }

                    double? lat, lng, alt;
                    float? track;
                    string _;
                    TryGetAircraftPosition(icao, out lat, out lng, out alt, out track, out _);
                    if(!lat.HasValue || !lng.HasValue) continue;

                    var sample = new Sample {
                        Lat = lat.Value,
                        Lng = lng.Value,
                        AltFeet = alt ?? 1000,
                        Heading = track ?? 0,
                    };

                    lock(q.Lock) {
                        // Skip stationary aircraft — identical samples would leave GE's camera
                        // motionless, and onStop needs motion-then-stop events to keep firing.
                        if(q.Samples.Last != null) {
                            var last = q.Samples.Last.Value;
                            if(Math.Abs(last.Lat - sample.Lat) < 1e-7
                                && Math.Abs(last.Lng - sample.Lng) < 1e-7) continue;
                        }
                        q.Samples.AddLast(sample);
                        while(q.Samples.Count > MaxQueueDepth) q.Samples.RemoveFirst();
                    }
                }
            } catch(Exception ex) {
                StatusDescription = "Sampler error: " + ex.Message;
            }
        }

        public void ShowWinFormsOptionsUI()
        {
            using(var dialog = new WinForms.OptionsView()) {
                dialog.Options = OptionsStorage.Load(this);

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    ApplyOptions(dialog.Options);
                }
            }
        }

        internal void ApplyOptions(Options options)
        {
            _Options = options;

            if(!options.Enabled) {
                if(_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.RemoveHtmlContentInjector(injector);
                    }
                    _InjectorsActive = false;
                }
                Status = "Disabled";
            } else {
                if(!_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.AddHtmlContentInjector(injector);
                    }
                    _InjectorsActive = true;
                }
                Status = "Enabled";
            }
        }

        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;
            if(!(_Options?.Enabled ?? false)) return;

            try {
                var path = args.PathAndFile ?? "";

                if(string.Equals(path, "/PilotsView/View.kml", StringComparison.OrdinalIgnoreCase)) {
                    HandleViewKmlRequest(args);
                } else if(string.Equals(path, "/PilotsView/Camera.kml", StringComparison.OrdinalIgnoreCase)) {
                    HandleCameraKmlRequest(args);
                } else if(string.Equals(path, "/PilotsView/Launch.html", StringComparison.OrdinalIgnoreCase)) {
                    HandleLaunchPageRequest(args);
                } else if(string.Equals(path, "/PilotsView/Hud.png", StringComparison.OrdinalIgnoreCase)) {
                    HandleHudImageRequest(args);
                } else if(string.Equals(path, "/PilotsView/Web.html", StringComparison.OrdinalIgnoreCase)) {
                    HandleWebPageRequest(args);
                } else if(string.Equals(path, "/PilotsView/Position.json", StringComparison.OrdinalIgnoreCase)) {
                    HandlePositionJsonRequest(args);
                }
            } catch(Exception ex) {
                StatusDescription = "Request error: " + ex.Message;
            }
        }

        private static string GetQueryParam(RequestReceivedEventArgs args, string name)
        {
            try {
                var qs = args.QueryString;
                if(qs != null) {
                    var val = qs[name];
                    if(val != null) return val;
                }
            } catch { }

            try {
                var rawUrl = args.Request.RawUrl ?? "";
                var qIdx = rawUrl.IndexOf('?');
                if(qIdx >= 0) {
                    var query = rawUrl.Substring(qIdx + 1);
                    foreach(var pair in query.Split('&')) {
                        var eqIdx = pair.IndexOf('=');
                        if(eqIdx > 0) {
                            var key = Uri.UnescapeDataString(pair.Substring(0, eqIdx));
                            if(string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) {
                                return Uri.UnescapeDataString(pair.Substring(eqIdx + 1));
                            }
                        }
                    }
                }
            } catch { }

            return "";
        }

        /// <summary>
        /// Serves a KML file. Desktop gets a NetworkLink for live tracking.
        /// Mobile gets a static snapshot (no polling) to avoid the 99% loading issue.
        /// </summary>
        private void HandleViewKmlRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            var name = GetQueryParam(args, "name").Trim();
            var isMobile = !string.IsNullOrEmpty(GetQueryParam(args, "mobile"));
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            if(string.IsNullOrEmpty(name)) {
                name = icao;
            }

            string kml;
            if(isMobile) {
                kml = BuildMobileKml(args, icao, name);
            } else {
                kml = BuildDesktopKml(args, icao, name);
            }

            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "application/vnd.google-earth.kml+xml";
            args.Response.ContentLength = Encoding.UTF8.GetByteCount(kml);
            using(var stream = args.Response.OutputStream) {
                var bytes = Encoding.UTF8.GetBytes(kml);
                stream.Write(bytes, 0, bytes.Length);
            }
            args.Handled = true;
        }

        private string BuildDesktopKml(RequestReceivedEventArgs args, string icao, string name)
        {
            double? lat, lng, alt;
            float? track;
            string resolvedName;
            TryGetAircraftPosition(icao, out lat, out lng, out alt, out track, out resolvedName);
            if(!string.IsNullOrEmpty(resolvedName)) name = resolvedName;

            string baseUrl, rootPrefix;
            ResolveBaseUrl(args, out baseUrl, out rootPrefix);

            var cameraUrl = $"{baseUrl}{rootPrefix}/PilotsView/Camera.kml?icao={Uri.EscapeDataString(icao)}";
            var hudUrl = $"{baseUrl}{rootPrefix}/PilotsView/Hud.png?icao={Uri.EscapeDataString(icao)}";
            var tilt = Math.Max(0, Math.Min(90, _Options?.CameraTilt ?? 80));

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
            sb.AppendLine("  <Document>");
            sb.AppendLine($"    <name>Pilot's View - {EscapeXml(name)}</name>");
            sb.AppendLine("    <open>1</open>");
            // Initial Camera so Earth completes the initial zoom in one smooth flyTo
            // before the NetworkLink starts polling.
            if(lat.HasValue && lng.HasValue) {
                var altMeters = (alt ?? 1000) * 0.3048;
                var heading = track ?? 0;
                sb.AppendLine("    <Camera>");
                sb.AppendLine($"      <longitude>{lng.Value.ToString(CultureInfo.InvariantCulture)}</longitude>");
                sb.AppendLine($"      <latitude>{lat.Value.ToString(CultureInfo.InvariantCulture)}</latitude>");
                sb.AppendLine($"      <altitude>{altMeters.ToString("F1", CultureInfo.InvariantCulture)}</altitude>");
                sb.AppendLine($"      <heading>{heading.ToString("F1", CultureInfo.InvariantCulture)}</heading>");
                sb.AppendLine($"      <tilt>{tilt}</tilt>");
                sb.AppendLine("      <roll>0</roll>");
                sb.AppendLine("      <altitudeMode>absolute</altitudeMode>");
                sb.AppendLine("    </Camera>");
            }
            // viewRefreshMode=onStop waits for the camera to stop before fetching,
            // so the initial flyTo runs uninterrupted. Subsequent updates fire after
            // each step settles. Stepped motion is unavoidable — GE's flyToView always
            // applies ease-in-out per step, and there's no KML mechanism to change it.
            sb.AppendLine("    <NetworkLink>");
            sb.AppendLine("      <name>Live Camera</name>");
            sb.AppendLine("      <flyToView>1</flyToView>");
            sb.AppendLine("      <Link>");
            sb.AppendLine($"        <href>{EscapeXml(cameraUrl)}</href>");
            sb.AppendLine("        <viewRefreshMode>onStop</viewRefreshMode>");
            sb.AppendLine("        <viewRefreshTime>0</viewRefreshTime>");
            sb.AppendLine("        <viewFormat></viewFormat>");
            sb.AppendLine("      </Link>");
            sb.AppendLine("    </NetworkLink>");

            // HUD overlay: server renders a PNG of speed/alt/VSI/heading, GE pulls it every second.
            sb.AppendLine("    <ScreenOverlay>");
            sb.AppendLine("      <name>HUD</name>");
            sb.AppendLine("      <Icon>");
            sb.AppendLine($"        <href>{EscapeXml(hudUrl)}</href>");
            sb.AppendLine("        <refreshMode>onInterval</refreshMode>");
            sb.AppendLine("        <refreshInterval>1</refreshInterval>");
            sb.AppendLine("      </Icon>");
            sb.AppendLine("      <overlayXY x=\"0\" y=\"1\" xunits=\"fraction\" yunits=\"fraction\"/>");
            sb.AppendLine("      <screenXY x=\"10\" y=\"10\" xunits=\"pixels\" yunits=\"insetPixels\"/>");
            sb.AppendLine("      <size x=\"240\" y=\"160\" xunits=\"pixels\" yunits=\"pixels\"/>");
            sb.AppendLine("    </ScreenOverlay>");
            sb.AppendLine("  </Document>");
            sb.AppendLine("</kml>");
            return sb.ToString();
        }

        private static void ResolveBaseUrl(RequestReceivedEventArgs args, out string baseUrl, out string rootPrefix)
        {
            baseUrl = "http://localhost";
            rootPrefix = "";
            try {
                var uri = args.Request.Url;
                baseUrl = uri.GetLeftPart(UriPartial.Authority);
                var fwdHost = args.Request.Headers["X-Forwarded-Host"];
                var fwdProto = args.Request.Headers["X-Forwarded-Proto"];
                if(!string.IsNullOrEmpty(fwdHost)) {
                    var proto = string.IsNullOrEmpty(fwdProto) ? "http" : fwdProto;
                    baseUrl = proto + "://" + fwdHost;
                }
                var rawPath = uri.AbsolutePath;
                var pilotIdx = rawPath.IndexOf("/PilotsView/", StringComparison.OrdinalIgnoreCase);
                if(pilotIdx > 0) rootPrefix = rawPath.Substring(0, pilotIdx);
            } catch { }
        }

        private void TryGetAircraftPosition(string icao, out double? lat, out double? lng, out double? alt, out float? track, out string name)
        {
            lat = null; lng = null; alt = null; track = null; name = null;
            try {
                var feedManager = Factory.ResolveSingleton<IFeedManager>();
                foreach(var feed in feedManager.Feeds) {
                    if(feed.AircraftList == null) continue;
                    long unused1, unused2;
                    var aircraft = feed.AircraftList.TakeSnapshot(out unused1, out unused2);
                    foreach(var ac in aircraft) {
                        if(string.Equals(ac.Icao24, icao, StringComparison.OrdinalIgnoreCase)) {
                            lat = ac.Latitude;
                            lng = ac.Longitude;
                            alt = ac.Altitude;
                            track = ac.Track;
                            var cs = ac.Callsign;
                            var reg = ac.Registration;
                            if(!string.IsNullOrEmpty(cs)) name = cs.Trim();
                            else if(!string.IsNullOrEmpty(reg)) name = reg.Trim();
                            return;
                        }
                    }
                }
            } catch { }
        }

        /// <summary>
        /// Builds a KML for mobile: static Camera for instant terrain load,
        /// plus a NetworkLink for live tracking at a slower pace.
        /// </summary>
        private string BuildMobileKml(RequestReceivedEventArgs args, string icao, string name)
        {
            double? lat, lng, alt;
            float? track;
            string resolvedName;
            TryGetAircraftPosition(icao, out lat, out lng, out alt, out track, out resolvedName);
            if(!string.IsNullOrEmpty(resolvedName)) name = resolvedName;

            if(!lat.HasValue || !lng.HasValue) {
                return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                       "<kml xmlns=\"http://www.opengis.net/kml/2.2\">\n" +
                       "  <Document><name>Aircraft not found</name></Document>\n" +
                       "</kml>";
            }

            var altMeters = (alt ?? 1000) * 0.3048;
            var heading = track ?? 0;
            var tilt = Math.Max(0, Math.Min(90, _Options?.CameraTilt ?? 80));

            string baseUrl, rootPrefix;
            ResolveBaseUrl(args, out baseUrl, out rootPrefix);

            var cameraUrl = $"{baseUrl}{rootPrefix}/PilotsView/Camera.kml?icao={Uri.EscapeDataString(icao)}&mobile=1";

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
            sb.AppendLine("  <Document>");
            sb.AppendLine($"    <name>Pilot's View - {EscapeXml(name)}</name>");
            // Initial LookAt so terrain loads immediately at the aircraft location
            sb.AppendLine("    <LookAt>");
            sb.AppendLine($"      <longitude>{lng.Value.ToString(CultureInfo.InvariantCulture)}</longitude>");
            sb.AppendLine($"      <latitude>{lat.Value.ToString(CultureInfo.InvariantCulture)}</latitude>");
            sb.AppendLine($"      <altitude>{altMeters.ToString("F1", CultureInfo.InvariantCulture)}</altitude>");
            sb.AppendLine($"      <heading>{heading.ToString("F1", CultureInfo.InvariantCulture)}</heading>");
            sb.AppendLine($"      <tilt>{tilt}</tilt>");
            sb.AppendLine("      <range>300</range>");
            sb.AppendLine("      <altitudeMode>absolute</altitudeMode>");
            sb.AppendLine("    </LookAt>");
            // NetworkLink with onExpire - server paces refreshes.
            sb.AppendLine("    <NetworkLink>");
            sb.AppendLine("      <name>Live Camera</name>");
            sb.AppendLine("      <flyToView>1</flyToView>");
            sb.AppendLine("      <Link>");
            sb.AppendLine($"        <href>{EscapeXml(cameraUrl)}</href>");
            sb.AppendLine("        <refreshMode>onExpire</refreshMode>");
            sb.AppendLine("      </Link>");
            sb.AppendLine("    </NetworkLink>");
            sb.AppendLine("  </Document>");
            sb.AppendLine("</kml>");
            return sb.ToString();
        }

        /// <summary>
        /// Serves live camera KML updates polled by Google Earth's NetworkLink.
        /// Looks up the aircraft by ICAO from VRS's live aircraft list.
        /// </summary>
        private void HandleCameraKmlRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            var isMobile = !string.IsNullOrEmpty(GetQueryParam(args, "mobile"));
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            double? lat, lng, alt;
            float? track;
            string resolvedName;
            TryGetAircraftPosition(icao, out lat, out lng, out alt, out track, out resolvedName);
            var name = !string.IsNullOrEmpty(resolvedName) ? resolvedName : icao;

            string kml;
            if(!lat.HasValue || !lng.HasValue) {
                kml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                      "<kml xmlns=\"http://www.opengis.net/kml/2.2\">\n" +
                      "  <Document><name>Waiting for aircraft...</name></Document>\n" +
                      "</kml>";
            } else {
                // Choose the served sample: dequeue the oldest buffered one if present,
                // else fall back to the live current position.
                double servedLat = lat.Value, servedLng = lng.Value;
                double servedAltFeet = alt ?? 1000;
                double servedHeading = track ?? 0;
                var q = _Queues.GetOrAdd(icao, _ => new PositionQueue());
                q.LastAccessed = DateTime.UtcNow;
                lock(q.Lock) {
                    if(q.Samples.First != null) {
                        var s = q.Samples.First.Value;
                        q.Samples.RemoveFirst();
                        servedLat = s.Lat;
                        servedLng = s.Lng;
                        servedAltFeet = s.AltFeet;
                        servedHeading = s.Heading;
                    }
                }

                var altMeters = servedAltFeet * 0.3048;
                var heading = servedHeading;
                var tilt = Math.Max(0, Math.Min(90, _Options?.CameraTilt ?? 80));

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");

                if(isMobile) {
                    // Mobile keeps the onExpire-paced NetworkLinkControl with LookAt (no tour).
                    sb.AppendLine("  <NetworkLinkControl>");
                    var expires = DateTime.UtcNow.AddSeconds(5).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                    sb.AppendLine($"    <expires>{expires}</expires>");
                    sb.AppendLine("    <LookAt>");
                    sb.AppendLine($"      <longitude>{servedLng.ToString(CultureInfo.InvariantCulture)}</longitude>");
                    sb.AppendLine($"      <latitude>{servedLat.ToString(CultureInfo.InvariantCulture)}</latitude>");
                    sb.AppendLine($"      <altitude>{altMeters.ToString("F1", CultureInfo.InvariantCulture)}</altitude>");
                    sb.AppendLine($"      <heading>{heading.ToString("F1", CultureInfo.InvariantCulture)}</heading>");
                    sb.AppendLine($"      <tilt>{tilt}</tilt>");
                    sb.AppendLine("      <range>300</range>");
                    sb.AppendLine("      <altitudeMode>absolute</altitudeMode>");
                    sb.AppendLine("    </LookAt>");
                    sb.AppendLine("  </NetworkLinkControl>");
                } else {
                    sb.AppendLine("  <NetworkLinkControl>");
                    sb.AppendLine("    <Camera>");
                    sb.AppendLine($"      <longitude>{servedLng.ToString(CultureInfo.InvariantCulture)}</longitude>");
                    sb.AppendLine($"      <latitude>{servedLat.ToString(CultureInfo.InvariantCulture)}</latitude>");
                    sb.AppendLine($"      <altitude>{altMeters.ToString("F1", CultureInfo.InvariantCulture)}</altitude>");
                    sb.AppendLine($"      <heading>{heading.ToString("F1", CultureInfo.InvariantCulture)}</heading>");
                    sb.AppendLine($"      <tilt>{tilt}</tilt>");
                    sb.AppendLine("      <roll>0</roll>");
                    sb.AppendLine("      <altitudeMode>absolute</altitudeMode>");
                    sb.AppendLine("    </Camera>");
                    sb.AppendLine("  </NetworkLinkControl>");
                }

                sb.AppendLine("</kml>");
                kml = sb.ToString();
            }

            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "application/vnd.google-earth.kml+xml";
            args.Response.ContentLength = Encoding.UTF8.GetByteCount(kml);
            using(var stream = args.Response.OutputStream) {
                var bytes = Encoding.UTF8.GetBytes(kml);
                stream.Write(bytes, 0, bytes.Length);
            }
            args.Handled = true;
        }

        /// <summary>
        /// Serves a mobile launcher page that tries to open the KML in Google Earth via
        /// Android intent, with a fallback download button.
        /// </summary>
        /// <summary>
        /// Renders a small HUD PNG with the aircraft's flight data (callsign, altitude,
        /// ground speed, vertical speed, heading). Google Earth's ScreenOverlay refreshes
        /// the image via HTTP at the interval specified in the containing KML.
        /// </summary>
        /// <summary>
        /// Returns the current aircraft state as JSON for the Cesium-based web viewer.
        /// Called at 2Hz from Web.html; client-side interpolation handles the smoothing.
        /// </summary>
        private void HandlePositionJsonRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            string name = icao;
            double? lat = null, lng = null, alt = null, groundSpeed = null;
            float? track = null, verticalRate = null;

            try {
                var feedManager = Factory.ResolveSingleton<IFeedManager>();
                foreach(var feed in feedManager.Feeds) {
                    if(feed.AircraftList == null) continue;
                    long u1, u2;
                    var list = feed.AircraftList.TakeSnapshot(out u1, out u2);
                    foreach(var ac in list) {
                        if(string.Equals(ac.Icao24, icao, StringComparison.OrdinalIgnoreCase)) {
                            lat = ac.Latitude;
                            lng = ac.Longitude;
                            alt = ac.Altitude;
                            track = ac.Track;
                            groundSpeed = ac.GroundSpeed;
                            verticalRate = ac.VerticalRate;
                            var cs = ac.Callsign;
                            var reg = ac.Registration;
                            if(!string.IsNullOrEmpty(cs)) name = cs.Trim();
                            else if(!string.IsNullOrEmpty(reg)) name = reg.Trim();
                            break;
                        }
                    }
                }
            } catch { }

            var payload = Newtonsoft.Json.JsonConvert.SerializeObject(new {
                icao,
                name,
                lat,
                lng,
                altFt = alt,
                heading = track,
                groundSpeed,
                verticalRate,
                ts = DateTime.UtcNow.ToString("o"),
            });
            var bytes = Encoding.UTF8.GetBytes(payload);
            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "application/json";
            args.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            args.Response.ContentLength = bytes.Length;
            using(var s = args.Response.OutputStream) {
                s.Write(bytes, 0, bytes.Length);
            }
            args.Handled = true;
        }

        /// <summary>
        /// Serves a Cesium-based 3D pilot's view page. Cesium gives us direct per-frame
        /// control of the camera in a browser — no KML flyTo ease curves, no NetworkLink
        /// polling limitations. The page polls Position.json and interpolates each frame.
        /// </summary>
        private void HandleWebPageRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            var name = GetQueryParam(args, "name").Trim();
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }
            if(string.IsNullOrEmpty(name)) name = icao;

            var html = BuildWebPageHtml(icao, name);
            var bytes = Encoding.UTF8.GetBytes(html);
            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "text/html";
            args.Response.ContentLength = bytes.Length;
            using(var s = args.Response.OutputStream) {
                s.Write(bytes, 0, bytes.Length);
            }
            args.Handled = true;
        }

        private static string BuildWebPageHtml(string icao, string name)
        {
            var escName = EscapeXml(name);
            var escIcao = EscapeXml(icao);
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>Pilot's View - " + escName + @"</title>
  <link rel=""stylesheet"" href=""https://cesium.com/downloads/cesiumjs/releases/1.111/Build/Cesium/Widgets/widgets.css"">
  <script src=""https://cesium.com/downloads/cesiumjs/releases/1.111/Build/Cesium/Cesium.js""></script>
  <style>
    html, body { margin: 0; padding: 0; height: 100%; width: 100%; overflow: hidden; background: #000; font-family: Arial, sans-serif; }
    #cesium { position: absolute; inset: 0; }
    .cesium-viewer-bottom, .cesium-widget-credits { display: none !important; }
    #hud {
      position: absolute; top: 12px; left: 12px; z-index: 10;
      background: rgba(10,15,30,0.78); color: #fff;
      border: 1px solid rgba(100,160,255,0.6);
      border-radius: 6px; padding: 10px 14px; min-width: 200px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.5);
    }
    #hud .title { font-weight: bold; font-size: 16px; border-bottom: 1px solid rgba(150,180,220,0.4); padding-bottom: 6px; margin-bottom: 8px; }
    #hud .row { display: flex; justify-content: space-between; align-items: baseline; margin: 4px 0; }
    #hud .label { color: #b4d2f0; font-size: 11px; letter-spacing: 0.5px; }
    #hud .value { font-family: Consolas, 'Courier New', monospace; font-weight: bold; font-size: 14px; }
    #status { position: absolute; bottom: 12px; left: 12px; color: #aaa; font-size: 11px; z-index: 10; }
  </style>
</head>
<body>
  <div id=""cesium""></div>
  <div id=""hud"">
    <div class=""title"" id=""hudName"">" + escName + @"</div>
    <div class=""row""><span class=""label"">ALT</span><span class=""value"" id=""hudAlt"">---</span></div>
    <div class=""row""><span class=""label"">SPD</span><span class=""value"" id=""hudSpd"">---</span></div>
    <div class=""row""><span class=""label"">VSI</span><span class=""value"" id=""hudVsi"">---</span></div>
    <div class=""row""><span class=""label"">HDG</span><span class=""value"" id=""hudHdg"">---</span></div>
  </div>
  <div id=""status"">Waiting for aircraft data...</div>
  <script>
    const ICAO = " + Newtonsoft.Json.JsonConvert.SerializeObject(icao) + @";

    // current = what the camera is rendering this frame (lerp'd toward target).
    // target  = latest known aircraft position from the server.
    let current = null, target = null;
    let viewer = null;

    // Declared up-front so poll() can call applyCamera before the async init finishes.
    function applyCamera(p) {
      if (!viewer) return;
      viewer.camera.setView({
        destination: Cesium.Cartesian3.fromDegrees(p.lng, p.lat, p.alt),
        orientation: {
          heading: Cesium.Math.toRadians(p.hdg),
          pitch: Cesium.Math.toRadians(-8),  // Slight downward tilt so horizon is visible.
          roll: 0
        }
      });
    }

    (async function initCesium() {
      Cesium.Ion.defaultAccessToken = '';
      // ArcGIS provides tokenless satellite imagery and 3D terrain — no Cesium Ion
      // account required, and terrain is real (not ellipsoid), so mountains/coastlines
      // render correctly below the aircraft.
      const [imagery, terrain] = await Promise.all([
        Cesium.ArcGisMapServerImageryProvider.fromUrl(
          'https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer'
        ),
        Cesium.ArcGISTiledElevationTerrainProvider.fromUrl(
          'https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer'
        )
      ]);

      viewer = new Cesium.Viewer('cesium', {
        baseLayer: new Cesium.ImageryLayer(imagery),
        terrainProvider: terrain,
        timeline: false, animation: false, baseLayerPicker: false,
        geocoder: false, homeButton: false, sceneModePicker: false,
        navigationHelpButton: false, fullscreenButton: false, infoBox: false,
        selectionIndicator: false, shouldAnimate: true
      });
      viewer.scene.globe.enableLighting = false;
      viewer.scene.skyAtmosphere.show = true;

      // If position data arrived before the viewer was ready, snap the camera now.
      if (current) applyCamera(current);
    })().catch(e => {
      document.getElementById('status').textContent = 'Cesium init failed: ' + e.message;
      console.error(e);
    });

    async function poll() {
      try {
        const r = await fetch('Position.json?icao=' + encodeURIComponent(ICAO), { cache: 'no-store' });
        const d = await r.json();
        if (d.lat == null || d.lng == null) {
          document.getElementById('status').textContent = 'Waiting for aircraft data...';
          return;
        }
        document.getElementById('status').textContent = '';
        const altM = (d.altFt || 1000) * 0.3048;
        const hdg = d.heading == null ? (target ? target.hdg : 0) : d.heading;
        target = { lng: d.lng, lat: d.lat, alt: altM, hdg };
        if (!current) {
          current = { ...target };
          applyCamera(current);
        }
        document.getElementById('hudName').textContent = d.name || ICAO;
        document.getElementById('hudAlt').textContent = d.altFt != null ? Math.round(d.altFt).toLocaleString() + ' ft' : '---';
        document.getElementById('hudSpd').textContent = d.groundSpeed != null ? Math.round(d.groundSpeed) + ' kt' : '---';
        const vsi = d.verticalRate;
        document.getElementById('hudVsi').textContent = vsi != null ? (vsi >= 0 ? '+' : '') + Math.round(vsi).toLocaleString() + ' fpm' : '---';
        document.getElementById('hudHdg').textContent = d.heading != null ? String(Math.round(d.heading)).padStart(3,'0') + '°' : '---';
      } catch(e) { console.error(e); }
    }

    // Normalize a heading delta to the shortest path ([-180, 180]) so we don't spin the long way.
    function hdgDelta(from, to) {
      let d = to - from;
      while (d > 180) d -= 360;
      while (d < -180) d += 360;
      return d;
    }

    // Per-frame lerp toward the target. rate chosen so ~0.5s worth of catch-up per poll.
    function tick() {
      requestAnimationFrame(tick);
      if (!current || !target) return;
      const k = 0.08;
      current.lng += (target.lng - current.lng) * k;
      current.lat += (target.lat - current.lat) * k;
      current.alt += (target.alt - current.alt) * k;
      current.hdg = (current.hdg + hdgDelta(current.hdg, target.hdg) * k + 360) % 360;
      applyCamera(current);
    }

    poll();
    setInterval(poll, 500);
    tick();
  </script>
</body>
</html>";
        }

        private void HandleHudImageRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            string name = icao;
            double? alt = null, groundSpeed = null;
            float? track = null, verticalRate = null;

            try {
                var feedManager = Factory.ResolveSingleton<IFeedManager>();
                foreach(var feed in feedManager.Feeds) {
                    if(feed.AircraftList == null) continue;
                    long u1, u2;
                    var list = feed.AircraftList.TakeSnapshot(out u1, out u2);
                    foreach(var ac in list) {
                        if(string.Equals(ac.Icao24, icao, StringComparison.OrdinalIgnoreCase)) {
                            alt = ac.Altitude;
                            track = ac.Track;
                            groundSpeed = ac.GroundSpeed;
                            verticalRate = ac.VerticalRate;
                            var cs = ac.Callsign;
                            var reg = ac.Registration;
                            if(!string.IsNullOrEmpty(cs)) name = cs.Trim();
                            else if(!string.IsNullOrEmpty(reg)) name = reg.Trim();
                            break;
                        }
                    }
                }
            } catch(Exception ex) {
                StatusDescription = "HUD lookup error: " + ex.Message;
            }

            byte[] png;
            using(var bmp = new Bitmap(240, 160, PixelFormat.Format32bppArgb))
            using(var g = Graphics.FromImage(bmp)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);
                using(var bg = new SolidBrush(Color.FromArgb(190, 10, 15, 30))) {
                    g.FillRectangle(bg, 0, 0, 240, 160);
                }
                using(var border = new Pen(Color.FromArgb(180, 100, 160, 255), 1.5f)) {
                    g.DrawRectangle(border, 0, 0, 239, 159);
                }

                using(var titleFont = new Font("Arial", 14, FontStyle.Bold))
                using(var labelFont = new Font("Arial", 10, FontStyle.Regular))
                using(var valueFont = new Font("Consolas", 13, FontStyle.Bold)) {
                    g.DrawString(name, titleFont, Brushes.White, 10, 6);
                    using(var divider = new Pen(Color.FromArgb(120, 150, 180, 220), 1f)) {
                        g.DrawLine(divider, 10, 30, 230, 30);
                    }

                    var labelBrush = new SolidBrush(Color.FromArgb(200, 180, 210, 240));
                    var valueBrush = Brushes.White;
                    try {
                        var y = 38;
                        var rowH = 28;
                        void Row(string label, string val) {
                            g.DrawString(label, labelFont, labelBrush, 12, y + 4);
                            g.DrawString(val, valueFont, valueBrush, 70, y);
                            y += rowH;
                        }
                        Row("ALT", alt.HasValue ? alt.Value.ToString("N0", CultureInfo.InvariantCulture) + " ft" : "---");
                        Row("SPD", groundSpeed.HasValue ? groundSpeed.Value.ToString("N0", CultureInfo.InvariantCulture) + " kt" : "---");
                        var vsiStr = verticalRate.HasValue
                            ? (verticalRate.Value >= 0 ? "+" : "") + verticalRate.Value.ToString("N0", CultureInfo.InvariantCulture) + " fpm"
                            : "---";
                        Row("VSI", vsiStr);
                        Row("HDG", track.HasValue ? ((int)Math.Round(track.Value)).ToString(CultureInfo.InvariantCulture).PadLeft(3, '0') + "°" : "---");
                    } finally {
                        labelBrush.Dispose();
                    }
                }

                using(var ms = new MemoryStream()) {
                    bmp.Save(ms, ImageFormat.Png);
                    png = ms.ToArray();
                }
            }

            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "image/png";
            args.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            args.Response.ContentLength = png.Length;
            using(var s = args.Response.OutputStream) {
                s.Write(png, 0, png.Length);
            }
            args.Handled = true;
        }

        private void HandleLaunchPageRequest(RequestReceivedEventArgs args)
        {
            var icao = GetQueryParam(args, "icao").Trim().ToUpperInvariant();
            var name = GetQueryParam(args, "name").Trim();
            if(string.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }
            if(string.IsNullOrEmpty(name)) name = icao;

            // Build the KML URL using the same host
            var baseUrl = "";
            var rootPrefix = "";
            try {
                var uri = args.Request.Url;
                baseUrl = uri.GetLeftPart(UriPartial.Authority);
                var fwdHost = args.Request.Headers["X-Forwarded-Host"];
                var fwdProto = args.Request.Headers["X-Forwarded-Proto"];
                if(!string.IsNullOrEmpty(fwdHost)) {
                    var proto = string.IsNullOrEmpty(fwdProto) ? "http" : fwdProto;
                    baseUrl = proto + "://" + fwdHost;
                }
                var rawPath = uri.AbsolutePath;
                var pilotIdx = rawPath.IndexOf("/PilotsView/", StringComparison.OrdinalIgnoreCase);
                if(pilotIdx > 0) rootPrefix = rawPath.Substring(0, pilotIdx);
            } catch {
                baseUrl = "http://localhost";
            }

            var kmlUrl = $"{baseUrl}{rootPrefix}/PilotsView/View.kml?icao={Uri.EscapeDataString(icao)}&name={Uri.EscapeDataString(name)}&mobile=1";

            // Build the Android intent URI
            // intent://host/path?query#Intent;scheme=http;type=mime;package=com.google.earth;end
            Uri kmlUri;
            var intentUrl = kmlUrl; // fallback
            try {
                kmlUri = new Uri(kmlUrl);
                intentUrl = $"intent://{kmlUri.Host}{kmlUri.PathAndQuery}#Intent;scheme={kmlUri.Scheme};type=application/vnd.google-earth.kml+xml;package=com.google.earth;end";
            } catch { }

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head>");
            sb.AppendLine("<meta charset=\"utf-8\" />");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine($"<title>Pilot's View - {EscapeXml(name)}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  body { font-family: -apple-system, sans-serif; background: #1a1a2e; color: #eee; margin: 0; padding: 20px; text-align: center; }");
            sb.AppendLine("  .card { background: #16213e; border-radius: 12px; padding: 30px 20px; max-width: 400px; margin: 40px auto; box-shadow: 0 4px 20px rgba(0,0,0,0.4); }");
            sb.AppendLine("  h2 { margin: 0 0 8px; font-size: 20px; }");
            sb.AppendLine("  .sub { color: #888; font-size: 14px; margin-bottom: 24px; }");
            sb.AppendLine("  .status { font-size: 15px; margin: 16px 0; min-height: 24px; }");
            sb.AppendLine("  .btn { display: inline-block; padding: 14px 28px; border-radius: 8px; font-size: 16px; font-weight: 600; text-decoration: none; margin: 8px; cursor: pointer; border: none; }");
            sb.AppendLine("  .btn-primary { background: #3366cc; color: #fff; }");
            sb.AppendLine("  .btn-primary:active { background: #2255bb; }");
            sb.AppendLine("  .btn-secondary { background: #333; color: #ccc; }");
            sb.AppendLine("  .steps { text-align: left; font-size: 14px; color: #aaa; margin-top: 20px; line-height: 1.8; }");
            sb.AppendLine("  .steps b { color: #ddd; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<div class=\"card\">");
            sb.AppendLine($"  <h2>Pilot's View</h2>");
            sb.AppendLine($"  <div class=\"sub\">{EscapeXml(name)}</div>");
            sb.AppendLine("  <div class=\"status\" id=\"status\">Opening Google Earth...</div>");
            sb.AppendLine($"  <a id=\"intentLink\" href=\"{EscapeXml(intentUrl)}\" class=\"btn btn-primary\" style=\"display:none;\">Open in Google Earth</a>");
            sb.AppendLine($"  <a id=\"downloadLink\" href=\"{EscapeXml(kmlUrl)}\" class=\"btn btn-secondary\" style=\"display:none;\" download=\"PilotsView.kml\">Download KML File</a>");
            sb.AppendLine("  <div class=\"steps\" id=\"steps\" style=\"display:none;\">");
            sb.AppendLine("    <b>If Google Earth didn't open:</b><br/>");
            sb.AppendLine("    1. Tap <b>Download KML File</b> below<br/>");
            sb.AppendLine("    2. Open your <b>Downloads</b> folder<br/>");
            sb.AppendLine("    3. Tap the <b>.kml</b> file<br/>");
            sb.AppendLine("    4. Choose <b>Google Earth</b> to open it");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<script>");
            sb.AppendLine("(function() {");
            sb.AppendLine("  var status = document.getElementById('status');");
            sb.AppendLine("  var intentLink = document.getElementById('intentLink');");
            sb.AppendLine("  var downloadLink = document.getElementById('downloadLink');");
            sb.AppendLine("  var steps = document.getElementById('steps');");
            sb.AppendLine();
            sb.AppendLine("  // Try the intent URI automatically");
            sb.AppendLine("  var isAndroid = /Android/i.test(navigator.userAgent);");
            sb.AppendLine("  if(isAndroid) {");
            sb.AppendLine($"    window.location.href = '{intentUrl.Replace("'", "\\'")}';");
            sb.AppendLine("  } else {");
            sb.AppendLine("    // Not Android - just download the KML");
            sb.AppendLine($"    window.location.href = '{kmlUrl.Replace("'", "\\'")}';");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  // After a short delay, show fallback buttons");
            sb.AppendLine("  setTimeout(function() {");
            sb.AppendLine("    status.textContent = 'Google Earth should be opening. If not:';");
            sb.AppendLine("    intentLink.style.display = 'inline-block';");
            sb.AppendLine("    downloadLink.style.display = 'inline-block';");
            sb.AppendLine("    steps.style.display = 'block';");
            sb.AppendLine("  }, 2000);");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            var html = sb.ToString();

            args.Response.StatusCode = HttpStatusCode.OK;
            args.Response.MimeType = "text/html";
            args.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
            using(var stream = args.Response.OutputStream) {
                var bytes = Encoding.UTF8.GetBytes(html);
                stream.Write(bytes, 0, bytes.Length);
            }
            args.Handled = true;
        }

        private static string EscapeXml(string text)
        {
            if(string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        /// <summary>
        /// Builds the injected JavaScript that adds a "Pilot's View" link
        /// to the aircraft detail panel via VRS.LinkRenderHandler.
        /// </summary>
        private string BuildInjectScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.LinkRenderHandler || !VRS.linkRenderHandlers) return;");
            sb.AppendLine();
            sb.AppendLine("  VRS.LinkSite['PilotsView'] = 'pilotsview';");
            sb.AppendLine("  VRS.LinkSite['PilotsViewWeb'] = 'pilotsviewweb';");
            sb.AppendLine("  VRS.linkRenderHandlers.push(");
            sb.AppendLine("    new VRS.LinkRenderHandler({");
            sb.AppendLine("      linkSite:        VRS.LinkSite['PilotsView'],");
            sb.AppendLine("      displayOrder:    9500,");
            sb.AppendLine("      canLinkAircraft: function(aircraft) {");
            sb.AppendLine("        return aircraft.latitude.val != null && aircraft.longitude.val != null;");
            sb.AppendLine("      },");
            sb.AppendLine("      hasChanged: function(aircraft) {");
            sb.AppendLine("        return aircraft.latitude.chg || aircraft.longitude.chg;");
            sb.AppendLine("      },");
            sb.AppendLine("      title:    function() { return \"Pilot's View\"; },");
            sb.AppendLine("      buildUrl: function(aircraft) {");
            sb.AppendLine("        var icao = aircraft.formatIcao() || '';");
            sb.AppendLine("        var name = aircraft.formatCallsign(false) || aircraft.formatRegistration() || icao;");
            sb.AppendLine("        var params = '?icao=' + encodeURIComponent(icao) + '&name=' + encodeURIComponent(name);");
            sb.AppendLine("        if(/Android/i.test(navigator.userAgent)) {");
            sb.AppendLine("          return 'PilotsView/Launch.html' + params;");
            sb.AppendLine("        }");
            sb.AppendLine("        return 'PilotsView/View.kml' + params;");
            sb.AppendLine("      },");
            sb.AppendLine("      target: 'pilotsView'");
            sb.AppendLine("    })");
            sb.AppendLine("  );");
            sb.AppendLine("  VRS.linkRenderHandlers.push(");
            sb.AppendLine("    new VRS.LinkRenderHandler({");
            sb.AppendLine("      linkSite:        VRS.LinkSite['PilotsViewWeb'],");
            sb.AppendLine("      displayOrder:    9501,");
            sb.AppendLine("      canLinkAircraft: function(aircraft) {");
            sb.AppendLine("        return aircraft.latitude.val != null && aircraft.longitude.val != null;");
            sb.AppendLine("      },");
            sb.AppendLine("      hasChanged: function(aircraft) {");
            sb.AppendLine("        return aircraft.latitude.chg || aircraft.longitude.chg;");
            sb.AppendLine("      },");
            sb.AppendLine("      title:    function() { return \"Pilot's View (Web)\"; },");
            sb.AppendLine("      buildUrl: function(aircraft) {");
            sb.AppendLine("        var icao = aircraft.formatIcao() || '';");
            sb.AppendLine("        var name = aircraft.formatCallsign(false) || aircraft.formatRegistration() || icao;");
            sb.AppendLine("        return 'PilotsView/Web.html?icao=' + encodeURIComponent(icao) + '&name=' + encodeURIComponent(name);");
            sb.AppendLine("      },");
            sb.AppendLine("      target: 'pilotsViewWeb'");
            sb.AppendLine("    })");
            sb.AppendLine("  );");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
