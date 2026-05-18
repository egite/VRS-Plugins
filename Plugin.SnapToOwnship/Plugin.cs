using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.SnapToOwnship
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebSite _WebSite;
        private IWebServer _WebServer;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.SnapToOwnship"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Snap to Ownship"; } }
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
        }

        // Serves the currently-resolved ownship ICAO so the injected click handler can pick up
        // option changes without a desktop.html reload. Without this, saving a new ICAO in
        // WebAdmin only takes effect after F5 (the script bakes the value in at injection time).
        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;
            try {
                if(string.Equals(args.PathAndFile, "/SnapToOwnship/Ownship.json", StringComparison.OrdinalIgnoreCase)) {
                    var icao = ResolveOwnshipIcao(_Options);
                    var json = "{\"icao\":\"" + (icao ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    args.Response.StatusCode = HttpStatusCode.OK;
                    args.Response.MimeType = "application/json";
                    args.Response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
                    args.Response.ContentLength = bytes.Length;
                    using(var s = args.Response.OutputStream) { s.Write(bytes, 0, bytes.Length); }
                    args.Handled = true;
                }
            } catch(Exception ex) {
                StatusDescription = "Request error: " + ex.Message;
            }
        }

        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "SnapToOwnshipPluginOptions.html",
                    "Snap to Ownship",
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
                var icao = ResolveOwnshipIcao(options);
                var source = options.AutoDetectIcao ? " (auto)" : "";
                Status = String.Format("Enabled - ICAO: {0}{1}",
                    string.IsNullOrEmpty(icao) ? "(not set)" : icao,
                    source);
            }
        }

        /// <summary>
        /// Returns the effective ownship ICAO — either the manually-configured value, or the
        /// most recent value the Stratux GPS plugin has read from the Stratux device when
        /// auto-detect mode is on. Returns an empty string (not null) when nothing is available.
        /// </summary>
        internal string ResolveOwnshipIcao(Options options)
        {
            if(options == null) return "";
            if(options.AutoDetectIcao) {
                return (StratuxBridge.TryGetOwnshipIcao() ?? "").Trim().ToUpperInvariant();
            }
            return (options.OwnshipIcao ?? "").Trim().ToUpperInvariant();
        }

        private string BuildInjectScript()
        {
            // Bake in the current ICAO as a fallback for when the JSON endpoint is unreachable,
            // but the click handler always re-fetches Ownship.json so option changes take effect
            // without a desktop.html reload.
            var bakedIcao = ResolveOwnshipIcao(_Options);

            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(typeof VRS === 'undefined' || typeof $ === 'undefined') return;");
            sb.AppendFormat("  var fallbackIcao = '{0}';\n", bakedIcao.Replace("'", "\\'"));
            sb.AppendLine();
            // Resolve pageSettings / aircraftList / mapPlugin fresh at click time. Capturing them
            // at hookInitialised time can grab them before mapLoaded() assigns them, and a stale
            // reference doesn't survive map provider switches (Google <-> Leaflet via TileServerMBTiles).
            sb.AppendLine("  function getPageSettings() {");
            sb.AppendLine("    return (VRS.bootstrap && VRS.bootstrap.pageSettings) || null;");
            sb.AppendLine("  }");
            sb.AppendLine("  function getMapPlugin() {");
            sb.AppendLine("    var ps = getPageSettings();");
            sb.AppendLine("    if(ps && ps.mapPlugin) return ps.mapPlugin;");
            sb.AppendLine("    try {");
            sb.AppendLine("      var mapJQ = (ps && ps.mapJQ) || $('#map');");
            sb.AppendLine("      if(mapJQ && mapJQ.length && VRS.jQueryUIHelper && VRS.jQueryUIHelper.getMapPlugin) {");
            sb.AppendLine("        return VRS.jQueryUIHelper.getMapPlugin(mapJQ);");
            sb.AppendLine("      }");
            sb.AppendLine("    } catch(e) {}");
            sb.AppendLine("    return null;");
            sb.AppendLine("  }");
            sb.AppendLine();
            // Fetch the current ICAO from the server endpoint so saved option changes apply
            // immediately, falling back to the value baked in at page-load time if the endpoint
            // fails (e.g. older plugin build without the endpoint).
            sb.AppendLine("  function fetchCurrentIcao(cb) {");
            sb.AppendLine("    $.ajax({");
            sb.AppendLine("      url: 'SnapToOwnship/Ownship.json',");
            sb.AppendLine("      method: 'GET',");
            sb.AppendLine("      dataType: 'json',");
            sb.AppendLine("      cache: false,");
            sb.AppendLine("      success: function(data) { cb((data && data.icao ? data.icao : '').toUpperCase() || fallbackIcao.toUpperCase()); },");
            sb.AppendLine("      error: function() { cb(fallbackIcao.toUpperCase()); }");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  function snapWithIcao(icaoUpper) {");
            sb.AppendLine("    if(!icaoUpper) { console.warn('[SnapToOwnship] no ownship ICAO configured'); return; }");
            sb.AppendLine("    var ps = getPageSettings();");
            sb.AppendLine("    var mp = getMapPlugin();");
            sb.AppendLine("    if(!mp) { console.warn('[SnapToOwnship] map plugin not available'); return; }");
            sb.AppendLine("    var aircraft = (ps && ps.aircraftList && ps.aircraftList.findAircraftByIcao) ? ps.aircraftList.findAircraftByIcao(icaoUpper) : null;");
            sb.AppendLine("    if(aircraft && aircraft.hasPosition && aircraft.hasPosition()) {");
            sb.AppendLine("      var pos = aircraft.getPosition();");
            sb.AppendLine("      if(ps.aircraftList.setSelectedAircraft) ps.aircraftList.setSelectedAircraft(aircraft, false);");
            sb.AppendLine("      if(mp.panTo) mp.panTo(pos); else mp.setCenter(pos);");
            sb.AppendLine("      return;");
            sb.AppendLine("    }");
            // Fallback: aircraft not yet in the local list (or no position yet). Fetch AircraftList.json,
            // recenter on whatever the server has, and retry selecting once the live list catches up.
            sb.AppendLine("    $.ajax({");
            sb.AppendLine("      url: 'AircraftList.json',");
            sb.AppendLine("      method: 'GET',");
            sb.AppendLine("      dataType: 'json',");
            sb.AppendLine("      cache: false,");
            sb.AppendLine("      success: function(data) {");
            sb.AppendLine("        var acList = (data && data.acList) || [];");
            sb.AppendLine("        var found = null;");
            sb.AppendLine("        for(var i = 0; i < acList.length; i++) {");
            sb.AppendLine("          if(acList[i].Icao && acList[i].Icao.toUpperCase() === icaoUpper) { found = acList[i]; break; }");
            sb.AppendLine("        }");
            sb.AppendLine("        if(!found || found.Lat == null || found.Long == null) {");
            sb.AppendLine("          console.warn('[SnapToOwnship] ownship ICAO ' + icaoUpper + ' not found in AircraftList.json (no position yet?)');");
            sb.AppendLine("          return;");
            sb.AppendLine("        }");
            sb.AppendLine("        var mp2 = getMapPlugin();");
            sb.AppendLine("        if(mp2) {");
            sb.AppendLine("          var pos = { lat: found.Lat, lng: found.Long };");
            sb.AppendLine("          if(mp2.panTo) mp2.panTo(pos); else mp2.setCenter(pos);");
            sb.AppendLine("        }");
            sb.AppendLine("        var retries = 0;");
            sb.AppendLine("        var trySelect = function() {");
            sb.AppendLine("          var ps2 = getPageSettings();");
            sb.AppendLine("          var ac = (ps2 && ps2.aircraftList && ps2.aircraftList.findAircraftByIcao) ? ps2.aircraftList.findAircraftByIcao(icaoUpper) : null;");
            sb.AppendLine("          if(ac && ps2.aircraftList.setSelectedAircraft) { ps2.aircraftList.setSelectedAircraft(ac, false); }");
            sb.AppendLine("          else if(retries++ < 10) { setTimeout(trySelect, 1000); }");
            sb.AppendLine("        };");
            sb.AppendLine("        setTimeout(trySelect, 500);");
            sb.AppendLine("      },");
            sb.AppendLine("      error: function(xhr, status, err) { console.warn('[SnapToOwnship] AircraftList.json fetch failed: ' + status + ' ' + err); }");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  function buildButton() {");
            sb.AppendLine("    var btn = document.createElement('button');");
            sb.AppendLine("    btn.type = 'button';");
            sb.AppendLine("    btn.textContent = 'Snap to Ownship';");
            sb.AppendLine("    btn.style.cssText = 'padding:8px 16px;background:#3366cc;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:13px;box-shadow:0 2px 6px rgba(0,0,0,0.3);';");
            sb.AppendLine("    btn.onmouseenter = function() { btn.style.background='#2255bb'; };");
            sb.AppendLine("    btn.onmouseleave = function() { btn.style.background='#3366cc'; };");
            // jQuery .on('click') is the same as .onclick for a fresh element, but it survives any
            // future jQuery DOM manipulation and gives consistent event bubbling.
            sb.AppendLine("    $(btn).on('click', function(e) {");
            sb.AppendLine("      e.preventDefault();");
            sb.AppendLine("      e.stopPropagation();");
            sb.AppendLine("      fetchCurrentIcao(function(icao) { snapWithIcao(icao); });");
            sb.AppendLine("    });");
            // Leaflet: clicks on .leaflet-control descendants still propagate to the map by default,
            // which can trigger pan/zoom/marker handlers that fight the recenter. Suppress that.
            sb.AppendLine("    try {");
            sb.AppendLine("      if(typeof L !== 'undefined' && L.DomEvent) {");
            sb.AppendLine("        L.DomEvent.disableClickPropagation(btn);");
            sb.AppendLine("        L.DomEvent.disableScrollPropagation(btn);");
            sb.AppendLine("      }");
            sb.AppendLine("    } catch(e) {}");
            sb.AppendLine("    return btn;");
            sb.AppendLine("  }");
            sb.AppendLine();
            // Wait for the map plugin to exist before adding the control. hookInitialised fires on
            // bootstrap completion but mapLoaded() may not have populated pageSettings.mapPlugin yet.
            sb.AppendLine("  var _added = false;");
            sb.AppendLine("  function tryAddButton() {");
            sb.AppendLine("    if(_added) return true;");
            sb.AppendLine("    var mp = getMapPlugin();");
            sb.AppendLine("    if(!mp || !mp.addControl) return false;");
            sb.AppendLine("    try {");
            sb.AppendLine("      mp.addControl($(buildButton()), VRS.MapPosition.BottomRight);");
            sb.AppendLine("      _added = true;");
            sb.AppendLine("      return true;");
            sb.AppendLine("    } catch(e) { console.warn('[SnapToOwnship] addControl failed: ' + e); return false; }");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  function pollInit() {");
            sb.AppendLine("    if(!tryAddButton()) setTimeout(pollInit, 500);");
            sb.AppendLine("  }");
            sb.AppendLine("  if(VRS.globalDispatch && VRS.globalEvent && VRS.globalEvent.bootstrapCreated) {");
            sb.AppendLine("    VRS.globalDispatch.hook(VRS.globalEvent.bootstrapCreated, function(bootstrap) {");
            sb.AppendLine("      bootstrap.hookInitialised(function() { pollInit(); });");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            // Belt and braces: if bootstrapCreated already fired before this script ran, poll anyway.
            sb.AppendLine("  setTimeout(pollInit, 1000);");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
