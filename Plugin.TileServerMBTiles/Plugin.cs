using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.TileServerMBTiles
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebServer _WebServer;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;

        // One reader per .mbtiles file (keyed by sanitised filename without extension)
        private ConcurrentDictionary<string, MBTilesReader> _Readers
            = new ConcurrentDictionary<string, MBTilesReader>(StringComparer.OrdinalIgnoreCase);

        // Display name per key
        private ConcurrentDictionary<string, string> _DisplayNames
            = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // URL pattern: /TileServerMBTiles/{sourceKey}/{z}/{x}/{y}.{ext}
        private static readonly Regex _TileUrlPattern = new Regex(
            @"^/TileServerMBTiles/([^/]+)/(\d+)/(\d+)/(\d+)\.\w+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.TileServerMBTiles"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Tile Server MBTiles"; } }
        public string Version { get { return "1.0.0"; } }
        public bool HasOptions { get { return true; } }

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

        public event EventHandler StatusChanged;

        protected virtual void OnStatusChanged(EventArgs args)
        {
            EventHelper.Raise(StatusChanged, this, args);
        }

        public void RegisterImplementations(IClassFactory classFactory)
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

        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "TileServerMBTilesPluginOptions.html",
                    "Tile Server MBTiles",
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

            DisposeAllReaders();
        }

        public void ShowWinFormsOptionsUI()
        {
            using(var dialog = new WinForms.OptionsView()) {
                dialog.Options = OptionsStorage.Load(this);

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    DisposeAllReaders();
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
                DisposeAllReaders();
                Status = "Disabled";
                StatusDescription = "";
            } else {
                if(!_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.AddHtmlContentInjector(injector);
                    }
                    _InjectorsActive = true;
                }

                // Enumerate .mbtiles files and create a reader for each
                DisposeAllReaders();
                var folderPath = options.FolderPath ?? "";
                if(Directory.Exists(folderPath)) {
                    var files = Directory.GetFiles(folderPath, "*.mbtiles")
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach(var file in files) {
                        var displayName = Path.GetFileNameWithoutExtension(file);
                        var key = SanitiseSourceName(displayName);
                        if(!string.IsNullOrEmpty(key) && !_Readers.ContainsKey(key)) {
                            _Readers[key] = new MBTilesReader(file, options.IsTms);
                            _DisplayNames[key] = displayName;
                        }
                    }
                }

                Status = "Enabled";
                StatusDescription = $"{_Readers.Count} map(s) found in folder";
            }
        }

        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;
            if(!(_Options?.Enabled ?? false)) return;

            // Check for tile source list endpoint
            if(string.Equals(args.PathAndFile, "/TileServerMBTiles/Sources.json", StringComparison.OrdinalIgnoreCase)) {
                HandleSourcesRequest(args);
                return;
            }

            // Check for tile request
            var match = _TileUrlPattern.Match(args.PathAndFile);
            if(match.Success) {
                HandleTileRequest(args, match);
            }
        }

        private void HandleSourcesRequest(RequestReceivedEventArgs args)
        {
            var sources = new List<object>();

            foreach(var kvp in _Readers) {
                var key = kvp.Key;
                var reader = kvp.Value;
                var meta = reader.GetMetadata();

                string displayName;
                if(!_DisplayNames.TryGetValue(key, out displayName)) {
                    displayName = key;
                }

                string format;
                meta.TryGetValue("format", out format);

                sources.Add(new {
                    name    = displayName,
                    key     = key,
                    format  = format ?? "png",
                    isTms   = _Options?.IsTms ?? true,
                });
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(sources);
            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        private void HandleTileRequest(RequestReceivedEventArgs args, Match match)
        {
            var sourceName = match.Groups[1].Value;
            int z, x, y;
            if(!int.TryParse(match.Groups[2].Value, out z) ||
               !int.TryParse(match.Groups[3].Value, out x) ||
               !int.TryParse(match.Groups[4].Value, out y)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            MBTilesReader reader;
            if(!_Readers.TryGetValue(sourceName, out reader)) {
                args.Response.StatusCode = HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            byte[] tileData = null;
            var maxZoom = reader.MaxZoom;

            if(z <= maxZoom) {
                tileData = reader.GetTile(z, x, y);
            } else {
                // Overzoom: fetch the parent tile at maxZoom, then crop and
                // scale the sub-region that corresponds to the requested tile.
                var zoomDiff = z - maxZoom;
                var scaledX = x >> zoomDiff;
                var scaledY = y >> zoomDiff;
                var parentData = reader.GetTile(maxZoom, scaledX, scaledY);

                if(parentData != null) {
                    tileData = CropAndScaleTile(parentData, x, y, zoomDiff);
                }
            }

            if(tileData == null) {
                args.Response.StatusCode = HttpStatusCode.NotFound;
                args.Handled = true;
                return;
            }

            var mimeType = DetectTileMimeType(tileData);

            var responder = Factory.Resolve<IResponder>();
            responder.SendBinary(args.Request, args.Response, tileData, mimeType, true);
            args.Handled = true;
        }

        /// <summary>
        /// Crops the sub-region of a parent tile that corresponds to the
        /// requested overzoom coordinates, then scales it to 256x256.
        /// </summary>
        private byte[] CropAndScaleTile(byte[] parentData, int x, int y, int zoomDiff)
        {
            const int TileSize = 256;

            try {
                using(var ms = new MemoryStream(parentData))
                using(var parentImg = new Bitmap(ms)) {
                    // How many child tiles fit in one parent tile at this zoom diff
                    var scale = 1 << zoomDiff;

                    // Which sub-tile within the parent (0..scale-1)
                    var subX = x & (scale - 1);
                    var subY = y & (scale - 1);

                    // Size of the sub-region in parent pixels
                    var cropSize = parentImg.Width / scale;
                    if(cropSize < 1) cropSize = 1;

                    var srcRect = new Rectangle(subX * cropSize, subY * cropSize, cropSize, cropSize);
                    var destRect = new Rectangle(0, 0, TileSize, TileSize);

                    using(var result = new Bitmap(TileSize, TileSize, PixelFormat.Format32bppArgb))
                    using(var g = Graphics.FromImage(result)) {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(parentImg, destRect, srcRect, GraphicsUnit.Pixel);

                        using(var outMs = new MemoryStream()) {
                            result.Save(outMs, ImageFormat.Png);
                            return outMs.ToArray();
                        }
                    }
                }
            } catch {
                return null;
            }
        }

        private string DetectTileMimeType(byte[] data)
        {
            if(data.Length >= 8) {
                // PNG: 89 50 4E 47
                if(data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                    return MimeType.PngImage;
                // JPEG: FF D8 FF
                if(data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
                    return MimeType.JpegImage;
                // GZIP (for PBF/vector tiles): 1F 8B
                if(data[0] == 0x1F && data[1] == 0x8B)
                    return "application/x-protobuf";
            }
            return MimeType.PngImage;
        }

        /// <summary>
        /// Makes a source name safe for use in URLs.
        /// </summary>
        private static string SanitiseSourceName(string name)
        {
            return Regex.Replace((name ?? "").Trim(), @"[^a-zA-Z0-9_-]", "_");
        }

        private void DisposeAllReaders()
        {
            foreach(var kvp in _Readers) {
                kvp.Value.Dispose();
            }
            _Readers.Clear();
            _DisplayNames.Clear();
        }

        /// <summary>
        /// Builds JavaScript that adds MBTiles sources as base map options in VRS.
        /// When an MBTiles source is selected it replaces the default base tile layer.
        /// A select dropdown is added to the map for switching between sources.
        /// </summary>
        private string BuildInjectScript()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(typeof VRS === 'undefined' || typeof L === 'undefined') return;");
            sb.AppendLine();
            sb.AppendLine("  var _sources = null;");
            sb.AppendLine("  var _mapPlugin = null;");
            sb.AppendLine("  var _leafletMap = null;");
            sb.AppendLine("  var _baseTileLayer = null;");
            sb.AppendLine("  var _originalUrl = null;");
            sb.AppendLine("  var _activeKey = null;");
            sb.AppendLine("  var _tileUrls = {};");
            sb.AppendLine();

            // Fetch sources from plugin endpoint
            sb.AppendLine("  function fetchSources(callback) {");
            sb.AppendLine("    var xhr = new XMLHttpRequest();");
            sb.AppendLine("    xhr.open('GET', 'TileServerMBTiles/Sources.json', true);");
            sb.AppendLine("    xhr.onreadystatechange = function() {");
            sb.AppendLine("      if(xhr.readyState === 4 && xhr.status === 200) {");
            sb.AppendLine("        try { _sources = JSON.parse(xhr.responseText); } catch(e) { _sources = []; }");
            sb.AppendLine("        if(callback) callback();");
            sb.AppendLine("      }");
            sb.AppendLine("    };");
            sb.AppendLine("    xhr.send();");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Switch map by swapping the URL on VRS's own base tile layer
            sb.AppendLine("  function switchToSource(key) {");
            sb.AppendLine("    if(!_baseTileLayer) return;");
            sb.AppendLine("    if(!key || key === '__default__') {");
            sb.AppendLine("      _baseTileLayer.setUrl(_originalUrl);");
            sb.AppendLine("      _activeKey = null;");
            sb.AppendLine("      try { localStorage.removeItem('vrsMBTilesSource'); } catch(e) {}");
            sb.AppendLine("    } else if(_tileUrls[key]) {");
            sb.AppendLine("      _baseTileLayer.setUrl(_tileUrls[key]);");
            sb.AppendLine("      _activeKey = key;");
            sb.AppendLine("      try { localStorage.setItem('vrsMBTilesSource', key); } catch(e) {}");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Opacity: applies to the base tile layer directly
            sb.AppendLine("  function updateOpacity(val) {");
            sb.AppendLine("    if(_baseTileLayer && _activeKey) _baseTileLayer.setOpacity(val / 100);");
            sb.AppendLine("    if(!_activeKey && _baseTileLayer) _baseTileLayer.setOpacity(1);");
            sb.AppendLine("    try { localStorage.setItem('vrsMBTilesOpacity', val); } catch(e) {}");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Setup
            sb.AppendLine("  function setup() {");
            sb.AppendLine("    if(!_sources || _sources.length === 0) return false;");
            sb.AppendLine("    var mapJQ = $('#map_canvas');");
            sb.AppendLine("    if(!mapJQ.length) mapJQ = $('#map');");
            sb.AppendLine("    if(!mapJQ.length) return false;");
            sb.AppendLine("    if(!VRS.jQueryUIHelper || !VRS.jQueryUIHelper.getMapPlugin) return false;");
            sb.AppendLine("    _mapPlugin = VRS.jQueryUIHelper.getMapPlugin(mapJQ);");
            sb.AppendLine("    if(!_mapPlugin || !_mapPlugin.isOpen()) return false;");
            sb.AppendLine("    try { _leafletMap = _mapPlugin.getNative(); } catch(e) {}");
            sb.AppendLine("    if(!_leafletMap) return false;");
            sb.AppendLine();

            // Find VRS's base tile layer and save its original URL
            sb.AppendLine("    _leafletMap.eachLayer(function(layer) {");
            sb.AppendLine("      if(layer._url && !_baseTileLayer) {");
            sb.AppendLine("        _baseTileLayer = layer;");
            sb.AppendLine("        _originalUrl = layer._url;");
            sb.AppendLine("      }");
            sb.AppendLine("    });");
            sb.AppendLine("    if(!_baseTileLayer) return false;");
            sb.AppendLine();

            // Build tile URLs for each source
            sb.AppendLine("    for(var i = 0; i < _sources.length; i++) {");
            sb.AppendLine("      var src = _sources[i];");
            sb.AppendLine("      var ext = (src.format === 'jpg' || src.format === 'jpeg') ? 'jpg' : 'png';");
            sb.AppendLine("      _tileUrls[src.key] = 'TileServerMBTiles/' + encodeURIComponent(src.key) + '/{z}/{x}/{y}.' + ext;");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Build Leaflet control with dropdown + opacity slider
            sb.AppendLine("    var MapSelect = L.Control.extend({");
            sb.AppendLine("      options: { position: 'topright' },");
            sb.AppendLine("      onAdd: function() {");
            sb.AppendLine("        var div = L.DomUtil.create('div', 'leaflet-bar');");
            sb.AppendLine("        div.style.cssText = 'background:#fff;border-radius:4px;padding:4px;';");
            sb.AppendLine("        var sel = L.DomUtil.create('select', '', div);");
            sb.AppendLine("        sel.style.cssText = 'display:block;width:auto;font-size:13px;border:1px solid #ccc;border-radius:3px;padding:2px 4px;cursor:pointer;background:#fff;';");
            sb.AppendLine("        var opt = document.createElement('option');");
            sb.AppendLine("        opt.value = '__default__';");
            sb.AppendLine("        opt.textContent = 'Default Map';");
            sb.AppendLine("        sel.appendChild(opt);");
            sb.AppendLine("        for(var i = 0; i < _sources.length; i++) {");
            sb.AppendLine("          var o = document.createElement('option');");
            sb.AppendLine("          o.value = _sources[i].key;");
            sb.AppendLine("          o.textContent = _sources[i].name;");
            sb.AppendLine("          sel.appendChild(o);");
            sb.AppendLine("        }");
            sb.AppendLine("        sel.addEventListener('change', function() {");
            sb.AppendLine("          switchToSource(sel.value);");
            sb.AppendLine("          var isMBTiles = sel.value !== '__default__';");
            sb.AppendLine("          slider.style.display = isMBTiles ? 'block' : 'none';");
            sb.AppendLine("          if(isMBTiles) updateOpacity(slider.value);");
            sb.AppendLine("          else _baseTileLayer.setOpacity(1);");
            sb.AppendLine("        });");
            sb.AppendLine("        var slider = L.DomUtil.create('input', '', div);");
            sb.AppendLine("        slider.type = 'range';");
            sb.AppendLine("        slider.min = '0'; slider.max = '100';");
            sb.AppendLine("        var savedOpacity = '100';");
            sb.AppendLine("        try { savedOpacity = localStorage.getItem('vrsMBTilesOpacity') || '100'; } catch(e) {}");
            sb.AppendLine("        slider.value = savedOpacity;");
            sb.AppendLine("        slider.style.cssText = 'display:none;width:100%;margin-top:4px;cursor:pointer;';");
            sb.AppendLine("        slider.addEventListener('input', function() { updateOpacity(slider.value); });");
            sb.AppendLine("        L.DomEvent.disableClickPropagation(div);");
            sb.AppendLine("        L.DomEvent.disableScrollPropagation(div);");
            sb.AppendLine("        this._select = sel;");
            sb.AppendLine("        this._slider = slider;");
            sb.AppendLine("        return div;");
            sb.AppendLine("      }");
            sb.AppendLine("    });");
            sb.AppendLine("    var ctrl = new MapSelect();");
            sb.AppendLine("    ctrl.addTo(_leafletMap);");
            sb.AppendLine();

            // Restore saved selection and opacity
            sb.AppendLine("    var saved = null;");
            sb.AppendLine("    try { saved = localStorage.getItem('vrsMBTilesSource'); } catch(e) {}");
            sb.AppendLine("    if(saved && _tileUrls[saved]) {");
            sb.AppendLine("      ctrl._select.value = saved;");
            sb.AppendLine("      switchToSource(saved);");
            sb.AppendLine("      ctrl._slider.style.display = 'block';");
            sb.AppendLine("      updateOpacity(ctrl._slider.value);");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    return true;");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Retry until map is ready
            sb.AppendLine("  var _retries = 0;");
            sb.AppendLine("  function trySetup() {");
            sb.AppendLine("    fetchSources(function() {");
            sb.AppendLine("      if(!setup() && _retries < 10) { _retries++; setTimeout(trySetup, 1500); }");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine("  setTimeout(trySetup, 2000);");
            sb.AppendLine();

            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
