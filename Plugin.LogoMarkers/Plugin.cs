using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using Newtonsoft.Json;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.LogoMarkers
{
    /// <summary>
    /// The entry point for the plugin that swaps aircraft SVG icons with operator
    /// logo composites (logo + heading arrow) on the map.
    /// </summary>
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebServer _WebServer;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        private ConcurrentDictionary<string, byte[]> _LogoCache = new ConcurrentDictionary<string, byte[]>();
        private ConcurrentDictionary<string, byte[]> _ImageCache = new ConcurrentDictionary<string, byte[]>();
        private string _CachedAvailableJson;
        private DateTime _CachedAvailableTime;
        private readonly object _AvailableLock = new object();

        /// <summary>
        /// Gets the last initialised instance of the plugin object.
        /// </summary>
        public static Plugin Singleton { get; private set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Id { get { return "VirtualRadarServer.Plugin.LogoMarkers"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string PluginFolder { get; set; }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Name { get { return "Logo Markers"; } }

        /// <summary>
        /// See interface docs.
        /// </summary>
        public string Version { get { return "3.2.0"; } }

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
                    "LogoMarkersPluginOptions.html",
                    "Logo Markers",
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
                    _LogoCache.Clear();
                    _ImageCache.Clear();
                    _CachedAvailableJson = null;
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

            if(string.Equals(args.PathAndFile, "/LogoMarkers/Available.json", StringComparison.OrdinalIgnoreCase)) {
                HandleAvailableRequest(args);
            } else if(string.Equals(args.PathAndFile, "/LogoMarkers/Logo", StringComparison.OrdinalIgnoreCase)) {
                HandleLogoRequest(args);
            } else if(string.Equals(args.PathAndFile, "/LogoMarkers/Icon.png", StringComparison.OrdinalIgnoreCase)) {
                HandleIconRequest(args);
            }
        }

        /// <summary>
        /// Returns a JSON array of operator ICAOs that have logo BMP files.
        /// </summary>
        private void HandleAvailableRequest(RequestReceivedEventArgs args)
        {
            string json;
            lock(_AvailableLock) {
                if(_CachedAvailableJson == null || (DateTime.UtcNow - _CachedAvailableTime).TotalSeconds > 60) {
                    var folder = GetOperatorFlagsFolder();
                    var icaos = new List<string>();
                    if(!String.IsNullOrEmpty(folder) && Directory.Exists(folder)) {
                        foreach(var file in Directory.GetFiles(folder, "*.bmp")) {
                            icaos.Add(Path.GetFileNameWithoutExtension(file));
                        }
                    }
                    _CachedAvailableJson = JsonConvert.SerializeObject(icaos);
                    _CachedAvailableTime = DateTime.UtcNow;
                }
                json = _CachedAvailableJson;
            }

            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        /// <summary>
        /// Serves the raw operator logo BMP file. Image compositing (heading
        /// arrow overlay) is performed client-side via the Canvas API, removing
        /// the server-side dependency on System.Drawing / libgdiplus.
        /// </summary>
        private void HandleLogoRequest(RequestReceivedEventArgs args)
        {
            var icao = (args.QueryString["icao"] ?? "").Trim().ToUpperInvariant();

            if(String.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = System.Net.HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            var folder = GetOperatorFlagsFolder();
            var logoPath = String.IsNullOrEmpty(folder) ? null : Path.Combine(folder, icao + ".bmp");
            if(logoPath == null || !File.Exists(logoPath)) {
                args.Response.StatusCode = System.Net.HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            byte[] bmpBytes;
            if(!_LogoCache.TryGetValue(icao, out bmpBytes)) {
                bmpBytes = File.ReadAllBytes(logoPath);
                _LogoCache.TryAdd(icao, bmpBytes);
            }

            var responder = Factory.Resolve<IResponder>();
            responder.SendBinary(args.Request, args.Response, bmpBytes, MimeType.BitmapImage, true);
            args.Handled = true;
        }

        /// <summary>
        /// Serves a pre-composited PNG (heading arrow + operator logo) built
        /// server-side via System.Drawing. Used when ServerSideCompositing is
        /// enabled. Requires libgdiplus on Linux/Mono.
        /// </summary>
        private void HandleIconRequest(RequestReceivedEventArgs args)
        {
            var icao = (args.QueryString["icao"] ?? "").Trim().ToUpperInvariant();
            int hdg;
            int.TryParse(args.QueryString["hdg"] ?? "0", out hdg);
            hdg = ((hdg % 360) + 360) % 360;
            hdg = (int)(Math.Round(hdg / 5.0) * 5) % 360;
            var noArrow = !String.IsNullOrEmpty(args.QueryString["noarrow"]);

            if(String.IsNullOrEmpty(icao)) {
                args.Response.StatusCode = System.Net.HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            var folder = GetOperatorFlagsFolder();
            var logoPath = String.IsNullOrEmpty(folder) ? null : Path.Combine(folder, icao + ".bmp");
            if(logoPath == null || !File.Exists(logoPath)) {
                args.Response.StatusCode = System.Net.HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            var cacheKey = noArrow ? (icao + "_na") : (icao + "_" + hdg);
            byte[] pngBytes;
            if(!_ImageCache.TryGetValue(cacheKey, out pngBytes)) {
                pngBytes = BuildCompositeImage(logoPath, hdg, noArrow);
                _ImageCache.TryAdd(cacheKey, pngBytes);
            }

            var responder = Factory.Resolve<IResponder>();
            responder.SendBinary(args.Request, args.Response, pngBytes, MimeType.PngImage, true);
            args.Handled = true;
        }

        /// <summary>
        /// Composites a heading arrow on top of an operator logo BMP using
        /// System.Drawing and returns the result as PNG bytes.
        /// </summary>
        private byte[] BuildCompositeImage(string logoPath, int heading, bool noArrow)
        {
            using(var logoBmp = new Bitmap(logoPath)) {
                int arrowHeight = noArrow ? 0 : 20;
                int width = logoBmp.Width;
                int height = logoBmp.Height + arrowHeight;

                using(var composite = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                using(var g = Graphics.FromImage(composite)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    if(!noArrow) {
                        // Draw heading arrow
                        g.TranslateTransform(width / 2f, arrowHeight / 2f);
                        g.RotateTransform(heading);
                        var arrowPoints = new PointF[] {
                            new PointF( 0, -9),
                            new PointF(-5,  1),
                            new PointF(-1,  0),
                            new PointF(-1,  9),
                            new PointF( 1,  9),
                            new PointF( 1,  0),
                            new PointF( 5,  1),
                        };
                        g.FillPolygon(Brushes.White, arrowPoints);
                        g.DrawPolygon(Pens.Black, arrowPoints);
                        g.ResetTransform();
                    }

                    // Draw logo below arrow
                    g.DrawImage(logoBmp, 0, arrowHeight, logoBmp.Width, logoBmp.Height);

                    using(var ms = new MemoryStream()) {
                        composite.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the configured OperatorFlagsFolder from shared configuration.
        /// </summary>
        private string GetOperatorFlagsFolder()
        {
            var sharedConfig = Factory.ResolveSingleton<ISharedConfiguration>();
            var config = sharedConfig.Get();
            return config.BaseStationSettings.OperatorFlagsFolder;
        }

        /// <summary>
        /// Builds the inline JavaScript that monkey-patches the aircraft plotter
        /// to display operator logos with heading arrows.
        /// Mode 0 = normal aircraft icons
        /// Mode 1 = logos replace aircraft icons (with heading arrow)
        /// Mode 2 = normal aircraft icons + logo floating above each one
        /// </summary>
        private string BuildInjectScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.AircraftPlotter) return;");
            sb.AppendLine();

            // Compositing mode flag (emitted from server-side option)
            sb.AppendLine(String.Format("  var _serverSide = {0};", _Options.ServerSideCompositing ? "true" : "false"));
            sb.AppendLine();

            // State: _mode 0=normal icons, 1=logos replace icons, 2=logos above icons.
            // Default to 2 (Logos+Icon) so fresh users get the richest view; localStorage
            // overrides this for anyone who has previously cycled the toggle.
            sb.AppendLine("  var _mode = 2;");
            sb.AppendLine("  var _generation = 0;");
            sb.AppendLine("  var _availableLogos = null;");
            sb.AppendLine("  var _toggleBtn = null;");
            sb.AppendLine("  var _mapPlugin = null;");
            sb.AppendLine("  var _logoMarkers = {};");
            sb.AppendLine("  var _modeLabels = ['Icons', 'Logos', 'Logos+Icon'];");
            sb.AppendLine("  var _logosVisible = true;");
            sb.AppendLine();

            // Client-side logo image cache and compositing state
            sb.AppendLine("  var _logoImages = {};");
            sb.AppendLine("  var _logoLoading = {};");
            sb.AppendLine("  var _compositeCache = {};");
            sb.AppendLine();

            // Load saved state
            sb.AppendLine("  try { var s = localStorage.getItem('vrsLogoMarkers'); if(s !== null) { var n = parseInt(s,10); _mode = isNaN(n) ? 2 : n; } } catch(e) {}");
            sb.AppendLine("  if(_mode < 0 || _mode > 2) _mode = 2;");
            sb.AppendLine("  if(_mode > 0) _generation = 1;");
            sb.AppendLine("  try { if(localStorage.getItem('vrsLogosVisible') === '0') _logosVisible = false; } catch(e) {}");
            sb.AppendLine();

            // Fetch available logos
            sb.AppendLine("  function fetchAvailable() {");
            sb.AppendLine("    try {");
            sb.AppendLine("      var xhr = new XMLHttpRequest();");
            sb.AppendLine("      xhr.open('GET', 'LogoMarkers/Available.json', true);");
            sb.AppendLine("      xhr.onreadystatechange = function() {");
            sb.AppendLine("        if(xhr.readyState === 4 && xhr.status === 200) {");
            sb.AppendLine("          try {");
            sb.AppendLine("            var arr = JSON.parse(xhr.responseText);");
            sb.AppendLine("            var map = {};");
            sb.AppendLine("            for(var i = 0; i < arr.length; i++) map[arr[i].toUpperCase()] = true;");
            sb.AppendLine("            _availableLogos = map;");
            sb.AppendLine("          } catch(e) {}");
            sb.AppendLine("        }");
            sb.AppendLine("      };");
            sb.AppendLine("      xhr.send();");
            sb.AppendLine("    } catch(e) {}");
            sb.AppendLine("  }");
            sb.AppendLine("  fetchAvailable();");
            sb.AppendLine("  setInterval(fetchAvailable, 60000);");
            sb.AppendLine();

            // Client-side compositing: draws heading arrow + logo onto a canvas,
            // returns a data URL. Loads the logo BMP from the server on first use.
            sb.AppendLine("  function getCompositeUrl(icao, hdg, noArrow) {");
            sb.AppendLine("    var key = noArrow ? (icao + '_na') : (icao + '_' + hdg);");
            sb.AppendLine("    if(_compositeCache[key]) return _compositeCache[key];");
            sb.AppendLine("    var img = _logoImages[icao];");
            sb.AppendLine("    if(!img) {");
            sb.AppendLine("      if(!_logoLoading[icao]) {");
            sb.AppendLine("        _logoLoading[icao] = true;");
            sb.AppendLine("        var newImg = new Image();");
            sb.AppendLine("        newImg.onload = function() {");
            sb.AppendLine("          _logoImages[icao] = newImg;");
            sb.AppendLine("          delete _logoLoading[icao];");
            sb.AppendLine("          _generation++;");
            sb.AppendLine("        };");
            sb.AppendLine("        newImg.onerror = function() {");
            sb.AppendLine("          delete _logoLoading[icao];");
            sb.AppendLine("        };");
            sb.AppendLine("        newImg.src = 'LogoMarkers/Logo?icao=' + encodeURIComponent(icao);");
            sb.AppendLine("      }");
            sb.AppendLine("      return null;");
            sb.AppendLine("    }");
            sb.AppendLine("    var arrowH = noArrow ? 0 : 20;");
            sb.AppendLine("    var cvs = document.createElement('canvas');");
            sb.AppendLine("    cvs.width = img.width;");
            sb.AppendLine("    cvs.height = img.height + arrowH;");
            sb.AppendLine("    var ctx = cvs.getContext('2d');");
            sb.AppendLine("    if(!noArrow) {");
            sb.AppendLine("      ctx.save();");
            sb.AppendLine("      ctx.translate(cvs.width / 2, arrowH / 2);");
            sb.AppendLine("      ctx.rotate(hdg * Math.PI / 180);");
            sb.AppendLine("      ctx.beginPath();");
            sb.AppendLine("      ctx.moveTo(0, -9);");
            sb.AppendLine("      ctx.lineTo(-5, 1);");
            sb.AppendLine("      ctx.lineTo(-1, 0);");
            sb.AppendLine("      ctx.lineTo(-1, 9);");
            sb.AppendLine("      ctx.lineTo(1, 9);");
            sb.AppendLine("      ctx.lineTo(1, 0);");
            sb.AppendLine("      ctx.lineTo(5, 1);");
            sb.AppendLine("      ctx.closePath();");
            sb.AppendLine("      ctx.fillStyle = '#ffffff';");
            sb.AppendLine("      ctx.fill();");
            sb.AppendLine("      ctx.strokeStyle = '#000000';");
            sb.AppendLine("      ctx.lineWidth = 1;");
            sb.AppendLine("      ctx.stroke();");
            sb.AppendLine("      ctx.restore();");
            sb.AppendLine("    }");
            sb.AppendLine("    ctx.drawImage(img, 0, arrowH);");
            sb.AppendLine("    var dataUrl = cvs.toDataURL('image/png');");
            sb.AppendLine("    _compositeCache[key] = dataUrl;");
            sb.AppendLine("    return dataUrl;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Server-side icon URL builder
            sb.AppendLine("  function getIconUrl(icao, hdg, noArrow) {");
            sb.AppendLine("    if(noArrow) return 'LogoMarkers/Icon.png?icao=' + encodeURIComponent(icao) + '&hdg=0&noarrow=1';");
            sb.AppendLine("    return 'LogoMarkers/Icon.png?icao=' + encodeURIComponent(icao) + '&hdg=' + hdg;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Unified image URL getter that branches on compositing mode
            sb.AppendLine("  function getImageUrl(icao, hdg, noArrow) {");
            sb.AppendLine("    return _serverSide ? getIconUrl(icao, hdg, noArrow) : getCompositeUrl(icao, hdg, noArrow);");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Scale factor for logos based on zoom level
            sb.AppendLine("  function logoScale(zoom) {");
            sb.AppendLine("    return Math.min(1, Math.max(0.3, (zoom - 2) / 8));");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Remove all secondary logo markers (used when leaving mode 2)
            sb.AppendLine("  function removeAllLogoMarkers() {");
            sb.AppendLine("    if(!_mapPlugin) return;");
            sb.AppendLine("    for(var id in _logoMarkers) {");
            sb.AppendLine("      try { _mapPlugin.destroyMarker(id); } catch(e) {}");
            sb.AppendLine("    }");
            sb.AppendLine("    _logoMarkers = {};");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Monkey-patch haveIconDetailsChanged
            sb.AppendLine("  var _origHaveChanged = VRS.AircraftPlotter.prototype.haveIconDetailsChanged;");
            sb.AppendLine("  VRS.AircraftPlotter.prototype.haveIconDetailsChanged = function(details, mapZoomLevel) {");
            sb.AppendLine("    if(details && (details._logoGen !== _generation || details._logoZoom !== mapZoomLevel)) return true;");
            sb.AppendLine("    return _origHaveChanged.call(this, details, mapZoomLevel);");
            sb.AppendLine("  };");
            sb.AppendLine();

            // Monkey-patch createIcon
            sb.AppendLine("  var _origCreateIcon = VRS.AircraftPlotter.prototype.createIcon;");
            sb.AppendLine("  VRS.AircraftPlotter.prototype.createIcon = function(details, mapZoomLevel, isSelectedAircraft) {");
            sb.AppendLine("    if(details) { details._logoGen = _generation; details._logoZoom = mapZoomLevel; }");
            // Mode 1: replace aircraft icon with logo + arrow composite
            sb.AppendLine("    if(_mode === 1 && _availableLogos && details && details.aircraft) {");
            sb.AppendLine("      var opIcao = details.aircraft.operatorIcao ? details.aircraft.operatorIcao.val : null;");
            sb.AppendLine("      if(opIcao && _availableLogos[opIcao.toUpperCase()]) {");
            sb.AppendLine("        var hdg = Math.round((details.aircraft.heading ? details.aircraft.heading.val || 0 : 0) / 5) * 5;");
            sb.AppendLine("        var s = logoScale(mapZoomLevel);");
            sb.AppendLine("        var w = Math.round(85 * s);");
            sb.AppendLine("        var cacheKey = opIcao + '_' + hdg;");
            sb.AppendLine("        if(cacheKey === details._logoCacheKey && details._logoW === w) return null;");
            sb.AppendLine("        var url = getImageUrl(opIcao, hdg, false);");
            sb.AppendLine("        if(!url) return _origCreateIcon.call(this, details, mapZoomLevel, isSelectedAircraft);");
            sb.AppendLine("        details._logoCacheKey = cacheKey;");
            sb.AppendLine("        details.iconUrl = url;");
            sb.AppendLine("        details._logoW = w;");
            sb.AppendLine("        var h = Math.round(40 * s);");
            sb.AppendLine("        var size = { width: w, height: h };");
            sb.AppendLine("        return new VRS.MapIcon(url, size, { x: Math.round(w / 2), y: Math.round(h * 0.75) }, { x: 0, y: 0 }, size, null);");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            // Mode 0 and 2: use normal aircraft icon
            sb.AppendLine("    return _origCreateIcon.call(this, details, mapZoomLevel, isSelectedAircraft);");
            sb.AppendLine("  };");
            sb.AppendLine();

            // Monkey-patch refreshAircraftMarker to manage mode-2 logo markers
            sb.AppendLine("  var _origRefresh = VRS.AircraftPlotter.prototype.refreshAircraftMarker;");
            sb.AppendLine("  VRS.AircraftPlotter.prototype.refreshAircraftMarker = function(aircraft, forceRefresh, ignoreBounds, bounds, mapZoomLevel, isSelectedAircraft) {");
            sb.AppendLine("    _origRefresh.call(this, aircraft, forceRefresh, ignoreBounds, bounds, mapZoomLevel, isSelectedAircraft);");
            sb.AppendLine("    if(!_mapPlugin || !_availableLogos || !aircraft) return;");
            sb.AppendLine("    var markerId = 'logo_' + aircraft.icao.val;");
            sb.AppendLine("    if(_mode === 2 && aircraft.hasPosition() && !aircraft.positionStale.val) {");
            sb.AppendLine("      var opIcao = aircraft.operatorIcao ? aircraft.operatorIcao.val : null;");
            sb.AppendLine("      if(opIcao && _availableLogos[opIcao.toUpperCase()]) {");
            sb.AppendLine("        var pos = aircraft.getPosition();");
            sb.AppendLine("        var url = getImageUrl(opIcao, 0, true);");
            sb.AppendLine("        if(!url) return;");
            sb.AppendLine("        var s2 = logoScale(mapZoomLevel);");
            sb.AppendLine("        var w2 = Math.round(85 * s2), h2 = Math.round(20 * s2);");
            sb.AppendLine("        var size = { width: w2, height: h2 };");
            sb.AppendLine("        var icon = new VRS.MapIcon(url, size, { x: Math.round(w2 / 2), y: Math.round(h2 * 1.5) }, { x: 0, y: 0 }, size, null);");
            sb.AppendLine("        var existing = _logoMarkers[markerId];");
            sb.AppendLine("        if(existing && existing._lastW !== w2) {");
            sb.AppendLine("          try { _mapPlugin.destroyMarker(markerId); } catch(e) {}");
            sb.AppendLine("          delete _logoMarkers[markerId];");
            sb.AppendLine("          existing = null;");
            sb.AppendLine("        }");
            sb.AppendLine("        if(existing) {");
            sb.AppendLine("          existing.setPosition(pos);");
            sb.AppendLine("          if(existing._lastLogoKey !== opIcao) { existing.setIcon(icon); existing._lastLogoKey = opIcao; }");
            sb.AppendLine("        } else {");
            sb.AppendLine("          var m = _mapPlugin.addMarker(markerId, { clickable: false, draggable: false, flat: true, icon: icon, visible: true, position: pos, zIndex: 99 });");
            sb.AppendLine("          if(m) { m._lastLogoKey = opIcao; m._lastW = w2; _logoMarkers[markerId] = m; }");
            sb.AppendLine("        }");
            sb.AppendLine("        return;");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            // Not mode 2, or aircraft has no logo -- remove any stale logo marker
            sb.AppendLine("    if(_logoMarkers[markerId]) {");
            sb.AppendLine("      try { _mapPlugin.destroyMarker(markerId); } catch(e) {}");
            sb.AppendLine("      delete _logoMarkers[markerId];");
            sb.AppendLine("    }");
            sb.AppendLine("  };");
            sb.AppendLine();

            // Also patch removeDetails to clean up logo markers when aircraft are removed
            sb.AppendLine("  var _origRemoveDetails = VRS.AircraftPlotter.prototype.removeDetails;");
            sb.AppendLine("  VRS.AircraftPlotter.prototype.removeDetails = function(details) {");
            sb.AppendLine("    if(details && details.aircraft && details.aircraft.icao) {");
            sb.AppendLine("      var markerId = 'logo_' + details.aircraft.icao.val;");
            sb.AppendLine("      if(_logoMarkers[markerId]) {");
            sb.AppendLine("        try { _mapPlugin.destroyMarker(markerId); } catch(e) {}");
            sb.AppendLine("        delete _logoMarkers[markerId];");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("    return _origRemoveDetails.call(this, details);");
            sb.AppendLine("  };");
            sb.AppendLine();

            // Toggle button text
            sb.AppendLine("  function updateToggleText() {");
            sb.AppendLine("    if(_toggleBtn) _toggleBtn.textContent = _modeLabels[_mode];");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Create map toggle button
            sb.AppendLine("  function addMapToggle() {");
            sb.AppendLine("    var mapJQ = $('#map');");
            sb.AppendLine("    if(!mapJQ.length || !VRS.jQueryUIHelper || !VRS.jQueryUIHelper.getMapPlugin) return false;");
            sb.AppendLine("    _mapPlugin = VRS.jQueryUIHelper.getMapPlugin(mapJQ);");
            sb.AppendLine("    if(!_mapPlugin || !_mapPlugin.isOpen()) { _mapPlugin = null; return false; }");
            sb.AppendLine("    _toggleBtn = document.createElement('div');");
            sb.AppendLine("    _toggleBtn.className = 'mapButton';");
            sb.AppendLine("    _toggleBtn.style.cursor = 'pointer';");
            sb.AppendLine("    _toggleBtn.style.userSelect = 'none';");
            sb.AppendLine("    _toggleBtn.style.fontWeight = 'bold';");
            sb.AppendLine("    updateToggleText();");
            sb.AppendLine("    _toggleBtn.addEventListener('click', function(e) {");
            sb.AppendLine("      var oldMode = _mode;");
            sb.AppendLine("      _mode = (_mode + 1) % 3;");
            sb.AppendLine("      _generation++;");
            sb.AppendLine("      if(oldMode === 2) removeAllLogoMarkers();");
            sb.AppendLine("      updateToggleText();");
            sb.AppendLine("      try { localStorage.setItem('vrsLogoMarkers', '' + _mode); } catch(ex) {}");
            sb.AppendLine("      e.stopPropagation();");
            sb.AppendLine("    });");
            sb.AppendLine("    _mapPlugin.addControl(_toggleBtn, VRS.MapPosition.TopLeft);");

            // Move logos button into the menu button's leaflet-control wrapper so they sit side by side
            sb.AppendLine("    try {");
            sb.AppendLine("      var menuSpan = null;");
            sb.AppendLine("      var spans = document.querySelectorAll('.leaflet-top.leaflet-left span');");
            sb.AppendLine("      for(var si = 0; si < spans.length; si++) { if(spans[si].textContent === 'Menu') { menuSpan = spans[si]; break; } }");
            sb.AppendLine("      if(menuSpan) {");
            sb.AppendLine("        var menuCtrl = menuSpan;");
            sb.AppendLine("        while(menuCtrl && (!menuCtrl.classList || !menuCtrl.classList.contains('leaflet-control'))) menuCtrl = menuCtrl.parentElement;");
            sb.AppendLine("        if(menuCtrl) {");
            sb.AppendLine("          var oldWrapper = _toggleBtn.parentElement;");
            sb.AppendLine("          menuCtrl.style.display = 'flex';");
            sb.AppendLine("          menuCtrl.style.alignItems = 'flex-start';");
            sb.AppendLine("          menuCtrl.style.gap = '5px';");
            sb.AppendLine("          _toggleBtn.style.margin = '0';");
            sb.AppendLine("          _toggleBtn.style.marginTop = '2px';");
            sb.AppendLine("          _toggleBtn.style.padding = '3px 6px';");
            sb.AppendLine("          menuCtrl.appendChild(_toggleBtn);");
            sb.AppendLine("          if(oldWrapper && oldWrapper !== menuCtrl && !oldWrapper.firstChild) oldWrapper.parentElement.removeChild(oldWrapper);");
            sb.AppendLine("        }");
            sb.AppendLine("      }");
            sb.AppendLine("    } catch(ex) {}");
            sb.AppendLine("    if(!_logosVisible) {");
            sb.AppendLine("      _toggleBtn.style.display = 'none';");
            sb.AppendLine("    }");
            sb.AppendLine("    return true;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Retry until map is ready
            sb.AppendLine("  function tryAddToggle() {");
            sb.AppendLine("    if(!addMapToggle()) setTimeout(tryAddToggle, 500);");
            sb.AppendLine("  }");
            sb.AppendLine("  tryAddToggle();");
            sb.AppendLine();

            // Add Logos Toggle checkbox to settings menu
            sb.AppendLine("  function tryAddMenuItem() {");
            sb.AppendLine("    if(!VRS.bootstrap || !VRS.bootstrap.pageSettings || !VRS.bootstrap.pageSettings.settingsMenu) { setTimeout(tryAddMenuItem, 500); return; }");
            sb.AppendLine("    VRS.bootstrap.pageSettings.settingsMenu.hookBeforeAddingFixedMenuItems(function(menu, menuItems) {");
            sb.AppendLine("      menuItems.push(new VRS.MenuItem({");
            sb.AppendLine("        name: 'logosToggle',");
            sb.AppendLine("        labelKey: function() { return 'Logos Toggle'; },");
            sb.AppendLine("        checked: function() { return _logosVisible; },");
            sb.AppendLine("        clickCallback: function() {");
            sb.AppendLine("          _logosVisible = !_logosVisible;");
            sb.AppendLine("          if(_toggleBtn) _toggleBtn.style.display = _logosVisible ? '' : 'none';");
            sb.AppendLine("          try { localStorage.setItem('vrsLogosVisible', _logosVisible ? '1' : '0'); } catch(ex) {}");
            sb.AppendLine("        },");
            sb.AppendLine("        noAutoClose: true");
            sb.AppendLine("      }));");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine("  tryAddMenuItem();");
            sb.AppendLine();

            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
