using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using Newtonsoft.Json;
using VirtualRadar.Interface;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.CustomLinks
{
    /// <summary>
    /// The entry point for the plugin that adds user-configurable links to the
    /// aircraft detail panel in the web UI.
    /// </summary>
    public class Plugin : IPlugin
    {
        /// <summary>
        /// The plugin's options.
        /// </summary>
        private Options _Options;

        /// <summary>
        /// The web site that we inject content into.
        /// </summary>
        private IWebSite _WebSite;

        /// <summary>
        /// The content injectors we've registered.
        /// </summary>
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();

        /// <summary>
        /// True if injectors are currently added to the web site.
        /// </summary>
        private bool _InjectorsActive;

        /// <summary>
        /// Gets the last initialised instance of the plugin object. At run-time only one plugin
        /// object gets created and initialised.
        /// </summary>
        public static Plugin Singleton { get; private set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Id { get { return "VirtualRadarServer.Plugin.CustomLinks"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string PluginFolder { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Name { get { return "Custom Links"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Version { get { return "3.0.0"; } }

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
        public void RegisterImplementations(InterfaceFactory.IClassFactory classFactory)
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
                    "CustomLinksPluginOptions.html",
                    "Custom Links",
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
                Status = String.Format("Enabled - {0} link(s)", options.Links.Count);
            }
        }

        /// <summary>
        /// Builds the inline JavaScript that registers custom links with VRS.
        /// </summary>
        private string BuildInjectScript()
        {
            var links = _Options?.Links ?? new List<LinkDefinition>();
            var jsonText = JsonConvert.SerializeObject(links);

            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.LinkRenderHandler || !VRS.linkRenderHandlers) return;");
            sb.AppendFormat("  var links = {0};\n", jsonText);
            sb.AppendLine("  for(var i = 0; i < links.length; ++i) {");
            sb.AppendLine("    (function(index, link) {");
            sb.AppendLine("      var siteName = 'CustomLink_' + index;");
            sb.AppendLine("      VRS.LinkSite[siteName] = siteName.toLowerCase();");
            sb.AppendLine("      VRS.linkRenderHandlers.push(");
            sb.AppendLine("        new VRS.LinkRenderHandler({");
            sb.AppendLine("          linkSite:        VRS.LinkSite[siteName],");
            sb.AppendLine("          displayOrder:    10000 + index,");
            sb.AppendLine("          canLinkAircraft: function(aircraft) {");
            sb.AppendLine("            var url = link.Url || '';");
            sb.AppendLine("            if(url.indexOf('{icao}') !== -1     && !aircraft.formatIcao())           return false;");
            sb.AppendLine("            if(url.indexOf('{reg}') !== -1      && !aircraft.formatRegistration())   return false;");
            sb.AppendLine("            if(url.indexOf('{callsign}') !== -1 && !aircraft.formatCallsign(false))  return false;");
            sb.AppendLine("            return true;");
            sb.AppendLine("          },");
            sb.AppendLine("          hasChanged: function(aircraft) {");
            sb.AppendLine("            return aircraft.icao.chg || aircraft.registration.chg || aircraft.callsign.chg;");
            sb.AppendLine("          },");
            sb.AppendLine("          title:    function() { return link.Name || 'Custom Link'; },");
            sb.AppendLine("          buildUrl: function(aircraft) {");
            sb.AppendLine("            var url = link.Url || '';");
            sb.AppendLine("            url = url.replace(/\\{icao\\}/g,     encodeURIComponent(aircraft.formatIcao()          || ''));");
            sb.AppendLine("            url = url.replace(/\\{reg\\}/g,      encodeURIComponent(aircraft.formatRegistration()  || ''));");
            sb.AppendLine("            url = url.replace(/\\{callsign\\}/g, encodeURIComponent(aircraft.formatCallsign(false) || ''));");
            sb.AppendLine("            return url;");
            sb.AppendLine("          },");
            sb.AppendLine("          target: 'customLink-' + index");
            sb.AppendLine("        })");
            sb.AppendLine("      );");
            sb.AppendLine("    })(i, links[i]);");
            sb.AppendLine("  }");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
