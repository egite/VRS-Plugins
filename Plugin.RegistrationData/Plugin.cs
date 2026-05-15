using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using InterfaceFactory;
using Newtonsoft.Json;
using VirtualRadar.Interface;
using VirtualRadar.Interface.WebServer;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.RegistrationData
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private IWebServer _WebServer;
        private IWebSite _WebSite;
        private List<HtmlContentInjector> _Injectors = new List<HtmlContentInjector>();
        private bool _InjectorsActive;
        private RegistrationDatabase _Database;
        internal RegistrationDatabase Database { get { return _Database; } }
        private DatabaseDownloader _Downloader;
        internal DatabaseDownloader Downloader { get { return _Downloader; } }
        private string _CachedHtmlTemplate;
        private HashSet<string> _LaddSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        internal int LaddCount { get { return _LaddSet.Count; } }

        private void LoadLaddList()
        {
            try {
                var assembly = Assembly.GetExecutingAssembly();
                using(var stream = assembly.GetManifestResourceStream("VirtualRadar.Plugin.RegistrationData.ladd.csv")) {
                    if(stream == null) return;
                    using(var reader = new System.IO.StreamReader(stream)) {
                        string line;
                        while((line = reader.ReadLine()) != null) {
                            var parts = line.Split(',');
                            if(parts.Length >= 1) {
                                var reg = parts[0].Trim().ToUpperInvariant();
                                if(!string.IsNullOrEmpty(reg)) _LaddSet.Add(reg);
                            }
                        }
                    }
                }
            } catch { }
        }

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object> _IconStatusCache
            = new System.Collections.Concurrent.ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private volatile int _IconCacheVersion = 0;

        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.RegistrationData"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Registration Data"; } }
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
            LoadLaddList();

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
                    "RegistrationDataPluginOptions.html",
                    "Registration Data",
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

            if(_Downloader != null) {
                _Downloader.Dispose();
                _Downloader = null;
            }

            if(_Database != null) {
                _Database.Dispose();
                _Database = null;
            }
        }

        public void ShowWinFormsOptionsUI()
        {
            using(var dialog = new WinForms.OptionsView()) {
                dialog.Options = OptionsStorage.Load(this);
                dialog.Database = _Database;
                dialog.DownloadAircraftRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadAircraftDatabase());
                };
                dialog.DownloadAirmenRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadAirmenDatabase());
                };
                dialog.DownloadCcarRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadCcarDatabase());
                };
                dialog.DownloadNtsbRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadNtsbDatabase());
                };
                dialog.DownloadSdrRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadSdrDatabase());
                };
                dialog.DownloadCasaRequested += (s, e) => {
                    ThreadPool.QueueUserWorkItem(_ => _Downloader?.DownloadCasaDatabase());
                };

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    ApplyOptions(dialog.Options);
                }
            }
        }

        internal void ReloadOptions()
        {
            _Options = OptionsStorage.Load(this);
            _IconStatusCache.Clear();
            _IconCacheVersion++;
        }

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

                if(_Downloader != null) {
                    _Downloader.Stop();
                }

                Status = "Disabled";
                StatusDescription = "";
            } else {
                if(!_InjectorsActive) {
                    foreach(var injector in _Injectors) {
                        _WebSite.AddHtmlContentInjector(injector);
                    }
                    _InjectorsActive = true;
                }

                // Initialize database
                if(_Database == null) {
                    _Database = new RegistrationDatabase(options.DatabaseFolder);
                    _Database.EnsureAircraftSchema();
                    _Database.EnsureAirmenSchema();
                    _Database.EnsureCcarSchema();
                    _Database.EnsureNtsbSchema();
                    _Database.EnsureCasaSchema();
                    _Database.EnsureNzcaaSchema();
                }

                // Initialize downloader
                if(_Downloader == null) {
                    _Downloader = new DatabaseDownloader(_Database, this);
                    _Downloader.Start(options);
                }

                var counts = _Database.GetRecordCounts();
                long regCount, airmenCount, ccarCount, ntsbCount, casaCount;
                counts.TryGetValue("aircraft_registration", out regCount);
                counts.TryGetValue("airmen_basic", out airmenCount);
                counts.TryGetValue("ccar_aircraft", out ccarCount);
                counts.TryGetValue("ntsb_event", out ntsbCount);
                counts.TryGetValue("casa_aircraft", out casaCount);

                Status = "Enabled";
                StatusDescription = $"{regCount:N0} FAA aircraft, {airmenCount:N0} airmen, {ccarCount:N0} CCAR, {ntsbCount:N0} NTSB, {casaCount:N0} CASA";
            }
        }

        private void WebServer_BeforeRequestReceived(object sender, RequestReceivedEventArgs args)
        {
            if(args.Handled) return;
            if(!(_Options?.Enabled ?? false)) return;

            try {
                var path = args.PathAndFile ?? "";

                if(string.Equals(path, "/RegistrationData/Aircraft.json", StringComparison.OrdinalIgnoreCase)) {
                    HandleAircraftJsonRequest(args);
                } else if(string.Equals(path, "/RegistrationData/Aircraft.html", StringComparison.OrdinalIgnoreCase)) {
                    HandleAircraftHtmlRequest(args);
                } else if(string.Equals(path, "/RegistrationData/Photos.json", StringComparison.OrdinalIgnoreCase)) {
                    HandlePhotosRequest(args);
                } else if(string.Equals(path, "/RegistrationData/Status.json", StringComparison.OrdinalIgnoreCase)) {
                    HandleStatusRequest(args);
                } else if(string.Equals(path, "/RegistrationData/IconStatus.json", StringComparison.OrdinalIgnoreCase)) {
                    HandleIconStatusRequest(args);
                }
            } catch(Exception ex) {
                StatusDescription = "Request error: " + ex.Message;
            }
        }

        private static string GetQueryParam(RequestReceivedEventArgs args, string name)
        {
            // Try the QueryString property first
            try {
                var qs = args.QueryString;
                if(qs != null) {
                    var val = qs[name];
                    if(val != null) return val;
                }
            } catch { }

            // Fall back to parsing from RawUrl
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

        private void HandleAircraftJsonRequest(RequestReceivedEventArgs args)
        {
            var reg = GetQueryParam(args, "reg").Trim();
            if(string.IsNullOrEmpty(reg)) {
                args.Response.StatusCode = HttpStatusCode.BadRequest;
                args.Handled = true;
                return;
            }

            // Check if Canadian registration
            var upperReg = reg.ToUpperInvariant().Replace("-", "");
            if(upperReg.StartsWith("C") && upperReg.Length >= 4 && "FGI".IndexOf(upperReg[1]) >= 0) {
                HandleCanadianJsonRequest(args, upperReg.Substring(1)); // strip the C prefix, keep FXXX
                return;
            }

            // Check if Australian registration
            if(upperReg.StartsWith("VH") && upperReg.Length >= 4) {
                HandleAustralianJsonRequest(args, reg);
                return;
            }

            // Check if New Zealand registration
            if(upperReg.StartsWith("ZK") && upperReg.Length >= 4) {
                HandleNzJsonRequest(args, reg);
                return;
            }

            var aircraft = _Database.GetAircraftByNNumber(reg);
            if(aircraft.Count == 0) {
                // Try CASA database for VH- registrations from VRS cache
                var regUpper = reg.ToUpperInvariant();
                if(regUpper.StartsWith("VH")) {
                    HandleAustralianJsonRequest(args, reg);
                    return;
                }

                // Try VRS cache for non-US aircraft
                var vrsData = _Database.GetVrsCacheByRegistration(reg);
                if(vrsData.Count == 0) {
                    var json = JsonConvert.SerializeObject(new { found = false });
                    SendJson(args, json);
                    return;
                }
                // Return VRS cache data
                var vrsResult = new {
                    found = true,
                    source = "vrs_cache",
                    registration = new {
                        registration = GetVal(vrsData, "Registration"),
                        icao_hex = GetVal(vrsData, "Icao"),
                        country = GetVal(vrsData, "Country"),
                        serial_number = GetVal(vrsData, "Serial"),
                        year_built = GetVal(vrsData, "YearBuilt"),
                    },
                    aircraft_ref = new {
                        mfr = GetVal(vrsData, "Manufacturer"),
                        model = GetVal(vrsData, "Model"),
                    },
                    engine_ref = new { },
                    vrs_operator = GetVal(vrsData, "Operator"),
                    vrs_operator_icao = GetVal(vrsData, "OperatorIcao"),
                    vrs_model_icao = GetVal(vrsData, "ModelIcao"),
                    is_ladd = IsLadd(reg),
                };
                SendJson(args, JsonConvert.SerializeObject(vrsResult));
                return;
            }

            string mfrMdlCode, engMfrMdl;
            aircraft.TryGetValue("mfr_mdl_code", out mfrMdlCode);
            aircraft.TryGetValue("eng_mfr_mdl", out engMfrMdl);

            var acftRef = _Database.GetAircraftReference(mfrMdlCode);
            var engRef = _Database.GetEngineReference(engMfrMdl);

            // Pilot matching
            string ownerName, ownerStreet, ownerCity, ownerState, registrantType;
            aircraft.TryGetValue("name", out ownerName);
            aircraft.TryGetValue("street", out ownerStreet);
            aircraft.TryGetValue("city", out ownerCity);
            aircraft.TryGetValue("state", out ownerState);
            aircraft.TryGetValue("type_registrant", out registrantType);

            // Check if FAA owner info is redacted
            bool ownerRedacted = string.IsNullOrWhiteSpace(ownerName) && string.IsNullOrWhiteSpace(ownerStreet) && string.IsNullOrWhiteSpace(ownerCity);
            string vrsOperator = null;
            string vrsOperatorIcao = null;
            string vrsModelIcao = null;

            // Always look up VRS cache for model ICAO type and operator info
            {
                string modeSCode;
                aircraft.TryGetValue("mode_s_code", out modeSCode);
                if(!string.IsNullOrEmpty(modeSCode)) {
                    try {
                        var icaoHex = Convert.ToInt32(modeSCode, 8).ToString("X6");
                        var vrsCacheResult = _Database.GetOperatorFromVrsCache(icaoHex);
                        string op, opIcao, mdlIcao;
                        vrsCacheResult.TryGetValue("operator", out op);
                        vrsCacheResult.TryGetValue("operator_icao", out opIcao);
                        vrsCacheResult.TryGetValue("model_icao", out mdlIcao);
                        if(!string.IsNullOrWhiteSpace(opIcao)) vrsOperatorIcao = opIcao;
                        if(!string.IsNullOrWhiteSpace(mdlIcao)) vrsModelIcao = mdlIcao;
                        if(ownerRedacted && !string.IsNullOrWhiteSpace(op)) {
                            vrsOperator = op;
                            // Use VRS operator as the owner name for pilot matching
                            ownerName = op;
                        }
                    } catch { }
                }
            }

            var matcher = new PilotMatcher(_Database, _Options.FuzzyMatchMaxDistance);
            var matches = matcher.FindMatches(ownerName, ownerStreet, ownerCity, ownerState, registrantType);

            bool isCorporate = false;
            bool isAddressOnly = false;
            if(!string.IsNullOrEmpty(registrantType)) {
                int regType;
                if(int.TryParse(registrantType, out regType) && regType >= 2 && regType <= 8 && regType != 2 && regType != 4) {
                    isCorporate = true;
                    isAddressOnly = true; // Name match skipped, searched by address only
                }
            }

            // For experimental aircraft, also try matching by manufacturer name (the builder)
            // Builder name is "FIRSTNAME LASTNAME" format (reversed from FAA owner format)
            string certification;
            aircraft.TryGetValue("certification", out certification);
            bool isExperimental = (certification ?? "").Contains("4");
            if(isExperimental) {
                string mfrName = "";
                acftRef.TryGetValue("mfr", out mfrName);
                if(!string.IsNullOrEmpty(mfrName)) {
                    var mfrMatches = matcher.FindBuilderMatches(mfrName, ownerStreet, ownerCity, ownerState);
                    // Add builder matches with a bonus for experimental/kit-built aircraft
                    var existingIds = new HashSet<string>(matches.Select(m => GetVal(m.AirmenRecord, "unique_id")));
                    foreach(var m in mfrMatches) {
                        m.Score += 25; // Builder-is-pilot bonus for experimental aircraft
                        if(m.Score >= 100) m.MatchType = MatchType.Exact;
                        else if(m.Score >= 70) m.MatchType = MatchType.Close;
                        if(!existingIds.Contains(GetVal(m.AirmenRecord, "unique_id"))) {
                            matches.Add(m);
                        }
                    }
                    // If corporate but experimental, don't treat as corporate (builder is likely the pilot)
                    if(matches.Count > 0) isCorporate = false;
                }
            }

            var pilotResults = matches.Select(m => new {
                first_name = GetVal(m.AirmenRecord, "first_name"),
                last_name = GetVal(m.AirmenRecord, "last_name"),
                street = GetVal(m.AirmenRecord, "street1"),
                street2 = GetVal(m.AirmenRecord, "street2"),
                city = GetVal(m.AirmenRecord, "city"),
                state = GetVal(m.AirmenRecord, "state"),
                zip_code = GetVal(m.AirmenRecord, "zip_code"),
                country = GetVal(m.AirmenRecord, "country"),
                region = GetVal(m.AirmenRecord, "region"),
                med_class = GetVal(m.AirmenRecord, "med_class"),
                med_date = GetVal(m.AirmenRecord, "med_date"),
                med_exp_date = GetVal(m.AirmenRecord, "med_exp_date"),
                unique_id = GetVal(m.AirmenRecord, "unique_id"),
                score = m.Score,
                match_type = m.MatchType.ToString(),
                certificates = m.Certificates.Select(c => new {
                    type = GetVal(c, "certificate_type"),
                    level = GetVal(c, "level"),
                    expires = GetVal(c, "expires"),
                    ratings = GetVal(c, "ratings"),
                }).ToList(),
            }).ToList();

            // NTSB accident/incident lookup
            string nNumber;
            aircraft.TryGetValue("n_number", out nNumber);
            var ntsbEvents = _Database.GetNtsbByRegistration(nNumber ?? reg);

            // Determine aircraft type match for each NTSB event (N-numbers get reused)
            string currentMfr = "", currentModel = "";
            acftRef.TryGetValue("mfr", out currentMfr);
            acftRef.TryGetValue("model", out currentModel);
            currentMfr = (currentMfr ?? "").Trim().ToUpperInvariant();
            currentModel = (currentModel ?? "").Trim().ToUpperInvariant();

            var sdrReports = _Database.GetSdrByRegistration(nNumber ?? reg);
            var sdrResults = sdrReports.Select(sr => new {
                operator_control_number = GetVal(sr, "operator_control_number"),
                difficulty_date = GetVal(sr, "difficulty_date"),
                aircraft_make = GetVal(sr, "aircraft_make"),
                aircraft_model = GetVal(sr, "aircraft_model"),
                jasc_code = GetVal(sr, "jasc_code"),
                part_name = GetVal(sr, "part_name"),
                part_number = GetVal(sr, "part_number"),
                nature_of_condition = GetVal(sr, "nature_of_condition"),
                stage_of_operation = GetVal(sr, "stage_of_operation"),
                how_discovered = GetVal(sr, "how_discovered"),
                discrepancy = GetVal(sr, "discrepancy"),
            }).ToList();
            var ntsbResults = ntsbEvents.Select(ev => new {
                ntsb_no = GetVal(ev, "ntsb_no"),
                ev_date = GetVal(ev, "ev_date"),
                ev_city = GetVal(ev, "ev_city"),
                ev_state = GetVal(ev, "ev_state"),
                ev_country = GetVal(ev, "ev_country"),
                ev_type = GetVal(ev, "ev_type"),
                ev_highest_injury = GetVal(ev, "ev_highest_injury"),
                inj_tot_f = GetVal(ev, "inj_tot_f"),
                inj_tot_s = GetVal(ev, "inj_tot_s"),
                inj_tot_m = GetVal(ev, "inj_tot_m"),
                inj_tot_n = GetVal(ev, "inj_tot_n"),
                latitude = GetVal(ev, "latitude"),
                longitude = GetVal(ev, "longitude"),
                acft_make = GetVal(ev, "acft_make"),
                acft_model = GetVal(ev, "acft_model"),
                acft_series = GetVal(ev, "acft_series"),
                acft_category = GetVal(ev, "acft_category"),
                damage = GetVal(ev, "damage"),
                far_part = GetVal(ev, "far_part"),
                oper_name = GetVal(ev, "oper_name"),
                phase_flt_spec = GetVal(ev, "phase_flt_spec"),
                narr_cause = GetVal(ev, "narr_cause"),
                apt_name = GetVal(ev, "apt_name"),
                light_cond = GetVal(ev, "light_cond"),
                sky_cond_nonceil = GetVal(ev, "sky_cond_nonceil"),
                wind_vel_kts = GetVal(ev, "wind_vel_kts"),
                wx_brief_comp = GetVal(ev, "wx_brief_comp"),
                type_match = NtsbTypeMatches(GetVal(ev, "acft_make"), GetVal(ev, "acft_model"), currentMfr, currentModel),
            }).ToList();

            var result = new {
                found = true,
                source = "faa",
                db_date = _Options?.LastAircraftDownload.ToString("d-MMM-yyyy") ?? "",
                pilot_db_date = _Options?.LastAirmenDownload.ToString("d-MMM-yyyy") ?? "",
                pilot_threshold = _Options?.PilotMatchThreshold ?? 100,
                weight_unit = _Options?.WeightUnit ?? "lbs",
                ntsb_db_date = _Options?.LastNtsbDownload.ToString("d-MMM-yyyy") ?? "",
                sdr_db_date = _Options?.LastSdrDownload.ToString("d-MMM-yyyy") ?? "",
                registration = aircraft,
                aircraft_ref = acftRef,
                engine_ref = engRef,
                is_corporate = isCorporate,
                is_address_only = isAddressOnly,
                pilots = pilotResults,
                ntsb_events = ntsbResults,
                sdr_reports = sdrResults,
                owner_redacted = ownerRedacted,
                vrs_operator = vrsOperator ?? "",
                vrs_operator_icao = vrsOperatorIcao ?? "",
                vrs_model_icao = vrsModelIcao ?? "",
                is_ladd = IsLadd(nNumber ?? reg),
            };

            SendJson(args, JsonConvert.SerializeObject(result));
        }

        private void HandleAircraftHtmlRequest(RequestReceivedEventArgs args)
        {
            var reg = GetQueryParam(args, "reg").Trim();
            var html = GetHtmlTemplate();
            html = html.Replace("{{REG}}", reg.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;"));

            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, html, Encoding.UTF8, MimeType.Html);
            args.Handled = true;
        }

        private void HandleCanadianJsonRequest(RequestReceivedEventArgs args, string mark)
        {
            var aircraft = _Database.GetCcarAircraftByMark(mark);
            if(aircraft.Count == 0) {
                // Try with leading space (old 3-char marks)
                aircraft = _Database.GetCcarAircraftByMark(mark.Length == 4 ? mark.Substring(1) : mark);
            }
            if(aircraft.Count == 0) {
                SendJson(args, JsonConvert.SerializeObject(new { found = false }));
                return;
            }

            string storedMark;
            aircraft.TryGetValue("mark", out storedMark);
            var owners = _Database.GetCcarOwnersByMark(storedMark ?? mark);

            var result = new {
                found = true,
                source = "ccar",
                db_date = _Options?.LastCcarDownload.ToString("d-MMM-yyyy") ?? "",
                registration = aircraft,
                owners = owners.Select(o => new {
                    owner_name = GetVal(o, "owner_name"),
                    street = GetVal(o, "street"),
                    city = GetVal(o, "city"),
                    province_state = GetVal(o, "province_state"),
                    postal_code = GetVal(o, "postal_code"),
                    country = GetVal(o, "country"),
                    owner_type = GetVal(o, "owner_type"),
                }).ToList(),
                weight_unit = _Options?.WeightUnit ?? "lbs",
            };

            SendJson(args, JsonConvert.SerializeObject(result));
        }

        private void HandleAustralianJsonRequest(RequestReceivedEventArgs args, string reg)
        {
            // Extract mark: strip "VH-" prefix
            var upperReg = reg.ToUpperInvariant().Replace("-", "");
            var casaMark = upperReg.Length > 2 ? upperReg.Substring(2) : upperReg;

            var casaData = _Database.GetCasaAircraftByMark(casaMark);
            if(casaData.Count == 0) {
                // Try with hyphen stripped from reg directly
                var dashIdx = reg.IndexOf('-');
                if(dashIdx >= 0) {
                    casaMark = reg.Substring(dashIdx + 1).Trim().ToUpperInvariant();
                    casaData = _Database.GetCasaAircraftByMark(casaMark);
                }
            }
            if(casaData.Count == 0) {
                // Fall back to VRS cache
                var vrsData = _Database.GetVrsCacheByRegistration(reg);
                if(vrsData.Count == 0) {
                    SendJson(args, JsonConvert.SerializeObject(new { found = false }));
                    return;
                }
                var vrsResult = new {
                    found = true,
                    source = "vrs_cache",
                    registration = new {
                        registration = GetVal(vrsData, "Registration"),
                        icao_hex = GetVal(vrsData, "Icao"),
                        country = GetVal(vrsData, "Country"),
                        serial_number = GetVal(vrsData, "Serial"),
                        year_built = GetVal(vrsData, "YearBuilt"),
                    },
                    aircraft_ref = new {
                        mfr = GetVal(vrsData, "Manufacturer"),
                        model = GetVal(vrsData, "Model"),
                    },
                    engine_ref = new { },
                    vrs_operator = GetVal(vrsData, "Operator"),
                    vrs_operator_icao = GetVal(vrsData, "OperatorIcao"),
                    vrs_model_icao = GetVal(vrsData, "ModelIcao"),
                    is_ladd = IsLadd(reg),
                };
                SendJson(args, JsonConvert.SerializeObject(vrsResult));
                return;
            }

            // Parse json_data for full field display
            object allFields = casaData;
            string jsonStr;
            if(casaData.TryGetValue("json_data", out jsonStr) && !string.IsNullOrEmpty(jsonStr)) {
                try { allFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonStr); } catch { }
            }

            var result = new {
                found = true,
                source = "casa",
                db_date = _Options?.LastCasaDownload.ToString("d-MMM-yyyy") ?? "",
                registration = allFields,
                is_ladd = IsLadd(reg),
                weight_unit = _Options?.WeightUnit ?? "lbs",
            };

            SendJson(args, JsonConvert.SerializeObject(result));
        }

        private void HandleNzJsonRequest(RequestReceivedEventArgs args, string reg)
        {
            var upperReg = reg.ToUpperInvariant().Replace("-", "");
            // Try NZ CAA database
            var nzReg = upperReg.StartsWith("ZK") ? upperReg : "ZK" + upperReg;
            // Try with and without dash
            var nzData = _Database.GetNzcaaAircraft(nzReg);
            if(nzData.Count == 0) nzData = _Database.GetNzcaaAircraft("ZK-" + upperReg.Substring(2));
            if(nzData.Count == 0) nzData = _Database.GetNzcaaAircraft(reg);

            if(nzData.Count > 0) {
                object nzAllFields = nzData;
                string nzJsonStr;
                if(nzData.TryGetValue("json_data", out nzJsonStr) && !string.IsNullOrEmpty(nzJsonStr)) {
                    try { nzAllFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(nzJsonStr); } catch { }
                }
                var result = new {
                    found = true,
                    source = "nzcaa",
                    db_date = _Options?.LastNzcaaDownload.ToString("d-MMM-yyyy") ?? "",
                    registration = nzAllFields,
                    is_ladd = IsLadd(reg),
                    weight_unit = _Options?.WeightUnit ?? "lbs",
                };
                SendJson(args, JsonConvert.SerializeObject(result));
                return;
            }

            // Fall back to VRS cache
            var vrsData = _Database.GetVrsCacheByRegistration(reg);
            if(vrsData.Count == 0) {
                SendJson(args, JsonConvert.SerializeObject(new { found = false }));
                return;
            }
            var vrsResult = new {
                found = true,
                source = "vrs_cache",
                registration = new {
                    registration = GetVal(vrsData, "Registration"),
                    icao_hex = GetVal(vrsData, "Icao"),
                    country = GetVal(vrsData, "Country"),
                    serial_number = GetVal(vrsData, "Serial"),
                    year_built = GetVal(vrsData, "YearBuilt"),
                },
                aircraft_ref = new {
                    mfr = GetVal(vrsData, "Manufacturer"),
                    model = GetVal(vrsData, "Model"),
                },
                engine_ref = new { },
                vrs_operator = GetVal(vrsData, "Operator"),
                vrs_operator_icao = GetVal(vrsData, "OperatorIcao"),
                vrs_model_icao = GetVal(vrsData, "ModelIcao"),
                is_ladd = IsLadd(reg),
            };
            SendJson(args, JsonConvert.SerializeObject(vrsResult));
        }

        private void HandleStatusRequest(RequestReceivedEventArgs args)
        {
            var counts = _Database?.GetRecordCounts() ?? new Dictionary<string, long>();
            var options = _Options;

            var result = new {
                downloading = _Downloader?.IsDownloading ?? false,
                last_aircraft_download = options?.LastAircraftDownload,
                last_airmen_download = options?.LastAirmenDownload,
                record_counts = counts,
            };

            SendJson(args, JsonConvert.SerializeObject(result));
        }

        private void HandleIconStatusRequest(RequestReceivedEventArgs args)
        {
            var regsParam = GetQueryParam(args, "regs").Trim();
            if(string.IsNullOrEmpty(regsParam)) {
                SendJson(args, "{}");
                return;
            }

            var regs = regsParam.Split(',');
            var result = new Dictionary<string, object>();

            var pinkPatterns = new List<string>();
            foreach(var p in (_Options?.PinkRegistration ?? "").Split(',')) {
                var pr = p.Trim().ToUpperInvariant();
                if(!string.IsNullOrEmpty(pr)) pinkPatterns.Add(pr);
            }
            var modelPatterns = new List<string>();
            foreach(var m in (_Options?.HighlightModelIcao ?? "").Split(',')) {
                var hm = m.Trim().ToUpperInvariant();
                if(!string.IsNullOrEmpty(hm)) modelPatterns.Add(hm);
            }

            foreach(var rawReg in regs) {
                var reg = rawReg.Trim().ToUpperInvariant();
                if(string.IsNullOrEmpty(reg)) continue;
                if(result.ContainsKey(reg)) continue;

                // Check cache first
                object cached;
                if(_IconStatusCache.TryGetValue(reg, out cached)) {
                    result[reg] = cached;
                    continue;
                }

                bool isPink = MatchesAnyPattern(reg, pinkPatterns);
                bool isLadd = IsLadd(reg);

                bool hasNtsb = false;
                bool hasExactPilot = false;
                bool hasSdr = false;
                bool isModelHighlight = false;

                // Always check model highlight (even for pink/ownship)
                if(modelPatterns.Count > 0) {
                    try {
                        var acForModel = _Database.GetAircraftByNNumber(reg);
                        if(acForModel.Count > 0) {
                            string modeSCodeM;
                            acForModel.TryGetValue("mode_s_code", out modeSCodeM);
                            if(!string.IsNullOrEmpty(modeSCodeM)) {
                                try {
                                    var icaoHexM = Convert.ToInt32(modeSCodeM, 8).ToString("X6");
                                    var vrsCacheM = _Database.GetOperatorFromVrsCache(icaoHexM);
                                    string cachedModelIcaoM;
                                    vrsCacheM.TryGetValue("model_icao", out cachedModelIcaoM);
                                    if(!string.IsNullOrEmpty(cachedModelIcaoM) && MatchesAnyPattern(cachedModelIcaoM.Trim(), modelPatterns)) {
                                        isModelHighlight = true;
                                    }
                                } catch { }
                            }
                        }
                    } catch { }
                }

                // Always check pilot match and NTSB (needed for operator green and type matching)
                try {
                    var aircraft = _Database.GetAircraftByNNumber(reg);
                    if(aircraft.Count > 0) {
                        string ownerName, ownerStreet, ownerCity, ownerState, registrantType;
                        aircraft.TryGetValue("name", out ownerName);
                        aircraft.TryGetValue("street", out ownerStreet);
                        aircraft.TryGetValue("city", out ownerCity);
                        aircraft.TryGetValue("state", out ownerState);
                        aircraft.TryGetValue("type_registrant", out registrantType);
                        var matcher = new PilotMatcher(_Database, _Options?.FuzzyMatchMaxDistance ?? 2);
                        var matches = matcher.FindMatches(ownerName, ownerStreet, ownerCity, ownerState, registrantType);

                        // Builder matching for experimental aircraft (same logic as Aircraft.json)
                        string certification;
                        aircraft.TryGetValue("certification", out certification);
                        if((certification ?? "").Contains("4")) {
                            string mfrMdlCode2;
                            aircraft.TryGetValue("mfr_mdl_code", out mfrMdlCode2);
                            var acRef2 = _Database.GetAircraftReference(mfrMdlCode2);
                            string mfrName;
                            acRef2.TryGetValue("mfr", out mfrName);
                            if(!string.IsNullOrEmpty(mfrName)) {
                                var mfrMatches = matcher.FindBuilderMatches(mfrName, ownerStreet, ownerCity, ownerState);
                                var existingIds = new HashSet<string>(matches.Select(m => {
                                    string uid; m.AirmenRecord.TryGetValue("unique_id", out uid); return uid ?? "";
                                }));
                                foreach(var m in mfrMatches) {
                                    m.Score += 25;
                                    string uid; m.AirmenRecord.TryGetValue("unique_id", out uid);
                                    if(!existingIds.Contains(uid ?? "")) matches.Add(m);
                                }
                            }
                        }

                        var threshold = _Options?.PilotMatchThreshold ?? 100;
                        hasExactPilot = matches.Any(m => m.Score >= threshold);

                        if(!isPink) {
                            string mfrMdlCode;
                            aircraft.TryGetValue("mfr_mdl_code", out mfrMdlCode);
                            var acRef = _Database.GetAircraftReference(mfrMdlCode);
                            string mfr;
                            acRef.TryGetValue("mfr", out mfr);
                            mfr = (mfr ?? "").Trim().ToUpperInvariant();

                            string mdl;
                            acRef.TryGetValue("model", out mdl);
                            mdl = (mdl ?? "").Trim().ToUpperInvariant();

                            var events = _Database.GetNtsbByRegistration(reg);
                            hasNtsb = events.Any(ev => NtsbTypeMatches(
                                ev.ContainsKey("acft_make") ? ev["acft_make"] : "",
                                ev.ContainsKey("acft_model") ? ev["acft_model"] : "",
                                mfr, mdl));

                            hasSdr = _Database.HasSdrReports(reg);
                        }
                    }
                } catch(Exception ex) { StatusDescription = "IconPilot error: " + ex.Message; }

                var showPilot = _Options?.ShowPilotColor ?? true;
                var showNtsb = _Options?.ShowNtsbColor ?? true;
                var showSdr = _Options?.ShowSdrColor ?? true;
                var showModel = _Options?.ShowModelColor ?? true;
                var status = new {
                    ntsb = hasNtsb && showNtsb,
                    pilot = hasExactPilot && showPilot,
                    pilotRaw = hasExactPilot,  // always true if match, for operator cell
                    pink = isPink,
                    sdr = hasSdr && showSdr,
                    mdl = isModelHighlight && showModel,
                    ladd = isLadd && (_Options?.ShowLaddIndicator ?? true)
                };
                if(!isPink) _IconStatusCache[reg] = status; // Don't cache pink — it can change
                result[reg] = status;
            }

            var response = new { v = _IconCacheVersion, data = result, priority = (_Options?.ColorPriority ?? "pink,mdl,pilot,ntsb,sdr"), rowMode = (_Options?.RowColorMode ?? "row"), mdlRowMode = (_Options?.ModelRowColorMode ?? "row"), laddColor = (_Options?.LaddColor ?? "#e67e22") };
            SendJson(args, JsonConvert.SerializeObject(response));
        }

        private void HandlePhotosRequest(RequestReceivedEventArgs args)
        {
            if(!(_Options?.FetchAircraftPhotos ?? true)) {
                SendJson(args, "[]");
                return;
            }

            var reg = GetQueryParam(args, "reg").Trim();
            if(string.IsNullOrEmpty(reg)) {
                SendJson(args, "[]");
                return;
            }

            // Ensure TLS 1.2 for external API calls
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var photos = new List<object>();

            // Planespotters.net API
            try {
                using(var wc = new WebClient()) {
                    wc.Headers[HttpRequestHeader.Accept] = "application/json";
                    var json = wc.DownloadString("https://api.planespotters.net/pub/photos/reg/" + Uri.EscapeDataString(reg));
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var arr = obj["photos"] as Newtonsoft.Json.Linq.JArray;
                    if(arr != null) {
                        foreach(var p in arr) {
                            var url = p["thumbnail_large"]?["src"]?.ToString();
                            if(string.IsNullOrEmpty(url)) continue;
                            var link = (p["link"]?.ToString() ?? "").Replace("?utm_source=api", "");
                            var dataUri = DownloadAsDataUri(url);
                            if(dataUri == null) continue;
                            photos.Add(new { photoUrl = dataUri, photoLink = link, photographer = p["photographer"]?.ToString() ?? "", source = "Planespotters.net" });
                            if(photos.Count >= 2) break;
                        }
                    }
                }
            } catch { }

            // Airport-Data.com API
            try {
                using(var wc = new WebClient()) {
                    var json = wc.DownloadString("https://airport-data.com/api/ac_thumb.json?r=" + Uri.EscapeDataString(reg) + "&n=2");
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var arr = obj["data"] as Newtonsoft.Json.Linq.JArray;
                    if(arr != null) {
                        foreach(var p in arr) {
                            var url = p["image"]?.ToString() ?? "";
                            if(string.IsNullOrEmpty(url)) continue;
                            var dataUri = DownloadAsDataUri(url);
                            if(dataUri == null) continue;
                            photos.Add(new { photoUrl = dataUri, photoLink = p["link"]?.ToString() ?? "", photographer = p["photographer"]?.ToString() ?? "", source = "Airport-Data.com" });
                            if(photos.Count >= 4) break;
                        }
                    }
                }
            } catch { }

            // DuckDuckGo image search fallback
            if(photos.Count == 0 && (_Options?.DuckDuckGoImageFallback ?? true)) {
                try {
                    var ddgPhotos = SearchDuckDuckGoImages(reg + " aircraft", 3);
                    photos.AddRange(ddgPhotos);
                } catch { }
            }

            SendJson(args, JsonConvert.SerializeObject(photos));
        }

        /// <summary>
        /// Searches DuckDuckGo for images and returns up to maxResults photo objects.
        /// Uses the DDG vqd token + i.js endpoint for image results.
        /// </summary>
        private List<object> SearchDuckDuckGoImages(string query, int maxResults)
        {
            var results = new List<object>();

            using(var wc = new WebClient()) {
                wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

                // Step 1: Get the vqd token from DDG
                var searchUrl = "https://duckduckgo.com/?q=" + Uri.EscapeDataString(query) + "&iax=images&ia=images";
                var html = wc.DownloadString(searchUrl);

                var vqdMatch = Regex.Match(html, @"vqd=['""]([^'""]+)['""]");
                if(!vqdMatch.Success) {
                    vqdMatch = Regex.Match(html, @"vqd=(\d[\d-]+)");
                }
                if(!vqdMatch.Success) return results;
                var vqd = vqdMatch.Groups[1].Value;

                // Step 2: Fetch image results from the i.js API
                var apiUrl = "https://duckduckgo.com/i.js?l=us-en&o=json&q=" + Uri.EscapeDataString(query) + "&vqd=" + Uri.EscapeDataString(vqd) + "&f=,,,,,&p=1";
                wc.Headers[HttpRequestHeader.Referer] = searchUrl;
                var json = wc.DownloadString(apiUrl);
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var arr = obj["results"] as Newtonsoft.Json.Linq.JArray;
                if(arr == null) return results;

                foreach(var item in arr) {
                    var imageUrl = item["image"]?.ToString() ?? "";
                    var thumbUrl = item["thumbnail"]?.ToString() ?? "";
                    var sourceUrl = item["url"]?.ToString() ?? "";
                    var title = item["title"]?.ToString() ?? "";

                    var url = !string.IsNullOrEmpty(thumbUrl) ? thumbUrl : imageUrl;
                    if(string.IsNullOrEmpty(url)) continue;

                    var dataUri = DownloadAsDataUri(url);
                    if(dataUri == null) continue;

                    results.Add(new { photoUrl = dataUri, photoLink = sourceUrl, photographer = title, source = "DuckDuckGo" });
                    if(results.Count >= maxResults) break;
                }
            }

            return results;
        }

        private string DownloadAsDataUri(string url)
        {
            try {
                using(var wc = new WebClient()) {
                    wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0";
                    var bytes = wc.DownloadData(url);
                    var contentType = (wc.ResponseHeaders["Content-Type"] ?? "image/jpeg").Split(';')[0].Trim();
                    if(!contentType.StartsWith("image/")) return null;
                    return "data:" + contentType + ";base64," + Convert.ToBase64String(bytes);
                }
            } catch {
                return null;
            }
        }

        private void SendJson(RequestReceivedEventArgs args, string json)
        {
            var responder = Factory.Resolve<IResponder>();
            responder.SendText(args.Request, args.Response, json, Encoding.UTF8, MimeType.Json);
            args.Handled = true;
        }

        private static string GetVal(Dictionary<string, string> dict, string key)
        {
            string val;
            return dict.TryGetValue(key, out val) ? val : "";
        }

        private bool IsLadd(string reg)
        {
            if(string.IsNullOrEmpty(reg)) return false;
            reg = reg.Trim().ToUpperInvariant();
            return _LaddSet.Contains(reg) ||
                (reg.StartsWith("N") && _LaddSet.Contains(reg.Substring(1))) ||
                (!reg.StartsWith("N") && _LaddSet.Contains("N" + reg));
        }

        private static bool WildcardMatch(string value, string pattern)
        {
            if(string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(value)) return false;
            var p = pattern.Trim().ToUpperInvariant();
            var v = value.Trim().ToUpperInvariant();
            if(!p.Contains("*")) return v.Equals(p, StringComparison.OrdinalIgnoreCase);
            if(p == "*") return true;
            if(p.StartsWith("*") && p.EndsWith("*")) return v.Contains(p.Trim('*'));
            if(p.StartsWith("*")) return v.EndsWith(p.TrimStart('*'));
            if(p.EndsWith("*")) return v.StartsWith(p.TrimEnd('*'));
            // Pattern like "AB*CD" — split on * and check start/end
            var parts = p.Split('*');
            return parts.Length == 2 && v.StartsWith(parts[0]) && v.EndsWith(parts[1]);
        }

        private static bool MatchesAnyPattern(string value, IEnumerable<string> patterns)
        {
            if(string.IsNullOrEmpty(value)) return false;
            foreach(var p in patterns) {
                if(WildcardMatch(value, p)) return true;
            }
            return false;
        }

        private static bool NtsbTypeMatches(string evMake, string evModel, string regMfr, string regModel)
        {
            evMake = (evMake ?? "").Trim().ToUpperInvariant();
            evModel = (evModel ?? "").Trim().ToUpperInvariant();

            // If we don't have registration type info or NTSB type info, assume match
            if(string.IsNullOrEmpty(regMfr) || string.IsNullOrEmpty(evMake)) return true;

            // Check manufacturer similarity
            bool mfrMatch = false;
            if(evMake.Contains(regMfr) || regMfr.Contains(evMake)) {
                mfrMatch = true;
            } else {
                // Compare significant words
                var evWords = evMake.Split(new[] { ' ', '/', '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var mfrWords = regMfr.Split(new[] { ' ', '/', '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var ew in evWords) {
                    if(ew.Length < 3) continue;
                    foreach(var mw in mfrWords) {
                        if(mw.Length < 3) continue;
                        if(ew.Contains(mw) || mw.Contains(ew)) { mfrMatch = true; break; }
                    }
                    if(mfrMatch) break;
                }
            }

            if(mfrMatch) return true;

            // Manufacturer didn't match — check if models share any similarity as a fallback
            // (some manufacturers are listed differently, e.g. "TEXTRON AVIATION" vs "CESSNA")
            if(!string.IsNullOrEmpty(regModel) && !string.IsNullOrEmpty(evModel)) {
                if(evModel.Contains(regModel) || regModel.Contains(evModel)) return true;
                var evModelWord = evModel.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                var regModelWord = regModel.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                if(evModelWord.Length >= 2 && regModelWord.Length >= 2 && (evModelWord.Contains(regModelWord) || regModelWord.Contains(evModelWord))) return true;
            }

            return false;
        }

        private string GetHtmlTemplate()
        {
            if(_CachedHtmlTemplate == null) {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "VirtualRadar.Plugin.RegistrationData.Web.RegistrationData.html";
                using(var stream = assembly.GetManifestResourceStream(resourceName)) {
                    if(stream != null) {
                        using(var reader = new StreamReader(stream)) {
                            _CachedHtmlTemplate = reader.ReadToEnd();
                        }
                    }
                }

                if(_CachedHtmlTemplate == null) {
                    _CachedHtmlTemplate = "<html><body><h1>Registration Data</h1><p>Template not found.</p></body></html>";
                }
            }
            return _CachedHtmlTemplate;
        }

        private string BuildInjectScript()
        {
            var openInNewTab = _Options?.OpenInNewTab ?? true;

            var sb = new StringBuilder();
            sb.AppendLine(@"<script type=""text/javascript"">");
            sb.AppendLine("(function() {");
            sb.AppendLine("  if(!VRS || !VRS.LinkRenderHandler || !VRS.linkRenderHandlers) return;");
            sb.AppendLine("  VRS.LinkSite['faaData'] = 'faadata';");
            sb.AppendLine("  VRS.linkRenderHandlers.push(");
            sb.AppendLine("    new VRS.LinkRenderHandler({");
            sb.AppendLine("      linkSite:        VRS.LinkSite['faaData'],");
            sb.AppendLine("      displayOrder:    9000,");
            sb.AppendLine("      canLinkAircraft: function(aircraft) {");
            sb.AppendLine("        var reg = (aircraft.formatRegistration() || '').toUpperCase();");
            sb.AppendLine("        return reg.length > 0;");
            sb.AppendLine("      },");
            sb.AppendLine("      hasChanged: function(aircraft) {");
            sb.AppendLine("        return aircraft.registration.chg;");
            sb.AppendLine("      },");
            sb.AppendLine("      title: function(aircraft) {");
            sb.AppendLine("        var r = (aircraft.formatRegistration() || '').toUpperCase();");
            sb.AppendLine("        if(/^C-?[FGI]/.test(r)) return 'CCAR Lookup';");
            sb.AppendLine("        if(r.indexOf('N') === 0) return 'FAA Lookup';");
            sb.AppendLine("        return 'Aircraft Lookup';");
            sb.AppendLine("      },");
            sb.AppendLine("      buildUrl: function(aircraft) {");
            sb.AppendLine("        return 'RegistrationData/Aircraft.html?reg=' + encodeURIComponent(aircraft.formatRegistration() || '');");
            sb.AppendLine("      },");
            if(openInNewTab) {
                sb.AppendLine("      target: 'faaDataWindow'");
            } else {
                sb.AppendLine("      target: ''");
            }
            sb.AppendLine("    })");
            sb.AppendLine("  );");

            if(!openInNewTab) {
                // Popup overlay mode: intercept clicks on the FAA link
                sb.AppendLine("  document.addEventListener('click', function(e) {");
                sb.AppendLine("    var a = e.target.closest ? e.target.closest('a') : null;");
                sb.AppendLine("    if(!a) return;");
                sb.AppendLine("    var href = a.getAttribute('href') || '';");
                sb.AppendLine("    if(href.indexOf('RegistrationData/Aircraft.html') < 0) return;");
                sb.AppendLine("    e.preventDefault();");
                sb.AppendLine("    var overlay = document.createElement('div');");
                sb.AppendLine("    overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.7);z-index:10000;display:flex;align-items:center;justify-content:center;';");
                sb.AppendLine("    var frame = document.createElement('iframe');");
                sb.AppendLine("    frame.src = href;");
                sb.AppendLine("    frame.style.cssText = 'width:90%;height:90%;border:none;border-radius:8px;background:#fff;';");
                sb.AppendLine("    overlay.appendChild(frame);");
                sb.AppendLine("    overlay.addEventListener('click', function(ev) { if(ev.target === overlay) document.body.removeChild(overlay); });");
                sb.AppendLine("    document.body.appendChild(overlay);");
                sb.AppendLine("  });");
            }

            // Double-click/tap on map opens FAA Lookup for selected US/Canadian aircraft
            sb.AppendLine("  var faaAircraftList = null;");
            sb.AppendLine("  VRS.globalDispatch.hook(VRS.globalEvent.bootstrapCreated, function(bootstrap) {");
            sb.AppendLine("    var checkList = function() {");
            sb.AppendLine("      if(bootstrap.pageSettings && bootstrap.pageSettings.aircraftList) {");
            sb.AppendLine("        faaAircraftList = bootstrap.pageSettings.aircraftList;");
            sb.AppendLine("      } else { setTimeout(checkList, 500); }");
            sb.AppendLine("    };");
            sb.AppendLine("    setTimeout(checkList, 1000);");
            sb.AppendLine("  });");
            sb.AppendLine("  function openFaaLookup() {");
            sb.AppendLine("    if(!faaAircraftList) return;");
            sb.AppendLine("    var ac = faaAircraftList.getSelectedAircraft();");
            sb.AppendLine("    if(!ac) return;");
            sb.AppendLine("    var reg = ac.formatRegistration();");
            // Fall back to callsign if no registration
            sb.AppendLine("    if(!reg) { reg = ac.formatCallsign ? ac.formatCallsign(false) : ''; }");
            sb.AppendLine("    if(!reg) return;");
            sb.AppendLine("    var upper = reg.toUpperCase();");
            sb.AppendLine("    if(/^C-?[FGI][A-Z]{2,3}$/i.test(upper)) {");
            sb.AppendLine("      reg = upper.indexOf('-') >= 0 ? upper : 'C-' + upper;");
            sb.AppendLine("    } else {");
            sb.AppendLine("      reg = upper;");
            sb.AppendLine("    }");
            sb.AppendLine("    window.open('RegistrationData/Aircraft.html?reg=' + encodeURIComponent(reg), 'faaDataWindow');");
            sb.AppendLine("  }");
            sb.AppendLine("  document.addEventListener('dblclick', function(e) { openFaaLookup(); });");
            // Double-tap support for touch devices (mobile browsers ignore dblclick)
            // Guards: single finger only, no movement (rejects pans/pinch-zoom)
            sb.AppendLine("  var _lastTapTime = 0, _lastTapX = 0, _lastTapY = 0, _tapMoved = false;");
            sb.AppendLine("  document.addEventListener('touchstart', function(e) {");
            sb.AppendLine("    if(e.touches.length !== 1) { _lastTapTime = 0; return; }");
            sb.AppendLine("    _tapMoved = false;");
            sb.AppendLine("  });");
            sb.AppendLine("  document.addEventListener('touchmove', function(e) { _tapMoved = true; });");
            sb.AppendLine("  document.addEventListener('touchend', function(e) {");
            sb.AppendLine("    if(_tapMoved || e.changedTouches.length !== 1) { _lastTapTime = 0; return; }");
            sb.AppendLine("    var t = e.changedTouches[0];");
            sb.AppendLine("    var now = Date.now();");
            sb.AppendLine("    var dx = t.clientX - _lastTapX, dy = t.clientY - _lastTapY;");
            sb.AppendLine("    if(now - _lastTapTime < 300 && dx*dx + dy*dy < 900) {");
            sb.AppendLine("      e.preventDefault();");
            sb.AppendLine("      openFaaLookup();");
            sb.AppendLine("      _lastTapTime = 0;");
            sb.AppendLine("    } else {");
            sb.AppendLine("      _lastTapTime = now;");
            sb.AppendLine("      _lastTapX = t.clientX;");
            sb.AppendLine("      _lastTapY = t.clientY;");
            sb.AppendLine("    }");
            sb.AppendLine("  });");

            // (row coloring uses inline styles — VRS overwrites className on rows)

            // Icon coloring: red for NTSB incidents, green for exact pilot match
            sb.AppendLine("  var _iconCache = {};");
            sb.AppendLine("  var _cacheVersion = -1;");
            sb.AppendLine("  var _colorPriority = ['pink','mdl','pilot','ntsb','sdr'];");
            sb.AppendLine("  var _rowMode = 'row';");
            sb.AppendLine("  var _mdlRowMode = 'row';");
            sb.AppendLine("  var _laddColor = '#e67e22';");
            sb.AppendLine("  var _iconColors = {pink:['#CC3399','#FF69B4','#ffb6d9'], mdl:['#2980B9','#5DADE2','#d6eaf8'], pilot:['#00AA00','#00CC00','#e0ffe0'], ntsb:['#CC0000','#FF0000','#ffe0e0'], sdr:['#7B3F9E','#9B59B6','#e8d5f5']};");
            sb.AppendLine("  var _fetchQueue = [];");
            sb.AppendLine("  var _fetching = false;");
            sb.AppendLine("  function _processQueue() {");
            sb.AppendLine("    if(_fetching || !_fetchQueue.length) return;");
            sb.AppendLine("    _fetching = true;");
            sb.AppendLine("    var chunk = _fetchQueue.splice(0, 10);");
            sb.AppendLine("    var url = 'RegistrationData/IconStatus.json?regs=' + encodeURIComponent(chunk.join(','));");
            sb.AppendLine("    var xhr = new XMLHttpRequest();");
            sb.AppendLine("    xhr.open('GET', url);");
            sb.AppendLine("    xhr.onload = function() {");
            sb.AppendLine("      try {");
            sb.AppendLine("        var resp = JSON.parse(xhr.responseText);");
            sb.AppendLine("        var versionChanged = false;");
            sb.AppendLine("        if(resp.v !== undefined && resp.v !== _cacheVersion) {");
            sb.AppendLine("          if(_cacheVersion >= 0) { _iconCache = {}; versionChanged = true; }");
            sb.AppendLine("          _cacheVersion = resp.v;");
            sb.AppendLine("        }");
            sb.AppendLine("        if(resp.priority) _colorPriority = resp.priority.split(',');");
            sb.AppendLine("        if(resp.rowMode) _rowMode = resp.rowMode;");
            sb.AppendLine("        if(resp.mdlRowMode) _mdlRowMode = resp.mdlRowMode;");
            sb.AppendLine("        if(resp.laddColor) _laddColor = resp.laddColor;");
            sb.AppendLine("        var data = resp.data || resp;");
            sb.AppendLine("        for(var r in data) { _iconCache[r] = data[r]; }");
            sb.AppendLine("        if(versionChanged && faaAircraftList) {");
            sb.AppendLine("          var requeue = [];");
            sb.AppendLine("          faaAircraftList.toList().forEach(function(ac) {");
            sb.AppendLine("            var reg = ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("            if(reg && !_iconCache[reg]) {");
            sb.AppendLine("              _iconCache[reg] = null;");
            sb.AppendLine("              requeue.push(reg);");
            sb.AppendLine("            }");
            sb.AppendLine("          });");
            sb.AppendLine("          for(var qi = 0; qi < requeue.length; qi++) _fetchQueue.push(requeue[qi]);");
            sb.AppendLine("        }");
            sb.AppendLine("      } catch(e) {}");
            sb.AppendLine("      _fetching = false;");
            sb.AppendLine("      if(_fetchQueue.length) setTimeout(_processQueue, 50);");
            sb.AppendLine("    };");
            sb.AppendLine("    xhr.onerror = function() { _fetching = false; if(_fetchQueue.length) setTimeout(_processQueue, 200); };");
            sb.AppendLine("    xhr.send();");
            sb.AppendLine("  }");
            sb.AppendLine("  function fetchIconStatus(regs) {");
            sb.AppendLine("    if(!regs.length) return;");
            sb.AppendLine("    for(var i = 0; i < regs.length; i++) _fetchQueue.push(regs[i]);");
            sb.AppendLine("    _processQueue();");
            sb.AppendLine("  }");
            sb.AppendLine("  function getIconColour(aircraft, isSelected, originalCallback) {");
            sb.AppendLine("    if(isSelected) return '#FFD700';");
            sb.AppendLine("    var reg = aircraft.formatRegistration ? (aircraft.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("    if(reg) {");
            sb.AppendLine("      var status = _iconCache[reg];");
            sb.AppendLine("      if(status) {");
            sb.AppendLine("        for(var pi = 0; pi < _colorPriority.length; pi++) {");
            sb.AppendLine("          var ck = _colorPriority[pi];");
            sb.AppendLine("          if(status[ck] && _iconColors[ck]) return isSelected ? _iconColors[ck][0] : _iconColors[ck][1];");
            sb.AppendLine("        }");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("    return originalCallback(aircraft, isSelected);");
            sb.AppendLine("  }");
            // Hook into aircraft list updates — fetch status for each new aircraft immediately
            sb.AppendLine("  VRS.globalDispatch.hook(VRS.globalEvent.bootstrapCreated, function(bootstrap) {");
            sb.AppendLine("    setTimeout(function hookAll() {");
            sb.AppendLine("      try {");
            // Hook marker fill colour callbacks
            sb.AppendLine("      var markers = VRS.globalOptions.aircraftMarkers;");
            sb.AppendLine("      if(!markers || !markers.length) { setTimeout(hookAll, 200); return; }");
            sb.AppendLine("      for(var i = 0; i < markers.length; i++) {");
            sb.AppendLine("        if(markers[i]._faaHooked) continue;");
            sb.AppendLine("        markers[i]._faaHooked = true;");
            sb.AppendLine("        (function(marker) {");
            sb.AppendLine("          var origCb = marker.getSvgFillColourCallback();");
            sb.AppendLine("          marker.setSvgFillColourCallback(function(aircraft, isSelected) {");
            sb.AppendLine("            return getIconColour(aircraft, isSelected, origCb);");
            sb.AppendLine("          });");
            sb.AppendLine("        })(markers[i]);");
            sb.AppendLine("      }");
            // Wait for aircraft list then hook events
            sb.AppendLine("      if(!faaAircraftList) { setTimeout(hookAll, 200); return; }");
            // Fetch status when new aircraft appear
            sb.AppendLine("      faaAircraftList.hookAppliedJson(function(newAircraft) {");
            sb.AppendLine("        var regs = [];");
            sb.AppendLine("        newAircraft.foreachAircraft(function(ac) {");
            sb.AppendLine("          var reg = ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("          if(reg && !_iconCache[reg]) {");
            sb.AppendLine("            _iconCache[reg] = null;");
            sb.AppendLine("            regs.push(reg);");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("        if(regs.length) fetchIconStatus(regs);");
            sb.AppendLine("      });");
            // Initial scan of all existing aircraft on page load
            sb.AppendLine("      var initRegs = [];");
            sb.AppendLine("      faaAircraftList.toList().forEach(function(ac) {");
            sb.AppendLine("        var reg = ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("        if(reg && !_iconCache[reg] && (reg.indexOf('N') === 0 || /^C-?[FGI]/.test(reg))) {");
            sb.AppendLine("          _iconCache[reg] = null;");
            sb.AppendLine("          initRegs.push(reg);");
            sb.AppendLine("        }");
            sb.AppendLine("      });");
            sb.AppendLine("      if(initRegs.length) fetchIconStatus(initRegs);");
            // Color aircraft list rows using widget state to map rows to aircraft
            sb.AppendLine("      function colorRows() {");
            sb.AppendLine("        try {");
            sb.AppendLine("        var $el = jQuery('#aircraftList');");
            sb.AppendLine("        if(!$el.length) return;");
            sb.AppendLine("        var state = $el.data('aircraftListPluginState');");
            sb.AppendLine("        if(!state || !state.rowData || !state.tableBody) return;");
            sb.AppendLine("        var selAc = faaAircraftList.getSelectedAircraft();");
            sb.AppendLine("        var rows = state.tableBody[0].getElementsByTagName('tr');");
            sb.AppendLine("        for(var r = 0; r < rows.length; r++) {");
            sb.AppendLine("          var bg = '', st = null;");
            sb.AppendLine("          if(r < state.rowData.length && state.rowData[r].row.visible !== false) {");
            sb.AppendLine("            var acId = state.rowData[r].row.aircraftId;");
            sb.AppendLine("            var ac = faaAircraftList.findAircraftById(acId);");
            sb.AppendLine("            if(ac) {");
            sb.AppendLine("              var reg = ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("              st = reg ? _iconCache[reg] : null;");
            sb.AppendLine("              var isSel = selAc && selAc.id === ac.id;");
            sb.AppendLine("              if(isSel) { bg = '#ffff99'; }");
            sb.AppendLine("              else {");
            sb.AppendLine("                if(st) {");
            sb.AppendLine("                  for(var pi2 = 0; pi2 < _colorPriority.length; pi2++) {");
            sb.AppendLine("                    var ck2 = _colorPriority[pi2];");
            sb.AppendLine("                    if(st[ck2] && _iconColors[ck2]) { bg = _iconColors[ck2][2]; break; }");
            sb.AppendLine("                  }");
            sb.AppendLine("                }");
            sb.AppendLine("              }");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("          var cells = rows[r].cells;");
            sb.AppendLine("          var isLadd = st && st.ladd;");
            sb.AppendLine("          var acCallsign = ac ? (ac.callsign && ac.callsign.val ? ac.callsign.val.toUpperCase() : '') : '';");
            sb.AppendLine("          var acReg = ac ? (ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '') : '';");
            // Find which cell indices are reg and callsign
            sb.AppendLine("          var regIdx = -1, callIdx = -1;");
            sb.AppendLine("          var fmtCall = ac && ac.formatCallsign ? (ac.formatCallsign(false) || '').trim() : '';");
            sb.AppendLine("          for(var c = 0; c < cells.length; c++) {");
            sb.AppendLine("            var ct = (cells[c].textContent || '').trim();");
            sb.AppendLine("            if(acReg && ct === acReg && regIdx < 0) regIdx = c;");
            sb.AppendLine("          }");
            // Find callsign cell: match formatted callsign text, or if callsign equals reg find the OTHER cell with same text
            sb.AppendLine("          if(fmtCall) {");
            sb.AppendLine("            for(var c = 0; c < cells.length; c++) {");
            sb.AppendLine("              if(c === regIdx) continue;");
            sb.AppendLine("              var ct = (cells[c].textContent || '').trim();");
            sb.AppendLine("              if(ct === fmtCall) { callIdx = c; break; }");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            // If callsign is same as reg or blank, check for empty cell next to reg
            sb.AppendLine("          if(callIdx < 0 && regIdx >= 0) {");
            sb.AppendLine("            var adj = [regIdx-1, regIdx+1];");
            sb.AppendLine("            for(var ai = 0; ai < adj.length; ai++) {");
            sb.AppendLine("              if(adj[ai] >= 0 && adj[ai] < cells.length && adj[ai] !== regIdx) {");
            sb.AppendLine("                var at = (cells[adj[ai]].textContent || '').trim();");
            sb.AppendLine("                if(at === '' || at === acReg) { callIdx = adj[ai]; break; }");
            sb.AppendLine("              }");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            // Find model cells (ICAO type code and/or model name)
            sb.AppendLine("          var mdlIdxs = {};");
            sb.AppendLine("          function stripPunc(s) { return s.replace(/[^A-Z0-9]/gi, '').toUpperCase(); }");
            sb.AppendLine("          if(ac) {");
            sb.AppendLine("            var acMdlIcao = ac.modelIcao && ac.modelIcao.val ? ac.modelIcao.val.toUpperCase().trim() : '';");
            sb.AppendLine("            var acModel = ac.model && ac.model.val ? ac.model.val.trim() : '';");
            sb.AppendLine("            var acMdlStrip = stripPunc(acMdlIcao);");
            sb.AppendLine("            var acModelStrip = stripPunc(acModel);");
            sb.AppendLine("            for(var c = 0; c < cells.length; c++) {");
            sb.AppendLine("              if(c === regIdx || c === callIdx) continue;");
            sb.AppendLine("              var ct = (cells[c].textContent || '').trim();");
            sb.AppendLine("              if(!ct) continue;");
            sb.AppendLine("              var ctStrip = stripPunc(ct);");
            // Exact matches
            sb.AppendLine("              if(acMdlIcao && ct.toUpperCase() === acMdlIcao) { mdlIdxs[c] = true; continue; }");
            sb.AppendLine("              if(acModel && ct === acModel) { mdlIdxs[c] = true; continue; }");
            // Stripped comparison (ignores hyphens, spaces, etc.)
            sb.AppendLine("              if(acMdlStrip && acMdlStrip.length >= 2 && (ctStrip.indexOf(acMdlStrip) >= 0 || acMdlStrip.indexOf(ctStrip) >= 0)) { mdlIdxs[c] = true; continue; }");
            sb.AppendLine("              if(acModelStrip && acModelStrip.length >= 3 && (ctStrip.indexOf(acModelStrip) >= 0 || acModelStrip.indexOf(ctStrip) >= 0)) { mdlIdxs[c] = true; continue; }");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            // Find operator cell
            sb.AppendLine("          var oprIdx = -1;");
            sb.AppendLine("          if(ac) {");
            sb.AppendLine("            var acOpr = ac.operator && ac.operator.val ? ac.operator.val.trim() : '';");
            sb.AppendLine("            if(acOpr) {");
            sb.AppendLine("              for(var c = 0; c < cells.length; c++) {");
            sb.AppendLine("                if(c === regIdx || c === callIdx || mdlIdxs[c]) continue;");
            sb.AppendLine("                if((cells[c].textContent || '').trim() === acOpr) { oprIdx = c; break; }");
            sb.AppendLine("              }");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("          var isModelMatch = st && st.mdl;");
            sb.AppendLine("          var isPilotMatch = st && (st.pilotRaw || st.pilot);");
            sb.AppendLine("          var mode = _rowMode;");
            sb.AppendLine("          for(var c = 0; c < cells.length; c++) {");
            sb.AppendLine("            var isRegCell = c === regIdx;");
            sb.AppendLine("            var isCallCell = c === callIdx;");
            sb.AppendLine("            var isMdlCell = !!mdlIdxs[c];");
            sb.AppendLine("            var isOprCell = c === oprIdx;");
            // Model cells: colored for model matches when mdl option enabled
            sb.AppendLine("            var colorMdl = isMdlCell && isModelMatch && _mdlRowMode.indexOf('mdl') >= 0;");
            sb.AppendLine("            var colorOpr = isOprCell && isPilotMatch;");
            sb.AppendLine("            var colorThis = mode === 'row' || (isRegCell && mode.indexOf('reg') >= 0) || (isCallCell && mode.indexOf('call') >= 0) || colorMdl || colorOpr;");
            sb.AppendLine("            cells[c].style.backgroundColor = isSel ? bg : (colorOpr ? '#e0ffe0' : (colorThis ? bg : ''));");
            sb.AppendLine("            cells[c].style.color = (isLadd && (isRegCell || isCallCell)) ? _laddColor : '';");
            sb.AppendLine("          }");
            sb.AppendLine("        }");
            sb.AppendLine("        } catch(e) { console.error('colorRows error:', e); }");
            sb.AppendLine("      }");
            sb.AppendLine("      faaAircraftList.hookSelectedAircraftChanged(colorRows);");
            sb.AppendLine("      faaAircraftList.hookUpdated(colorRows);");
            sb.AppendLine("      setInterval(colorRows, 1000);");
            // LADD circles on map
            sb.AppendLine("      var _laddCircles = {};");
            sb.AppendLine("      var _leafletMap = null;");
            sb.AppendLine("      try {");
            sb.AppendLine("        var mapJQ = jQuery('#map');");
            sb.AppendLine("        if(mapJQ.length && VRS.jQueryUIHelper && VRS.jQueryUIHelper.getMapPlugin) {");
            sb.AppendLine("          var mp = VRS.jQueryUIHelper.getMapPlugin(mapJQ);");
            sb.AppendLine("          if(mp) try { _leafletMap = mp.getNative(); } catch(e) {}");
            sb.AppendLine("        }");
            sb.AppendLine("      } catch(e) {}");
            sb.AppendLine("      function updateLaddCircles() {");
            sb.AppendLine("        if(!_leafletMap || typeof L === 'undefined') return;");
            sb.AppendLine("        try {");
            sb.AppendLine("        var seen = {};");
            sb.AppendLine("        faaAircraftList.toList().forEach(function(ac) {");
            sb.AppendLine("          var reg = ac.formatRegistration ? (ac.formatRegistration() || '').toUpperCase() : '';");
            sb.AppendLine("          var st = reg ? _iconCache[reg] : null;");
            sb.AppendLine("          if(st && st.ladd && ac.hasPosition && ac.hasPosition()) {");
            sb.AppendLine("            var pos = ac.getPosition();");
            sb.AppendLine("            seen[reg] = true;");
            sb.AppendLine("            if(_laddCircles[reg]) {");
            sb.AppendLine("              _laddCircles[reg].setLatLng([pos.lat, pos.lng]);");
            sb.AppendLine("            } else {");
            sb.AppendLine("              _laddCircles[reg] = L.circleMarker([pos.lat, pos.lng], {radius:14, color:_laddColor, weight:2, fill:false, opacity:0.8}).addTo(_leafletMap);");
            sb.AppendLine("            }");
            sb.AppendLine("          }");
            sb.AppendLine("        });");
            sb.AppendLine("        for(var k in _laddCircles) {");
            sb.AppendLine("          if(!seen[k]) { _leafletMap.removeLayer(_laddCircles[k]); delete _laddCircles[k]; }");
            sb.AppendLine("        }");
            sb.AppendLine("        } catch(e) {}");
            sb.AppendLine("      }");
            sb.AppendLine("      setInterval(updateLaddCircles, 2000);");
            sb.AppendLine("      } catch(e) { console.error('RegistrationData hook error:', e); setTimeout(hookAll, 500); }");
            sb.AppendLine("    }, 500);");
            sb.AppendLine("  });");

            sb.AppendLine("})();");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}
