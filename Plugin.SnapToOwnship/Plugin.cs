using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.SnapToOwnship
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebSite _WebSite;
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
                Status = String.Format("Enabled - ICAO: {0}", string.IsNullOrEmpty(options.OwnshipIcao) ? "(not set)" : options.OwnshipIcao.ToUpperInvariant());
            }
        }

        private string BuildInjectScript()
        {
            var icao = (_Options?.OwnshipIcao ?? "").Trim().ToUpperInvariant();

            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(typeof VRS === 'undefined') return;");
            sb.AppendFormat("  var ownshipIcao = '{0}';\n", icao.Replace("'", "\\'"));
            sb.AppendLine("  if(!ownshipIcao) return;");
            sb.AppendLine();
            sb.AppendLine("  VRS.globalDispatch.hook(VRS.globalEvent.bootstrapCreated, function(bootstrap) {");
            sb.AppendLine("    bootstrap.hookInitialised(function(pageSettings) {");
            sb.AppendLine("      var btn = document.createElement('button');");
            sb.AppendLine("      btn.textContent = 'Snap to Ownship';");
            sb.AppendLine("      btn.style.cssText = 'padding:8px 16px;background:#3366cc;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:13px;box-shadow:0 2px 6px rgba(0,0,0,0.3);';");
            sb.AppendLine("      btn.onmouseenter = function() { btn.style.background='#2255bb'; };");
            sb.AppendLine("      btn.onmouseleave = function() { btn.style.background='#3366cc'; };");
            sb.AppendLine("      btn.onclick = function(e) {");
            sb.AppendLine("        e.stopPropagation();");
            sb.AppendLine("        var aircraft = pageSettings.aircraftList.findAircraftByIcao(ownshipIcao);");
            sb.AppendLine("        if(aircraft) {");
            sb.AppendLine("          pageSettings.aircraftList.setSelectedAircraft(aircraft, false);");
            sb.AppendLine("          if(pageSettings.mapPlugin && aircraft.latitude.val && aircraft.longitude.val) {");
            sb.AppendLine("            pageSettings.mapPlugin.setCenter({ lat: aircraft.latitude.val, lng: aircraft.longitude.val });");
            sb.AppendLine("          }");
            sb.AppendLine("          return;");
            sb.AppendLine("        }");
            sb.AppendLine("        $.ajax({");
            sb.AppendLine("          url: 'AircraftList.json',");
            sb.AppendLine("          method: 'GET',");
            sb.AppendLine("          dataType: 'json',");
            sb.AppendLine("          success: function(data) {");
            sb.AppendLine("            var acList = data.acList || [];");
            sb.AppendLine("            var found = null;");
            sb.AppendLine("            for(var i = 0; i < acList.length; i++) {");
            sb.AppendLine("              if(acList[i].Icao === ownshipIcao) { found = acList[i]; break; }");
            sb.AppendLine("            }");
            sb.AppendLine("            if(found && found.Lat != null && found.Long != null && pageSettings.mapPlugin) {");
            sb.AppendLine("              pageSettings.mapPlugin.setCenter({ lat: found.Lat, lng: found.Long });");
            sb.AppendLine("              var retries = 0;");
            sb.AppendLine("              var trySelect = function() {");
            sb.AppendLine("                var ac = pageSettings.aircraftList.findAircraftByIcao(ownshipIcao);");
            sb.AppendLine("                if(ac) { pageSettings.aircraftList.setSelectedAircraft(ac, false); }");
            sb.AppendLine("                else if(retries++ < 10) { setTimeout(trySelect, 1000); }");
            sb.AppendLine("              };");
            sb.AppendLine("              setTimeout(trySelect, 1000);");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("      };");
            sb.AppendLine("      var btnJQ = $(btn);");
            sb.AppendLine("      if(pageSettings.mapPlugin) {");
            sb.AppendLine("        pageSettings.mapPlugin.addControl(btnJQ, VRS.MapPosition.BottomRight);");
            sb.AppendLine("      }");
            sb.AppendLine("    });");
            sb.AppendLine("  });");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
