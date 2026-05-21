using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.DeselectAircraft
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.DeselectAircraft"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Deselect Aircraft"; } }
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

        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "DeselectAircraftPluginOptions.html",
                    "Deselect Aircraft",
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
                Status = "Enabled";
            }
        }

        private string BuildInjectScript()
        {
            // The injected script:
            //   - Adds a "Deselect" button at the map's BottomLeft control slot.
            //   - Starts hidden and only becomes visible while an aircraft is selected,
            //     via hookSelectedAircraftChanged on the page's aircraftList.
            //   - On click, clears the selection by calling setSelectedAircraft(null, false).
            // We resolve pageSettings / aircraftList / mapPlugin lazily because hookInitialised
            // can fire before mapLoaded() populates them, and references don't survive map
            // provider switches (Google <-> Leaflet via TileServerMBTiles).
            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(typeof VRS === 'undefined' || typeof $ === 'undefined') return;");
            sb.AppendLine();
            sb.AppendLine("  function getPageSettings() {");
            sb.AppendLine("    return (VRS.bootstrap && VRS.bootstrap.pageSettings) || null;");
            sb.AppendLine("  }");
            sb.AppendLine("  function getAircraftList() {");
            sb.AppendLine("    var ps = getPageSettings();");
            sb.AppendLine("    return (ps && ps.aircraftList) || null;");
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
            sb.AppendLine("  var _btn = null;");
            sb.AppendLine();
            sb.AppendLine("  function buildButton() {");
            sb.AppendLine("    var btn = document.createElement('button');");
            sb.AppendLine("    btn.type = 'button';");
            sb.AppendLine("    btn.textContent = 'Deselect Aircraft';");
            sb.AppendLine("    btn.title = 'Deselect the currently-selected aircraft';");
            sb.AppendLine("    btn.style.cssText = 'padding:8px 16px;background:#cc3333;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:13px;box-shadow:0 2px 6px rgba(0,0,0,0.3);display:none;';");
            sb.AppendLine("    btn.onmouseenter = function() { btn.style.background='#bb2222'; };");
            sb.AppendLine("    btn.onmouseleave = function() { btn.style.background='#cc3333'; };");
            // jQuery .on('click') survives any future DOM manipulation and keeps event bubbling consistent.
            sb.AppendLine("    $(btn).on('click', function(e) {");
            sb.AppendLine("      e.preventDefault();");
            sb.AppendLine("      e.stopPropagation();");
            sb.AppendLine("      var al = getAircraftList();");
            sb.AppendLine("      if(al && al.setSelectedAircraft) {");
            sb.AppendLine("        try { al.setSelectedAircraft(null, false); } catch(err) { console.warn('[DeselectAircraft] setSelectedAircraft failed: ' + err); }");
            sb.AppendLine("      }");
            sb.AppendLine("    });");
            // Leaflet: clicks on .leaflet-control descendants still propagate to the map by default,
            // which can trigger pan / marker handlers. Suppress that so the button click is local.
            sb.AppendLine("    try {");
            sb.AppendLine("      if(typeof L !== 'undefined' && L.DomEvent) {");
            sb.AppendLine("        L.DomEvent.disableClickPropagation(btn);");
            sb.AppendLine("        L.DomEvent.disableScrollPropagation(btn);");
            sb.AppendLine("      }");
            sb.AppendLine("    } catch(e) {}");
            sb.AppendLine("    return btn;");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  function updateVisibility() {");
            sb.AppendLine("    if(!_btn) return;");
            sb.AppendLine("    var al = getAircraftList();");
            sb.AppendLine("    var sel = (al && al.getSelectedAircraft) ? al.getSelectedAircraft() : null;");
            sb.AppendLine("    _btn.style.display = sel ? '' : 'none';");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  var _hookedSelectionChange = false;");
            sb.AppendLine("  function hookSelectionChange() {");
            sb.AppendLine("    if(_hookedSelectionChange) return true;");
            sb.AppendLine("    var al = getAircraftList();");
            sb.AppendLine("    if(!al || !al.hookSelectedAircraftChanged) return false;");
            sb.AppendLine("    try { al.hookSelectedAircraftChanged(updateVisibility); _hookedSelectionChange = true; return true; }");
            sb.AppendLine("    catch(e) { return false; }");
            sb.AppendLine("  }");
            sb.AppendLine();
            // Add the button to the map's BottomLeft control slot once the map plugin is ready.
            sb.AppendLine("  var _added = false;");
            sb.AppendLine("  function tryAddButton() {");
            sb.AppendLine("    if(_added) return true;");
            sb.AppendLine("    var mp = getMapPlugin();");
            sb.AppendLine("    if(!mp || !mp.addControl) return false;");
            sb.AppendLine("    try {");
            sb.AppendLine("      _btn = buildButton();");
            sb.AppendLine("      mp.addControl($(_btn), VRS.MapPosition.BottomLeft);");
            sb.AppendLine("      _added = true;");
            sb.AppendLine("      return true;");
            sb.AppendLine("    } catch(e) { console.warn('[DeselectAircraft] addControl failed: ' + e); return false; }");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  function pollInit() {");
            sb.AppendLine("    var addedOk = tryAddButton();");
            sb.AppendLine("    var hookedOk = hookSelectionChange();");
            sb.AppendLine("    if(addedOk) updateVisibility();");
            sb.AppendLine("    if(!addedOk || !hookedOk) setTimeout(pollInit, 500);");
            sb.AppendLine("  }");
            sb.AppendLine();
            sb.AppendLine("  if(VRS.globalDispatch && VRS.globalEvent && VRS.globalEvent.bootstrapCreated) {");
            sb.AppendLine("    VRS.globalDispatch.hook(VRS.globalEvent.bootstrapCreated, function(bootstrap) {");
            sb.AppendLine("      bootstrap.hookInitialised(function() { pollInit(); });");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            // Belt-and-braces: if bootstrapCreated already fired before this script ran, poll anyway.
            sb.AppendLine("  setTimeout(pollInit, 1000);");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
