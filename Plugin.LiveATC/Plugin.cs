using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dapper;
using InterfaceFactory;
using Newtonsoft.Json;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.SQLite;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.LiveATC
{
    /// <summary>
    /// The entry point for the plugin that enables double-clicking near an
    /// airport on the map to open its LiveATC page in a new browser tab.
    /// </summary>
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebServer _WebServer;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        private string _DatabaseFileName;
        private string _CachedAirportsJson;
        private DateTime _CachedTime;
        private readonly object _CacheLock = new object();

        /// <summary>
        /// Gets the last initialised instance of the plugin object.
        /// </summary>
        public static Plugin Singleton { get; private set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Id { get { return "VirtualRadarServer.Plugin.LiveATC"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string PluginFolder { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Name { get { return "Live ATC"; } }

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
        /// <param name="args"></param>
        protected virtual void OnStatusChanged(EventArgs args)
        {
            EventHelper.Raise(StatusChanged, this, args);
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="classFactory"></param>
        public void RegisterImplementations(IClassFactory classFactory)
        {
        }

        /// <summary>
        /// See interface docs.
        /// </summary>
        /// <param name="parameters"></param>
        public void Startup(PluginStartupParameters parameters)
        {
            Singleton = this;
            _Options = OptionsStorage.Load(this);
            _WebSite = parameters.WebSite;

            // Resolve database path
            var configurationStorage = Factory.ResolveSingleton<IConfigurationStorage>();
            _DatabaseFileName = Path.Combine(configurationStorage.Folder, "StandingData.sqb");

            var autoConfigWebServer = Factory.ResolveSingleton<IAutoConfigWebServer>();
            _WebServer = autoConfigWebServer.WebServer;
            _WebServer.BeforeRequestReceived += WebServer_BeforeRequestReceived;

            var pages = new[] { "/desktop.html", "/mobile.html", "/desktopReport.html", "/mobileReport.html" };
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
                    "LiveATCPluginOptions.html",
                    "Live ATC",
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

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    _CachedAirportsJson = null;
                    ApplyOptions(dialog.Options);
                }
            }
        }

        /// <summary>
        /// Applies the settings from the currently loaded options.
        /// </summary>
        private void ApplyOptions(Options options)
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

        /// <summary>
        /// Intercepts web requests for our custom endpoints.
        /// </summary>
        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;

            if(string.Equals(args.PathAndFile, "/LiveATC/Airports.json", StringComparison.OrdinalIgnoreCase)) {
                HandleAirportsRequest(args);
            }
        }

        /// <summary>
        /// Returns a JSON array of airports with ICAO codes and coordinates.
        /// </summary>
        private void HandleAirportsRequest(RequestReceivedEventArgs args)
        {
            string json;
            lock(_CacheLock) {
                if(_CachedAirportsJson == null || (DateTime.UtcNow - _CachedTime).TotalMinutes > 10) {
                    var airports = new List<object>();
                    try {
                        if(File.Exists(_DatabaseFileName)) {
                            using(var conn = CreateOpenConnection()) {
                                foreach(var row in conn.Query(@"
                                    SELECT [Icao], [Latitude], [Longitude]
                                    FROM   [Airport]
                                    WHERE  [Icao] IS NOT NULL
                                      AND  [Latitude] IS NOT NULL
                                      AND  [Longitude] IS NOT NULL
                                ")) {
                                    airports.Add(new {
                                        i = (string)row.Icao,
                                        a = (double)row.Latitude,
                                        o = (double)row.Longitude,
                                    });
                                }
                            }
                        }
                    } catch {
                        // If database is unavailable, return empty array
                    }
                    _CachedAirportsJson = JsonConvert.SerializeObject(airports);
                    _CachedTime = DateTime.UtcNow;
                }
                json = _CachedAirportsJson;
            }

            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        /// <summary>
        /// Returns an open connection to the standing data database.
        /// </summary>
        private IDbConnection CreateOpenConnection()
        {
            var connectionStringBuilder = Factory.Resolve<ISQLiteConnectionStringBuilder>().Initialise();
            connectionStringBuilder.DataSource = _DatabaseFileName;
            connectionStringBuilder.DateTimeFormat = SQLiteDateFormats.ISO8601;
            connectionStringBuilder.FailIfMissing = true;
            connectionStringBuilder.ReadOnly = true;
            connectionStringBuilder.JournalMode = SQLiteJournalModeEnum.Off;

            var result = Factory.Resolve<ISQLiteConnectionProvider>().Create(connectionStringBuilder.ConnectionString);
            result.Open();

            return result;
        }

        /// <summary>
        /// Builds the inline JavaScript that adds LiveATC double-click
        /// functionality to the map.
        /// </summary>
        private string BuildInjectScript()
        {
            var sb = new StringBuilder();
            // Sidebar CSS
            sb.AppendLine(@"<style type=""text/css"">");
            sb.AppendLine("#liveAtcSidebar { position:absolute; top:0; right:0; max-height:100%; z-index:1000; background:#1a1a2e; display:flex; flex-direction:column; font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; font-size:13px; color:#e0e0e0; border-radius:0 0 0 6px; }");
            sb.AppendLine("#liveAtcSidebar .sb-hdr { padding:8px 12px; font-weight:bold; font-size:14px; border-bottom:1px solid #333; background:#12122a; flex-shrink:0; cursor:pointer; user-select:none; white-space:nowrap; }");
            sb.AppendLine("#liveAtcSidebar .sb-list { flex:1; overflow-y:auto; }");
            sb.AppendLine("#liveAtcSidebar .sb-row { padding:5px 12px; cursor:pointer; border-bottom:1px solid rgba(255,255,255,0.05); white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }");
            sb.AppendLine("#liveAtcSidebar .sb-row:hover { background:rgba(255,255,255,0.1); }");
            sb.AppendLine("#liveAtcSidebar .sb-icao { font-weight:bold; color:#7ec8e3; }");
            sb.AppendLine("#liveAtcSidebar .sb-list::-webkit-scrollbar { width:6px; }");
            sb.AppendLine("#liveAtcSidebar .sb-list::-webkit-scrollbar-track { background:#12122a; }");
            sb.AppendLine("#liveAtcSidebar .sb-list::-webkit-scrollbar-thumb { background:#444; border-radius:3px; }");
            sb.AppendLine("</style>");
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.jQueryUIHelper) return;");
            sb.AppendLine();

            // State variables
            sb.AppendLine("  var _airports = null;");
            sb.AppendLine("  var _grid = {};");
            sb.AppendLine("  var _nearAirport = null;");
            sb.AppendLine("  var _mapPlugin = null;");
            sb.AppendLine("  var _nativeMap = null;");
            sb.AppendLine("  var _isLeaflet = false;");
            sb.AppendLine("  var _active = true;");
            sb.AppendLine("  var _sidebar = null;");
            sb.AppendLine("  var _sidebarCollapsed = false;");
            sb.AppendLine();

            // Load saved state
            sb.AppendLine("  try { var s = localStorage.getItem('vrsLiveATC'); if(s === '0') _active = false; } catch(e) {}");
            sb.AppendLine("  try { if(localStorage.getItem('vrsLiveATCCollapsed') === '1') _sidebarCollapsed = true; } catch(e) {}");
            sb.AppendLine();

            // Fetch airport data
            sb.AppendLine("  function fetchAirports() {");
            sb.AppendLine("    try {");
            sb.AppendLine("      var xhr = new XMLHttpRequest();");
            sb.AppendLine("      xhr.open('GET', 'LiveATC/Airports.json', true);");
            sb.AppendLine("      xhr.onreadystatechange = function() {");
            sb.AppendLine("        if(xhr.readyState === 4 && xhr.status === 200) {");
            sb.AppendLine("          try {");
            sb.AppendLine("            _airports = JSON.parse(xhr.responseText);");
            sb.AppendLine("            _grid = buildGrid(_airports);");
            sb.AppendLine("            updateSidebar();");
            sb.AppendLine("          } catch(e) {}");
            sb.AppendLine("        }");
            sb.AppendLine("      };");
            sb.AppendLine("      xhr.send();");
            sb.AppendLine("    } catch(e) {}");
            sb.AppendLine("  }");
            sb.AppendLine("  fetchAirports();");
            sb.AppendLine();

            // Spatial grid index — bucket airports by rounded lat/lon (1-degree cells)
            sb.AppendLine("  function buildGrid(airports) {");
            sb.AppendLine("    var g = {};");
            sb.AppendLine("    for(var i = 0; i < airports.length; i++) {");
            sb.AppendLine("      var key = Math.floor(airports[i].a) + ',' + Math.floor(airports[i].o);");
            sb.AppendLine("      if(!g[key]) g[key] = [];");
            sb.AppendLine("      g[key].push(i);");
            sb.AppendLine("    }");
            sb.AppendLine("    return g;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Haversine distance in nautical miles
            sb.AppendLine("  function distNm(lat1, lon1, lat2, lon2) {");
            sb.AppendLine("    var dLat = (lat2 - lat1) * 0.01745329;");
            sb.AppendLine("    var dLon = (lon2 - lon1) * 0.01745329;");
            sb.AppendLine("    var a = Math.sin(dLat/2) * Math.sin(dLat/2) +");
            sb.AppendLine("            Math.cos(lat1 * 0.01745329) * Math.cos(lat2 * 0.01745329) *");
            sb.AppendLine("            Math.sin(dLon/2) * Math.sin(dLon/2);");
            sb.AppendLine("    return 3440.065 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Find nearest airport within threshold (nautical miles)
            sb.AppendLine("  function findNearest(lat, lng, thresholdNm) {");
            sb.AppendLine("    if(!_airports) return null;");
            sb.AppendLine("    var best = null;");
            sb.AppendLine("    var bestDist = thresholdNm;");
            sb.AppendLine("    var cLat = Math.floor(lat);");
            sb.AppendLine("    var cLon = Math.floor(lng);");
            sb.AppendLine("    for(var dLat = -1; dLat <= 1; dLat++) {");
            sb.AppendLine("      for(var dLon = -1; dLon <= 1; dLon++) {");
            sb.AppendLine("        var key = (cLat + dLat) + ',' + (cLon + dLon);");
            sb.AppendLine("        var cell = _grid[key];");
            sb.AppendLine("        if(!cell) continue;");
            sb.AppendLine("        for(var j = 0; j < cell.length; j++) {");
            sb.AppendLine("          var ap = _airports[cell[j]];");
            sb.AppendLine("          var d = distNm(lat, lng, ap.a, ap.o);");
            sb.AppendLine("          if(d < bestDist) { bestDist = d; best = ap; }");
            sb.AppendLine("        }");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("    return best;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Red crosshair cursor as inline SVG data URI
            sb.AppendLine("  var _redCross = (function() {");
            sb.AppendLine("    var svg = '<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"32\" height=\"32\">'");
            sb.AppendLine("      + '<line x1=\"16\" y1=\"0\" x2=\"16\" y2=\"32\" stroke=\"red\" stroke-width=\"2\"/>'");
            sb.AppendLine("      + '<line x1=\"0\" y1=\"16\" x2=\"32\" y2=\"16\" stroke=\"red\" stroke-width=\"2\"/>'");
            sb.AppendLine("      + '</svg>';");
            sb.AppendLine("    return 'url(data:image/svg+xml;base64,' + btoa(svg) + ') 16 16, crosshair';");
            sb.AppendLine("  })();");
            sb.AppendLine();

            // Set cursor style
            sb.AppendLine("  function setCursor(active) {");
            sb.AppendLine("    if(_isLeaflet) {");
            sb.AppendLine("      _nativeMap.getContainer().style.cursor = active ? _redCross : '';");
            sb.AppendLine("    } else {");
            sb.AppendLine("      _nativeMap.setOptions({ draggableCursor: active ? _redCross : null });");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Mouse move handler
            sb.AppendLine("  function onMouseMove(e) {");
            sb.AppendLine("    if(!_active) {");
            sb.AppendLine("      if(_nearAirport) { _nearAirport = null; setCursor(false); }");
            sb.AppendLine("      return;");
            sb.AppendLine("    }");
            sb.AppendLine("    var lat, lng;");
            sb.AppendLine("    if(_isLeaflet) { lat = e.latlng.lat; lng = e.latlng.lng; }");
            sb.AppendLine("    else { lat = e.latLng.lat(); lng = e.latLng.lng(); }");
            sb.AppendLine("    var nearest = findNearest(lat, lng, 5);");
            sb.AppendLine("    if(nearest !== _nearAirport) {");
            sb.AppendLine("      _nearAirport = nearest;");
            sb.AppendLine("      setCursor(!!nearest);");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Double-click handler
            sb.AppendLine("  function onDblClick(e) {");
            sb.AppendLine("    if(!_active) return;");
            sb.AppendLine("    var lat, lng;");
            sb.AppendLine("    if(_isLeaflet) { lat = e.latlng.lat; lng = e.latlng.lng; }");
            sb.AppendLine("    else { lat = e.latLng.lat(); lng = e.latLng.lng(); }");
            sb.AppendLine("    var nearest = findNearest(lat, lng, 5);");
            sb.AppendLine("    if(nearest) {");
            sb.AppendLine("      var url = 'https://www.liveatc.net/search/?icao=' + nearest.i.toLowerCase();");
            sb.AppendLine("      window.open(url, '_blank');");
            sb.AppendLine("      if(_isLeaflet && e.originalEvent) e.originalEvent.preventDefault();");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Initialize map events
            sb.AppendLine("  function initMap() {");
            sb.AppendLine("    var mapJQ = $('#map');");
            sb.AppendLine("    if(!mapJQ.length || !VRS.jQueryUIHelper.getMapPlugin) return false;");
            sb.AppendLine("    _mapPlugin = VRS.jQueryUIHelper.getMapPlugin(mapJQ);");
            sb.AppendLine("    if(!_mapPlugin || !_mapPlugin.isOpen()) { _mapPlugin = null; return false; }");
            sb.AppendLine("    _nativeMap = _mapPlugin.getNative();");
            sb.AppendLine("    if(!_nativeMap) { _mapPlugin = null; return false; }");
            sb.AppendLine("    _isLeaflet = (typeof L !== 'undefined' && _nativeMap instanceof L.Map);");
            sb.AppendLine();

            // Disable default double-click zoom
            sb.AppendLine("    if(_isLeaflet) {");
            sb.AppendLine("      _nativeMap.doubleClickZoom.disable();");
            sb.AppendLine("      _nativeMap.on('mousemove', onMouseMove);");
            sb.AppendLine("      _nativeMap.on('dblclick', onDblClick);");
            sb.AppendLine("    } else if(typeof google !== 'undefined') {");
            sb.AppendLine("      _nativeMap.setOptions({ disableDoubleClickZoom: true });");
            sb.AppendLine("      google.maps.event.addListener(_nativeMap, 'mousemove', onMouseMove);");
            sb.AppendLine("      google.maps.event.addListener(_nativeMap, 'dblclick', onDblClick);");
            sb.AppendLine("    }");
            sb.AppendLine();




            // Create sidebar DOM
            sb.AppendLine("    var mapEl = document.getElementById('map');");
            sb.AppendLine("    if(mapEl) {");
            sb.AppendLine("      if(!mapEl.style.position || mapEl.style.position === 'static') mapEl.style.position = 'relative';");
            sb.AppendLine("      _sidebar = document.createElement('div');");
            sb.AppendLine("      _sidebar.id = 'liveAtcSidebar';");
            sb.AppendLine("      _sidebar.innerHTML = '<div class=\"sb-hdr\">' + (_sidebarCollapsed ? '\\u25B8 ' : '\\u25BE ') + 'LiveATC</div><div class=\"sb-list\"></div>';");
            sb.AppendLine("      if(!_active) _sidebar.style.display = 'none';");
            sb.AppendLine("      if(_sidebarCollapsed) _sidebar.querySelector('.sb-list').style.display = 'none';");
            sb.AppendLine("      mapEl.appendChild(_sidebar);");
            sb.AppendLine("      _sidebar.querySelector('.sb-hdr').addEventListener('click', function() {");
            sb.AppendLine("        _sidebarCollapsed = !_sidebarCollapsed;");
            sb.AppendLine("        _sidebar.querySelector('.sb-list').style.display = _sidebarCollapsed ? 'none' : '';");
            sb.AppendLine("        updateSidebar();");
            sb.AppendLine("        try { localStorage.setItem('vrsLiveATCCollapsed', _sidebarCollapsed ? '1' : '0'); } catch(ex) {}");
            sb.AppendLine("      });");
            sb.AppendLine("      _sidebar.querySelector('.sb-list').addEventListener('click', function(e) {");
            sb.AppendLine("        var row = e.target;");
            sb.AppendLine("        while(row && !row.classList.contains('sb-row')) row = row.parentElement;");
            sb.AppendLine("        if(row) {");
            sb.AppendLine("          var icao = row.getAttribute('data-icao');");
            sb.AppendLine("          window.open('https://www.liveatc.net/search/?icao=' + icao.toLowerCase(), '_blank');");
            sb.AppendLine("        }");
            sb.AppendLine("      });");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Hook map idle to update sidebar
            sb.AppendLine("    if(_mapPlugin.hookIdle) { _mapPlugin.hookIdle(updateSidebar); }");
            sb.AppendLine("    else if(_isLeaflet) { _nativeMap.on('moveend', updateSidebar); }");
            sb.AppendLine("    else if(typeof google !== 'undefined') { google.maps.event.addListener(_nativeMap, 'idle', updateSidebar); }");
            sb.AppendLine("    updateSidebar();");
            sb.AppendLine();
            sb.AppendLine("    return true;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Update sidebar with airports visible in current map bounds
            sb.AppendLine("  function updateSidebar() {");
            sb.AppendLine("    if(!_sidebar || !_mapPlugin || !_airports) return;");
            sb.AppendLine("    var b = _mapPlugin.getBounds();");
            sb.AppendLine("    if(!b) return;");
            sb.AppendLine("    var visible = [];");
            sb.AppendLine("    for(var i = 0; i < _airports.length; i++) {");
            sb.AppendLine("      var ap = _airports[i];");
            sb.AppendLine("      if(ap.a < b.brLat || ap.a > b.tlLat) continue;");
            sb.AppendLine("      if(b.tlLng <= b.brLng) { if(ap.o < b.tlLng || ap.o > b.brLng) continue; }");
            sb.AppendLine("      else { if(ap.o < b.tlLng && ap.o > b.brLng) continue; }");
            sb.AppendLine("      visible.push(ap);");
            sb.AppendLine("    }");
            sb.AppendLine("    visible.sort(function(a, b) { return a.i < b.i ? -1 : a.i > b.i ? 1 : 0; });");
            sb.AppendLine("    _sidebar.querySelector('.sb-hdr').textContent = (_sidebarCollapsed ? '\\u25B8 ' : '\\u25BE ') + 'LiveATC (' + visible.length + ')';");
            sb.AppendLine("    var html = '';");
            sb.AppendLine("    for(var i = 0; i < visible.length; i++) {");
            sb.AppendLine("      html += '<div class=\"sb-row\" data-icao=\"' + visible[i].i + '\"><span class=\"sb-icao\">' + visible[i].i + '</span></div>';");
            sb.AppendLine("    }");
            sb.AppendLine("    _sidebar.querySelector('.sb-list').innerHTML = html;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Show/hide sidebar based on _active state
            sb.AppendLine("  function toggleSidebar() {");
            sb.AppendLine("    if(!_sidebar) return;");
            sb.AppendLine("    _sidebar.style.display = _active ? 'flex' : 'none';");
            sb.AppendLine("    if(_active) updateSidebar();");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Add LiveATC toggle to settings menu (retry until menu exists)
            sb.AppendLine("  function tryAddMenuItem() {");
            sb.AppendLine("    if(!VRS.bootstrap || !VRS.bootstrap.pageSettings || !VRS.bootstrap.pageSettings.settingsMenu) { setTimeout(tryAddMenuItem, 500); return; }");
            sb.AppendLine("    VRS.bootstrap.pageSettings.settingsMenu.hookBeforeAddingFixedMenuItems(function(menu, menuItems) {");
            sb.AppendLine("      menuItems.push(new VRS.MenuItem({");
            sb.AppendLine("        name: 'liveAtcToggle',");
            sb.AppendLine("        labelKey: function() { return 'LiveATC'; },");
            sb.AppendLine("        checked: function() { return _active; },");
            sb.AppendLine("        clickCallback: function() {");
            sb.AppendLine("          _active = !_active;");
            sb.AppendLine("          if(!_active && _nearAirport) { _nearAirport = null; setCursor(''); }");
            sb.AppendLine("          toggleSidebar();");
            sb.AppendLine("          try { localStorage.setItem('vrsLiveATC', _active ? '1' : '0'); } catch(ex) {}");
            sb.AppendLine("        },");
            sb.AppendLine("        noAutoClose: true");
            sb.AppendLine("      }));");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine("  tryAddMenuItem();");
            sb.AppendLine();

            // Retry until map is ready
            sb.AppendLine("  function tryInit() {");
            sb.AppendLine("    if(!initMap()) setTimeout(tryInit, 500);");
            sb.AppendLine("  }");
            sb.AppendLine("  tryInit();");
            sb.AppendLine();

            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
