using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;

namespace VirtualRadar.Plugin.RegistrationData
{
    class DatabaseDownloader : IDisposable
    {
        internal const string DefaultAircraftZipUrl = "https://registry.faa.gov/database/ReleasableAircraft.zip";
        internal const string DefaultAirmenZipUrlPattern = "https://registry.faa.gov/database/CS{0}{1}.zip";
        internal const string DefaultCcarPageUrl = "https://wwwapps.tc.gc.ca/Saf-Sec-Sur/2/CCARCS-RIACC/DDZip.aspx";
        internal const string NtsbBaseUrl = "https://data.ntsb.gov/avdata/FileDirectory/DownloadFile?fileID=C%3A%5Cavdata%5C";
        internal const string DefaultNtsbZipUrl = NtsbBaseUrl + "avall.zip";
        // FAA migrated the SDR download host from "external-api.faa.gov"
        // to "external.apic4e.faa.gov" (verified live against
        // https://www.faa.gov/av-info/download_SDR).  The legacy host
        // returns 404 for every year; using anything starting with
        // external-api.faa.gov/sdrs/ now produces empty downloads.
        internal const string SdrCsvBaseUrl = "https://external.apic4e.faa.gov/sdrs/retrieve/SDR-";
        internal const string DefaultCasaUrl = "https://services.casa.gov.au/CSV/acrftreg.zip";
        internal const string DefaultNzcaaUrl = "https://www.aviation.govt.nz/assets/aircraft/aircraft-register/Aircraft-Register-for-website-.csv";

        private Options _CurrentOptions;

        private readonly RegistrationDatabase _Database;
        private readonly Plugin _Plugin;
        private Timer _Timer;
        private volatile bool _IsDownloadingAircraft;
        private volatile bool _IsDownloadingAirmen;
        private volatile bool _IsDownloadingCcar;
        private volatile bool _IsDownloadingNtsb;
        private volatile bool _IsDownloadingSdr;
        private volatile bool _IsDownloadingCasa;

        public bool IsDownloading { get { return _IsDownloadingAircraft || _IsDownloadingAirmen || _IsDownloadingCcar || _IsDownloadingNtsb || _IsDownloadingSdr || _IsDownloadingCasa || _IsDownloadingNzcaa; } }
        public bool IsDownloadingAircraft { get { return _IsDownloadingAircraft; } }
        public bool IsDownloadingAirmen { get { return _IsDownloadingAirmen; } }
        public bool IsDownloadingCcar { get { return _IsDownloadingCcar; } }
        public bool IsDownloadingNtsb { get { return _IsDownloadingNtsb; } }
        public bool IsDownloadingSdr { get { return _IsDownloadingSdr; } }
        public bool IsDownloadingCasa { get { return _IsDownloadingCasa; } }

        private volatile string _AircraftPhase = "";
        private volatile int _AircraftProgress;
        private volatile string _AirmenPhase = "";
        private volatile int _AirmenProgress;

        private volatile string _NtsbPhase = "";
        private volatile int _NtsbProgress;

        private volatile string _SdrPhase = "";
        private volatile int _SdrProgress;

        private volatile string _CasaPhase = "";
        private volatile int _CasaProgress;

        public string AircraftPhase { get { return _AircraftPhase; } }
        public int AircraftProgress { get { return _AircraftProgress; } }
        public string AirmenPhase { get { return _AirmenPhase; } }
        public int AirmenProgress { get { return _AirmenProgress; } }
        public string NtsbPhase { get { return _NtsbPhase; } }
        public int NtsbProgress { get { return _NtsbProgress; } }
        public string SdrPhase { get { return _SdrPhase; } }
        public int SdrProgress { get { return _SdrProgress; } }
        public string CasaPhase { get { return _CasaPhase; } }
        public int CasaProgress { get { return _CasaProgress; } }

        public DatabaseDownloader(RegistrationDatabase database, Plugin plugin)
        {
            _Database = database;
            _Plugin = plugin;
        }

        private string GetAircraftUrl()
        {
            var url = _CurrentOptions?.AircraftDownloadUrl;
            return string.IsNullOrWhiteSpace(url) ? DefaultAircraftZipUrl : url;
        }

        private string GetAirmenUrl()
        {
            var url = _CurrentOptions?.AirmenDownloadUrl;
            if(!string.IsNullOrWhiteSpace(url)) return url;
            var now = DateTime.UtcNow;
            return string.Format(DefaultAirmenZipUrlPattern, now.ToString("MM"), now.ToString("yyyy"));
        }

        private string GetAirmenFallbackUrl()
        {
            var url = _CurrentOptions?.AirmenDownloadUrl;
            if(!string.IsNullOrWhiteSpace(url)) return null; // custom URL, no fallback
            var prev = DateTime.UtcNow.AddMonths(-1);
            return string.Format(DefaultAirmenZipUrlPattern, prev.ToString("MM"), prev.ToString("yyyy"));
        }

        private string GetCcarUrl()
        {
            var url = _CurrentOptions?.CcarDownloadUrl;
            return string.IsNullOrWhiteSpace(url) ? DefaultCcarPageUrl : url;
        }

        private string GetNtsbUrl()
        {
            var url = _CurrentOptions?.NtsbDownloadUrl;
            return string.IsNullOrWhiteSpace(url) ? DefaultNtsbZipUrl : url;
        }

        public void Start(Options options)
        {
            _CurrentOptions = options;
            _Timer = new Timer(TimerCallback, options, TimeSpan.FromSeconds(10), TimeSpan.FromHours(6));
        }

        public void Stop()
        {
            if(_Timer != null) {
                _Timer.Dispose();
                _Timer = null;
            }
        }

        private void TimerCallback(object state)
        {
            var options = state as Options;
            if(options == null) return;
            if(!options.EnableAutomaticDownloads) return;

            try {
                var now = DateTime.UtcNow;
                var counts = _Database.GetRecordCounts();
                long regCount, airmenCount;
                counts.TryGetValue("aircraft_registration", out regCount);
                counts.TryGetValue("airmen_basic", out airmenCount);

                bool needsAircraft = regCount == 0 ||
                    options.LastAircraftDownload == DateTime.MinValue ||
                    (now - options.LastAircraftDownload).TotalDays >= options.AircraftUpdateIntervalDays;
                bool needsAirmen = airmenCount == 0 ||
                    options.LastAirmenDownload == DateTime.MinValue ||
                    (now - options.LastAirmenDownload).TotalDays >= options.AirmenUpdateIntervalDays;

                long ccarCount;
                counts.TryGetValue("ccar_aircraft", out ccarCount);
                bool needsCcar = ccarCount == 0 ||
                    options.LastCcarDownload == DateTime.MinValue ||
                    (now - options.LastCcarDownload).TotalDays >= options.CcarUpdateIntervalDays;

                long ntsbCount;
                counts.TryGetValue("ntsb_event", out ntsbCount);
                bool needsNtsb = ntsbCount == 0 ||
                    options.LastNtsbDownload == DateTime.MinValue ||
                    (now - options.LastNtsbDownload).TotalDays >= options.NtsbUpdateIntervalDays;

                if(needsAircraft) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadAircraftDatabase());
                }
                if(needsAirmen) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadAirmenDatabase());
                }
                if(needsCcar) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadCcarDatabase());
                }
                if(needsNtsb) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadNtsbDatabase());
                }

                long sdrCount;
                counts.TryGetValue("sdr_report", out sdrCount);
                bool needsSdr = sdrCount == 0 ||
                    options.LastSdrDownload == DateTime.MinValue ||
                    (now - options.LastSdrDownload).TotalDays >= options.SdrUpdateIntervalDays;

                if(needsSdr) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadSdrDatabase());
                }

                long casaCount;
                counts.TryGetValue("casa_aircraft", out casaCount);
                bool needsCasa = casaCount == 0 ||
                    options.LastCasaDownload == DateTime.MinValue ||
                    (now - options.LastCasaDownload).TotalDays >= options.CasaUpdateIntervalDays;

                if(needsCasa) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadCasaDatabase());
                }

                long nzcaaCount;
                counts.TryGetValue("nzcaa_aircraft", out nzcaaCount);
                bool needsNzcaa = nzcaaCount == 0 ||
                    options.LastNzcaaDownload == DateTime.MinValue ||
                    (now - options.LastNzcaaDownload).TotalDays >= options.NzcaaUpdateIntervalDays;

                if(needsNzcaa) {
                    ThreadPool.QueueUserWorkItem(_ => DownloadNzcaaDatabase());
                }
            } catch(Exception ex) {
                _Plugin.StatusDescription = "Timer error: " + ex.Message;
            }
        }

        public void DownloadAircraftDatabase()
        {
            if(_IsDownloadingAircraft) return;
            _IsDownloadingAircraft = true;

            try {
                _AircraftPhase = "Downloading...";
                _AircraftProgress = 5;
                _Plugin.StatusDescription = "Downloading aircraft registration database...";
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_Aircraft_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    var zipPath = Path.Combine(tempDir, "ReleasableAircraft.zip");
                    DownloadFile(GetAircraftUrl(), zipPath);

                    _AircraftPhase = "Extracting...";
                    _AircraftProgress = 20;
                    _Plugin.StatusDescription = "Extracting aircraft data...";
                    ZipFile.ExtractToDirectory(zipPath, tempDir);

                    _Database.DropAircraftTables();
                    _Database.EnsureAircraftSchema();

                    var masterFile = FindFile(tempDir, "MASTER.txt");
                    if(masterFile != null) {
                        _AircraftPhase = "Importing registrations...";
                        _AircraftProgress = 30;
                        _Plugin.StatusDescription = "Importing aircraft registrations...";
                        ImportMasterFile(masterFile, p => { _AircraftProgress = 30 + (int)(p * 50); });
                    }

                    var acftrefFile = FindFile(tempDir, "ACFTREF.txt");
                    if(acftrefFile != null) {
                        _AircraftPhase = "Importing aircraft refs...";
                        _AircraftProgress = 85;
                        _Plugin.StatusDescription = "Importing aircraft references...";
                        ImportAcftRefFile(acftrefFile);
                    }

                    var engineFile = FindFile(tempDir, "ENGINE.txt");
                    if(engineFile != null) {
                        _AircraftPhase = "Importing engines...";
                        _AircraftProgress = 92;
                        _Plugin.StatusDescription = "Importing engine references...";
                        ImportEngineFile(engineFile);
                    }

                    _AircraftProgress = 98;
                    var options = OptionsStorage.Load(_Plugin);
                    options.LastAircraftDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    var counts = _Database.GetRecordCounts();
                    long regCount;
                    counts.TryGetValue("aircraft_registration", out regCount);
                    _AircraftPhase = $"Complete: {regCount:N0} registrations";
                    _AircraftProgress = 100;
                    _Plugin.StatusDescription = $"Aircraft DB updated: {regCount:N0} registrations";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _AircraftPhase = "Failed: " + ex.Message;
                _AircraftProgress = 0;
                _Plugin.StatusDescription = "Aircraft download failed: " + ex.Message;
            } finally {
                _IsDownloadingAircraft = false;
            }
        }

        public void DownloadAirmenDatabase()
        {
            if(_IsDownloadingAirmen) return;
            _IsDownloadingAirmen = true;

            try {
                _AirmenPhase = "Downloading...";
                _AirmenProgress = 5;
                _Plugin.StatusDescription = "Downloading airmen certification database...";
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_Airmen_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    var url = GetAirmenUrl();
                    var zipPath = Path.Combine(tempDir, "airmen.zip");

                    try {
                        DownloadFile(url, zipPath);
                    } catch {
                        // Try previous month if current month's file isn't available yet
                        var fallback = GetAirmenFallbackUrl();
                        if(fallback != null) {
                            DownloadFile(fallback, zipPath);
                        } else throw;
                    }

                    _AirmenPhase = "Extracting...";
                    _AirmenProgress = 20;
                    _Plugin.StatusDescription = "Extracting airmen data...";
                    ZipFile.ExtractToDirectory(zipPath, tempDir);

                    _Database.DropAirmenTables();
                    _Database.EnsureAirmenSchema();

                    _AirmenPhase = "Importing airmen...";
                    _AirmenProgress = 30;
                    _Plugin.StatusDescription = "Importing airmen records...";
                    ImportAirmenFiles(tempDir, p => { _AirmenProgress = 30 + (int)(p * 65); _AirmenPhase = $"Importing... {_AirmenProgress}%"; });

                    _AirmenProgress = 98;
                    var options = OptionsStorage.Load(_Plugin);
                    options.LastAirmenDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    var counts = _Database.GetRecordCounts();
                    long airmenCount;
                    counts.TryGetValue("airmen_basic", out airmenCount);
                    _AirmenPhase = $"Complete: {airmenCount:N0} records";
                    _AirmenProgress = 100;
                    _Plugin.StatusDescription = $"Airmen DB updated: {airmenCount:N0} records";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _AirmenPhase = "Failed: " + ex.Message;
                _AirmenProgress = 0;
                _Plugin.StatusDescription = "Airmen download failed: " + ex.Message;
            } finally {
                _IsDownloadingAirmen = false;
            }
        }

        public void DownloadCcarDatabase()
        {
            if(_IsDownloadingCcar) return;
            _IsDownloadingCcar = true;

            try {
                _Plugin.StatusDescription = "Downloading CCAR database...";
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_CCAR_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    var zipPath = Path.Combine(tempDir, "ccar.zip");
                    DownloadCcarZip(zipPath);

                    _Plugin.StatusDescription = "Extracting CCAR data...";
                    ZipFile.ExtractToDirectory(zipPath, tempDir);

                    _Database.DropCcarTables();
                    _Database.EnsureCcarSchema();

                    var curFile = FindFile(tempDir, "carscurr.txt");
                    if(curFile != null) {
                        _Plugin.StatusDescription = "Importing CCAR aircraft...";
                        ImportCcarAircraftFile(curFile);
                    }

                    var ownFile = FindFile(tempDir, "carsownr.txt");
                    if(ownFile != null) {
                        _Plugin.StatusDescription = "Importing CCAR owners...";
                        ImportCcarOwnerFile(ownFile);
                    }

                    var options = OptionsStorage.Load(_Plugin);
                    options.LastCcarDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    var counts = _Database.GetRecordCounts();
                    long ccarCount;
                    counts.TryGetValue("ccar_aircraft", out ccarCount);
                    _Plugin.StatusDescription = $"CCAR DB updated: {ccarCount:N0} aircraft";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _Plugin.StatusDescription = "CCAR download failed: " + ex.Message;
            } finally {
                _IsDownloadingCcar = false;
            }
        }

        public void DownloadNtsbDatabase()
        {
            if(_IsDownloadingNtsb) return;
            _IsDownloadingNtsb = true;

            try {
                _NtsbPhase = "Downloading current data...";
                _NtsbProgress = 2;
                _Plugin.StatusDescription = "Downloading NTSB accident database...";
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_NTSB_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    _Database.EnsureNtsbSchema();

                    // Check if we already have pre-2008 data
                    bool hasPre2008 = false;
                    try {
                        var conn = _Database.GetNtsbConnection();
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = "SELECT COUNT(*) FROM ntsb_event WHERE ev_date LIKE '%-07 %' OR ev_date LIKE '%-06 %' OR ev_date LIKE '%-05 %' OR ev_date LIKE '%-04 %' OR ev_date LIKE '%-03 %' OR ev_date LIKE '%-02 %' OR ev_date LIKE '%-01 %' OR ev_date LIKE '%-00 %' OR ev_date LIKE '%-99 %' OR ev_date LIKE '%-98 %'";
                            hasPre2008 = (long)cmd.ExecuteScalar() > 100;
                        }
                    } catch { }

                    // Download avall.zip (2008+) — always needed for updates
                    var avallDir = Path.Combine(tempDir, "avall");
                    Directory.CreateDirectory(avallDir);
                    var avallZip = Path.Combine(tempDir, "avall.zip");
                    DownloadFile(GetNtsbUrl(), avallZip);

                    string pre2008Mdb = null;
                    if(!hasPre2008) {
                        _NtsbPhase = "Downloading pre-2008 data...";
                        _NtsbProgress = 10;
                        _Plugin.StatusDescription = "Downloading NTSB pre-2008 database...";

                        var pre2008Dir = Path.Combine(tempDir, "pre2008");
                        Directory.CreateDirectory(pre2008Dir);
                        var pre2008Zip = Path.Combine(tempDir, "Pre2008.zip");
                        DownloadFile(NtsbBaseUrl + "Pre2008.zip", pre2008Zip);
                        ZipFile.ExtractToDirectory(pre2008Zip, pre2008Dir);
                        pre2008Mdb = FindMdb(pre2008Dir);
                    } else {
                        _NtsbPhase = "Pre-2008 data already present, skipping...";
                    }

                    _NtsbPhase = "Extracting current data...";
                    _NtsbProgress = 20;
                    _Plugin.StatusDescription = "Extracting NTSB data...";
                    ZipFile.ExtractToDirectory(avallZip, avallDir);

                    var avallMdb = FindMdb(avallDir);

                    if(avallMdb == null && pre2008Mdb == null) {
                        _NtsbPhase = "Failed: MDB file not found in download";
                        _NtsbProgress = 0;
                        _Plugin.StatusDescription = "NTSB download failed: no MDB file found";
                        return;
                    }

                    // Only drop if we're doing a full rebuild (including pre-2008)
                    if(!hasPre2008) {
                        _Database.DropNtsbTables();
                        _Database.EnsureNtsbSchema();
                    }

                    // Import pre-2008 data first (only if not already present)
                    if(pre2008Mdb != null) {
                        _NtsbPhase = "Importing pre-2008 events...";
                        _NtsbProgress = 30;
                        _Plugin.StatusDescription = "Importing NTSB pre-2008 records...";
                        ImportNtsbMdb(pre2008Mdb);
                    }

                    // Import current data (2008+) — delete old 2008+ and re-import
                    if(avallMdb != null) {
                        if(hasPre2008) {
                            // Remove old 2008+ data before re-importing
                            try {
                                var conn = _Database.GetNtsbConnection();
                                using(var cmd = conn.CreateCommand()) {
                                    cmd.CommandText = "DELETE FROM ntsb_event WHERE ev_date LIKE '%-08 %' OR ev_date LIKE '%-09 %' OR ev_date LIKE '%-1_ %' OR ev_date LIKE '%-2_ %'";
                                    cmd.ExecuteNonQuery();
                                }
                            } catch { }
                        }
                        _NtsbPhase = "Importing 2008+ events...";
                        _NtsbProgress = 60;
                        _Plugin.StatusDescription = "Importing NTSB 2008+ records...";
                        ImportNtsbMdb(avallMdb);
                    }

                    _NtsbProgress = 98;
                    var options = OptionsStorage.Load(_Plugin);
                    options.LastNtsbDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    var counts = _Database.GetRecordCounts();
                    long ntsbCount;
                    counts.TryGetValue("ntsb_event", out ntsbCount);
                    _NtsbPhase = $"Complete: {ntsbCount:N0} events";
                    _NtsbProgress = 100;
                    _Plugin.StatusDescription = $"NTSB DB updated: {ntsbCount:N0} events";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _NtsbPhase = "Failed: " + ex.Message;
                _NtsbProgress = 0;
                _Plugin.StatusDescription = "NTSB download failed: " + ex.Message;
            } finally {
                _IsDownloadingNtsb = false;
            }
        }

        private void ImportNtsbMdb(string mdbPath)
        {
            using(var mdb = NtsbMdbReader.Open(mdbPath)) {
                // First, discover actual column names in each table
                var evCols = mdb.GetColumns("events");
                var acftCols = mdb.GetColumns("aircraft");
                var narrCols = mdb.GetColumns("narratives");

                // Read narratives into a lookup dict (ev_id -> probable cause)
                _NtsbPhase = "Reading narratives...";
                _NtsbProgress = 35;
                var narratives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var causeCol = FindColumn(narrCols, "narr_cause", "cause", "probable_cause");
                var narrEvCol = FindColumn(narrCols, "ev_id", "event_id");
                if(causeCol != null && narrEvCol != null) {
                    try {
                        foreach(var row in mdb.ReadRows("narratives")) {
                            string evId, cause;
                            row.TryGetValue(narrEvCol, out evId);
                            row.TryGetValue(causeCol, out cause);
                            if(!string.IsNullOrEmpty(evId) && !string.IsNullOrEmpty(cause)) {
                                narratives[evId] = cause;
                            }
                        }
                    } catch { /* narratives table may not exist in some versions */ }
                }

                // Build event column map using only columns that actually exist
                _NtsbPhase = "Reading events...";
                _NtsbProgress = 45;
                var evColMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var wantedEvCols = new[] {
                    "ev_id", "ntsb_no", "ev_date", "ev_city", "ev_state", "ev_country",
                    "ev_type", "ev_highest_injury", "inj_tot_f", "inj_tot_s", "inj_tot_m", "inj_tot_n",
                    "latitude", "longitude", "apt_name", "light_cond", "sky_cond_nonceil",
                    "wind_vel_kts", "wx_brief_comp"
                };
                foreach(var wanted in wantedEvCols) {
                    var actual = FindColumn(evCols, wanted);
                    if(actual != null) evColMap[wanted] = actual;
                }

                var events = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                string evIdActualCol;
                evColMap.TryGetValue("ev_id", out evIdActualCol);
                if(evIdActualCol != null) {
                    foreach(var row in mdb.ReadRows("events")) {
                        string evId;
                        if(!row.TryGetValue(evIdActualCol, out evId) || string.IsNullOrEmpty(evId)) continue;
                        var ev = new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase);
                        events[evId] = ev;
                    }
                }

                // Build aircraft column map using only columns that actually exist
                _NtsbPhase = "Importing aircraft events...";
                _NtsbProgress = 60;
                var acftColMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var wantedAcftCols = new[] {
                    "ev_id", "regis_no", "ntsb_no", "acft_make", "acft_model",
                    "acft_series", "acft_category", "damage", "far_part", "oper_name", "phase_flt_spec_code"
                };
                foreach(var wanted in wantedAcftCols) {
                    var actual = FindColumn(acftCols, wanted);
                    if(actual != null) acftColMap[wanted] = actual;
                }

                var sqliteConn = _Database.GetNtsbConnection();
                int count = 0;

                using(var transaction = sqliteConn.BeginTransaction()) {
                    using(var insertCmd = sqliteConn.CreateCommand()) {
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = @"INSERT INTO ntsb_event
                            (ev_id, ntsb_no, ev_date, ev_city, ev_state, ev_country,
                             ev_type, ev_highest_injury, inj_tot_f, inj_tot_s, inj_tot_m, inj_tot_n,
                             latitude, longitude, regis_no, acft_make, acft_model, acft_series,
                             acft_category, damage, far_part, oper_name, phase_flt_spec,
                             narr_cause, apt_name, light_cond, sky_cond_nonceil, wind_vel_kts, wx_brief_comp)
                            VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,
                                    @p12,@p13,@p14,@p15,@p16,@p17,@p18,@p19,@p20,@p21,@p22,
                                    @p23,@p24,@p25,@p26,@p27,@p28)";

                        for(int i = 0; i <= 28; i++) {
                            insertCmd.AddParam("@p" + i);
                        }

                        foreach(var acftRow in mdb.ReadRows("aircraft")) {
                            string GetAcft(string wanted) {
                                string actualCol;
                                if(!acftColMap.TryGetValue(wanted, out actualCol)) return "";
                                string val;
                                return acftRow.TryGetValue(actualCol, out val) ? val : "";
                            }

                            var evId = GetAcft("ev_id");
                            var regisNo = GetAcft("regis_no");

                            if(string.IsNullOrEmpty(regisNo)) continue;

                            Dictionary<string, string> ev;
                            if(!events.TryGetValue(evId, out ev)) continue;

                            string GetEv(string wanted) {
                                string actualCol;
                                if(!evColMap.TryGetValue(wanted, out actualCol)) return "";
                                string val;
                                return ev.TryGetValue(actualCol, out val) ? val : "";
                            }

                            string cause;
                            narratives.TryGetValue(evId, out cause);

                            var ntsbNo = GetAcft("ntsb_no");
                            if(string.IsNullOrEmpty(ntsbNo)) ntsbNo = GetEv("ntsb_no");

                            insertCmd.Parameters["@p0"].Value = evId;
                            insertCmd.Parameters["@p1"].Value = ntsbNo;
                            insertCmd.Parameters["@p2"].Value = GetEv("ev_date");
                            insertCmd.Parameters["@p3"].Value = GetEv("ev_city");
                            insertCmd.Parameters["@p4"].Value = GetEv("ev_state");
                            insertCmd.Parameters["@p5"].Value = GetEv("ev_country");
                            insertCmd.Parameters["@p6"].Value = GetEv("ev_type");
                            insertCmd.Parameters["@p7"].Value = GetEv("ev_highest_injury");
                            insertCmd.Parameters["@p8"].Value = GetEv("inj_tot_f");
                            insertCmd.Parameters["@p9"].Value = GetEv("inj_tot_s");
                            insertCmd.Parameters["@p10"].Value = GetEv("inj_tot_m");
                            insertCmd.Parameters["@p11"].Value = GetEv("inj_tot_n");
                            insertCmd.Parameters["@p12"].Value = GetEv("latitude");
                            insertCmd.Parameters["@p13"].Value = GetEv("longitude");
                            insertCmd.Parameters["@p14"].Value = regisNo;
                            insertCmd.Parameters["@p15"].Value = GetAcft("acft_make");
                            insertCmd.Parameters["@p16"].Value = GetAcft("acft_model");
                            insertCmd.Parameters["@p17"].Value = GetAcft("acft_series");
                            insertCmd.Parameters["@p18"].Value = GetAcft("acft_category");
                            insertCmd.Parameters["@p19"].Value = GetAcft("damage");
                            insertCmd.Parameters["@p20"].Value = GetAcft("far_part");
                            insertCmd.Parameters["@p21"].Value = GetAcft("oper_name");
                            insertCmd.Parameters["@p22"].Value = GetAcft("phase_flt_spec_code");
                            insertCmd.Parameters["@p23"].Value = cause ?? "";
                            insertCmd.Parameters["@p24"].Value = GetEv("apt_name");
                            insertCmd.Parameters["@p25"].Value = GetEv("light_cond");
                            insertCmd.Parameters["@p26"].Value = GetEv("sky_cond_nonceil");
                            insertCmd.Parameters["@p27"].Value = GetEv("wind_vel_kts");
                            insertCmd.Parameters["@p28"].Value = GetEv("wx_brief_comp");

                            insertCmd.ExecuteNonQuery();
                            count++;

                            if(count % 5000 == 0) {
                                _NtsbProgress = 60 + Math.Min(35, count / 2000);
                                _NtsbPhase = $"Importing... {count:N0} events";
                                _Plugin.StatusDescription = $"Importing NTSB events... {count:N0}";
                            }
                        }
                    }
                    transaction.Commit();
                }

                _Plugin.StatusDescription = $"NTSB import complete: {count:N0} events";
            }
        }

        private static string FindMdb(string directory)
        {
            var mdbFiles = Directory.GetFiles(directory, "*.mdb", SearchOption.AllDirectories);
            return mdbFiles.Length > 0 ? mdbFiles[0] : null;
        }

        private static string FindColumn(HashSet<string> columns, params string[] candidates)
        {
            foreach(var c in candidates) {
                if(columns.Contains(c)) return c;
            }
            // Try case-insensitive partial match
            foreach(var c in candidates) {
                foreach(var actual in columns) {
                    if(string.Equals(actual, c, StringComparison.OrdinalIgnoreCase)) return actual;
                }
            }
            return null;
        }

        private void DownloadCcarZip(string destPath)
        {
            var cookieContainer = new CookieContainer();

            // GET the page to extract ASP.NET form fields
            var ccarUrl = GetCcarUrl();
            var getReq = (HttpWebRequest)WebRequest.Create(ccarUrl);
            getReq.CookieContainer = cookieContainer;
            getReq.UserAgent = "VirtualRadarServer-RegistrationData-Plugin/1.0";
            string pageHtml;
            using(var resp = getReq.GetResponse())
            using(var reader = new StreamReader(resp.GetResponseStream())) {
                pageHtml = reader.ReadToEnd();
            }

            var viewState = ExtractFormValue(pageHtml, "__VIEWSTATE");
            var viewStateGen = ExtractFormValue(pageHtml, "__VIEWSTATEGENERATOR");
            var eventValidation = ExtractFormValue(pageHtml, "__EVENTVALIDATION");

            // POST to download
            var postData = "__VIEWSTATE=" + Uri.EscapeDataString(viewState)
                + "&__VIEWSTATEGENERATOR=" + Uri.EscapeDataString(viewStateGen)
                + "&__EVENTVALIDATION=" + Uri.EscapeDataString(eventValidation)
                + "&ctl00%24ContentPlaceHolder1%24btnDownload=Download";

            var postReq = (HttpWebRequest)WebRequest.Create(ccarUrl);
            postReq.Method = "POST";
            postReq.CookieContainer = cookieContainer;
            postReq.ContentType = "application/x-www-form-urlencoded";
            postReq.UserAgent = "VirtualRadarServer-RegistrationData-Plugin/1.0";

            var postBytes = System.Text.Encoding.UTF8.GetBytes(postData);
            postReq.ContentLength = postBytes.Length;
            using(var reqStream = postReq.GetRequestStream()) {
                reqStream.Write(postBytes, 0, postBytes.Length);
            }

            using(var resp = postReq.GetResponse())
            using(var stream = resp.GetResponseStream())
            using(var fs = new FileStream(destPath, FileMode.Create)) {
                stream.CopyTo(fs);
            }
        }

        private static string ExtractFormValue(string html, string fieldId)
        {
            // Find id="fieldId" ... value="..."
            var idStr = "id=\"" + fieldId + "\"";
            var idx = html.IndexOf(idStr, StringComparison.OrdinalIgnoreCase);
            if(idx < 0) return "";

            var valueStr = "value=\"";
            var vIdx = html.IndexOf(valueStr, idx, StringComparison.OrdinalIgnoreCase);
            if(vIdx < 0) return "";

            var start = vIdx + valueStr.Length;
            var end = html.IndexOf('"', start);
            if(end < 0) return "";

            return System.Net.WebUtility.HtmlDecode(html.Substring(start, end - start));
        }

        private void ImportCcarAircraftFile(string filePath)
        {
            var conn = _Database.GetCcarConnection();
            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO ccar_aircraft
                        (mark, common_name, model_name, serial_number, manufacturer_name,
                         aircraft_category, engine_category, number_of_engines, number_of_seats,
                         air_weight_kg, issue_date, effective_date, ineffective_date,
                         registered_purpose, flight_authority, province_state, city, registration_status)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17)";

                    for(int i = 0; i <= 17; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    // Field indices from carscurr.txt:
                    // 0=mark, 3=common_name(make), 4=model, 5=serial, 7=manufacturer,
                    // 10=aircraft_cat_e, 15=engine_cat_e, 17=num_engines, 18=num_seats, 19=weight_kg,
                    // 21=issue_date, 22=effective_date, 23=ineffective_date, 24=purpose_e,
                    // 26=flight_authority_e, 34=province_e, 36=city, 38=status_e
                    var fieldMap = new[] { 0, 3, 4, 5, 7, 10, 15, 17, 18, 19, 21, 22, 23, 24, 26, 34, 36, 38 };

                    int count = 0;
                    foreach(var line in File.ReadLines(filePath, System.Text.Encoding.GetEncoding(1252))) {
                        var fields = ParseCsvLine(line);
                        if(fields.Length < 39) continue;

                        for(int i = 0; i < fieldMap.Length; i++) {
                            cmd.Parameters["@p" + i].Value = fields[fieldMap[i]].Trim();
                        }

                        cmd.ExecuteNonQuery();
                        count++;

                        if(count % 5000 == 0) {
                            _Plugin.StatusDescription = $"Importing CCAR aircraft... {count:N0}";
                        }
                    }
                }
                transaction.Commit();
            }
        }

        private void ImportCcarOwnerFile(string filePath)
        {
            var conn = _Database.GetCcarConnection();
            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT INTO ccar_owner
                        (mark, owner_name, street, city, province_state, postal_code, country, owner_type)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)";

                    for(int i = 0; i <= 7; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    // Field indices from carsownr.txt: 0=mark,1=name,3=street,5=city,6=province,8=postal,9=country,11=owner_type
                    var fieldMap = new[] { 0, 1, 3, 5, 6, 8, 9, 11 };

                    int count = 0;
                    foreach(var line in File.ReadLines(filePath, System.Text.Encoding.GetEncoding(1252))) {
                        var fields = ParseCsvLine(line);
                        if(fields.Length < 12) continue;

                        for(int i = 0; i < fieldMap.Length; i++) {
                            cmd.Parameters["@p" + i].Value = fields[fieldMap[i]].Trim();
                        }

                        cmd.ExecuteNonQuery();
                        count++;

                        if(count % 5000 == 0) {
                            _Plugin.StatusDescription = $"Importing CCAR owners... {count:N0}";
                        }
                    }
                }
                transaction.Commit();
            }
        }

        private void DownloadFile(string url, string destPath)
        {
            DownloadFile(url, destPath, "VirtualRadarServer-RegistrationData-Plugin/1.0");
        }

        private void DownloadFile(string url, string destPath, string userAgent)
        {
            using(var client = new WebClient()) {
                client.Headers.Add("User-Agent", userAgent);
                client.DownloadFile(url, destPath);
            }
        }

        private string FindFile(string directory, string fileName)
        {
            var files = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories);
            return files.Length > 0 ? files[0] : null;
        }

        // Maps FAA MASTER.txt header names to our DB column names.
        // The FAA file has "OTHER NAMES" columns (co-owners) between
        // AIR WORTH DATE and EXPIRATION DATE that must be skipped.
        private static readonly string[][] MasterColumnMap = new[] {
            new[] { "N-NUMBER",           "n_number" },
            new[] { "SERIAL NUMBER",      "serial_number" },
            new[] { "MFR MDL CODE",       "mfr_mdl_code" },
            new[] { "ENG MFR MDL",        "eng_mfr_mdl" },
            new[] { "YEAR MFR",           "year_mfr" },
            new[] { "TYPE REGISTRANT",    "type_registrant" },
            new[] { "NAME",               "name" },
            new[] { "STREET",             "street" },
            new[] { "STREET2",            "street2" },
            new[] { "CITY",               "city" },
            new[] { "STATE",              "state" },
            new[] { "ZIP CODE",           "zip_code" },
            new[] { "REGION",             "region" },
            new[] { "COUNTY",             "county" },
            new[] { "COUNTRY",            "country" },
            new[] { "LAST ACTION DATE",   "last_action_date" },
            new[] { "CERT ISSUE DATE",    "cert_issue_date" },
            new[] { "CERTIFICATION",      "certification" },
            new[] { "TYPE AIRCRAFT",      "type_aircraft" },
            new[] { "TYPE ENGINE",        "type_engine" },
            new[] { "STATUS CODE",        "status_code" },
            new[] { "MODE S CODE",        "mode_s_code" },
            new[] { "FRACT OWNER",        "fract_owner" },
            new[] { "AIR WORTH DATE",     "air_worth_date" },
            new[] { "EXPIRATION DATE",    "expiration_date" },
            new[] { "UNIQUE ID",          "unique_id" },
            new[] { "KIT MFR",            "kit_mfr" },
            new[] { "KIT MODEL",          "kit_model" },
        };

        private void ImportMasterFile(string filePath, Action<double> progressCallback)
        {
            long totalLines = CountLines(filePath);
            var conn = _Database.GetAircraftConnection();
            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO aircraft_registration
                        (n_number, serial_number, mfr_mdl_code, eng_mfr_mdl, year_mfr,
                         type_registrant, name, street, street2, city, state, zip_code,
                         region, county, country, last_action_date, cert_issue_date,
                         certification, type_aircraft, type_engine,
                         status_code, mode_s_code, fract_owner, air_worth_date,
                         expiration_date, unique_id, kit_mfr, kit_model)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,
                                @p12,@p13,@p14,@p15,@p16,@p17,@p18,@p19,@p20,@p21,
                                @p22,@p23,@p24,@p25,@p26,@p27)";

                    for(int i = 0; i <= 27; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    // Parse header to build index map: faaColumnIndex -> dbParamIndex
                    int[] headerMap = null;
                    int count = 0;
                    foreach(var line in File.ReadLines(filePath)) {
                        if(count == 0) {
                            // Build mapping from FAA header
                            var headers = ParseCsvLine(line);
                            headerMap = BuildHeaderMap(headers, MasterColumnMap);
                            count++;
                            continue;
                        }

                        var fields = ParseCsvLine(line);

                        // Set all params to empty first
                        for(int i = 0; i <= 27; i++) {
                            cmd.Parameters["@p" + i].Value = "";
                        }

                        // Map fields using header positions
                        if(headerMap != null) {
                            for(int fi = 0; fi < fields.Length && fi < headerMap.Length; fi++) {
                                if(headerMap[fi] >= 0) {
                                    cmd.Parameters["@p" + headerMap[fi]].Value = fields[fi].Trim();
                                }
                            }
                        } else {
                            // Fallback: positional (shouldn't happen if header exists)
                            for(int i = 0; i <= 27 && i < fields.Length; i++) {
                                cmd.Parameters["@p" + i].Value = fields[i].Trim();
                            }
                        }

                        cmd.ExecuteNonQuery();
                        count++;

                        if(count % 10000 == 0) {
                            double pct = totalLines > 0 ? (double)count / totalLines : 0;
                            progressCallback(pct);
                            _AircraftPhase = $"Importing registrations... {count:N0}";
                            _Plugin.StatusDescription = $"Importing aircraft registrations... {count:N0} records";
                        }
                    }
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// Builds an array indexed by FAA CSV column position.
        /// Each element is the DB parameter index (0-27) if the column is wanted, or -1 to skip.
        /// </summary>
        private static int[] BuildHeaderMap(string[] headers, string[][] columnMap)
        {
            // Build lookup: FAA header name -> DB param index
            var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for(int i = 0; i < columnMap.Length; i++) {
                lookup[columnMap[i][0]] = i;
            }

            var map = new int[headers.Length];
            for(int i = 0; i < headers.Length; i++) {
                var h = headers[i].Trim();
                int paramIdx;
                map[i] = lookup.TryGetValue(h, out paramIdx) ? paramIdx : -1;
            }
            return map;
        }

        private void ImportAcftRefFile(string filePath)
        {
            var conn = _Database.GetAircraftConnection();
            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO aircraft_reference
                        (code, mfr, model, type_acft, type_eng, ac_cat, build_cert_ind,
                         no_eng, no_seats, ac_weight, speed)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)";

                    for(int i = 0; i <= 10; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    int count = 0;
                    foreach(var line in File.ReadLines(filePath)) {
                        if(count == 0) { count++; continue; }

                        var fields = ParseCsvLine(line);
                        if(fields.Length < 11) continue;

                        for(int i = 0; i <= 10 && i < fields.Length; i++) {
                            cmd.Parameters["@p" + i].Value = fields[i].Trim();
                        }

                        cmd.ExecuteNonQuery();
                        count++;
                    }
                }
                transaction.Commit();
            }
        }

        private void ImportEngineFile(string filePath)
        {
            var conn = _Database.GetAircraftConnection();
            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO engine_reference
                        (code, mfr, model, type, horsepower, thrust)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5)";

                    for(int i = 0; i <= 5; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    int count = 0;
                    foreach(var line in File.ReadLines(filePath)) {
                        if(count == 0) { count++; continue; }

                        var fields = ParseCsvLine(line);
                        if(fields.Length < 6) continue;

                        for(int i = 0; i <= 5 && i < fields.Length; i++) {
                            cmd.Parameters["@p" + i].Value = fields[i].Trim();
                        }

                        cmd.ExecuteNonQuery();
                        count++;
                    }
                }
                transaction.Commit();
            }
        }

        private void ImportAirmenFiles(string tempDir, Action<double> progressCallback)
        {
            var csvFiles = Directory.GetFiles(tempDir, "*.csv", SearchOption.AllDirectories);
            if(csvFiles.Length == 0) {
                // Try .txt files as an alternative
                csvFiles = Directory.GetFiles(tempDir, "*.txt", SearchOption.AllDirectories);
            }

            var conn = _Database.GetAirmenConnection();
            int totalCount = 0;

            // The airmen ZIP contains PILOT_BASIC/PILOT_CERT and NONPILOT_BASIC/NONPILOT_CERT
            // We want both pilot and non-pilot records
            var basicFiles = csvFiles.Where(f => {
                var name = Path.GetFileName(f);
                return name.IndexOf("PILOT_BASIC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       name.IndexOf("basic", StringComparison.OrdinalIgnoreCase) >= 0;
            }).OrderBy(f => Path.GetFileName(f)).ToArray();

            var certFiles = csvFiles.Where(f => {
                var name = Path.GetFileName(f);
                return name.IndexOf("PILOT_CERT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       name.IndexOf("cert", StringComparison.OrdinalIgnoreCase) >= 0;
            }).OrderBy(f => Path.GetFileName(f)).ToArray();

            long totalLines = 0;
            foreach(var f in basicFiles) totalLines += CountLines(f);
            foreach(var f in certFiles) totalLines += CountLines(f);
            long processedLines = 0;

            // Import all basic files (PILOT_BASIC + NONPILOT_BASIC)
            foreach(var basicFile in basicFiles) {
                _Plugin.StatusDescription = $"Importing {Path.GetFileName(basicFile)}...";
                using(var transaction = conn.BeginTransaction()) {
                    using(var cmd = conn.CreateCommand()) {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"INSERT OR REPLACE INTO airmen_basic
                            (unique_id, first_name, last_name, street1, street2,
                             city, state, zip_code, country, region,
                             med_class, med_date, med_exp_date)
                            VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12)";

                        for(int i = 0; i <= 12; i++) {
                            cmd.AddParam("@p" + i);
                        }

                        int count = 0;
                        foreach(var line in File.ReadLines(basicFile)) {
                            if(count == 0) { count++; continue; }

                            var fields = ParseCsvLine(line);
                            if(fields.Length < 10) continue;

                            for(int i = 0; i <= 12; i++) {
                                cmd.Parameters["@p" + i].Value = i < fields.Length ? fields[i].Trim() : "";
                            }

                            cmd.ExecuteNonQuery();
                            count++;
                            totalCount++;
                            processedLines++;

                            if(count % 10000 == 0) {
                                double pct = totalLines > 0 ? (double)processedLines / totalLines : 0;
                                progressCallback(pct);
                                _Plugin.StatusDescription = $"Importing {Path.GetFileName(basicFile)}... {count:N0}";
                            }
                        }
                    }
                    transaction.Commit();
                }
            }

            // Import all certificate files (PILOT_CERT + NONPILOT_CERT)
            foreach(var certFile in certFiles) {
                _Plugin.StatusDescription = $"Importing {Path.GetFileName(certFile)}...";
                using(var transaction = conn.BeginTransaction()) {
                    using(var cmd = conn.CreateCommand()) {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"INSERT INTO airmen_certificate
                            (unique_id, certificate_type, level, expires, ratings)
                            VALUES (@p0,@p1,@p2,@p3,@p4)";

                        for(int i = 0; i <= 4; i++) {
                            cmd.AddParam("@p" + i);
                        }

                        int count = 0;
                        foreach(var line in File.ReadLines(certFile)) {
                            if(count == 0) { count++; continue; }

                            var fields = ParseCsvLine(line);
                            if(fields.Length < 6) continue;

                            // Fields: unique_id(0), first_name(1), last_name(2), cert_type(3), level(4), expires(5), rating(6+)
                            cmd.Parameters["@p0"].Value = fields[0].Trim();
                            cmd.Parameters["@p1"].Value = fields[3].Trim();
                            cmd.Parameters["@p2"].Value = fields[4].Trim();
                            cmd.Parameters["@p3"].Value = fields[5].Trim();
                            var ratings = new List<string>();
                            for(int ri = 6; ri < fields.Length; ri++) {
                                var r = fields[ri].Trim();
                                if(!string.IsNullOrEmpty(r)) ratings.Add(r);
                            }
                            cmd.Parameters["@p4"].Value = string.Join(", ", ratings);

                            cmd.ExecuteNonQuery();
                            count++;
                            processedLines++;

                            if(count % 10000 == 0) {
                                double pct = totalLines > 0 ? (double)processedLines / totalLines : 0;
                                progressCallback(pct);
                                _Plugin.StatusDescription = $"Importing {Path.GetFileName(certFile)}... {count:N0}";
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        private static long CountLines(string filePath)
        {
            long count = 0;
            using(var reader = new StreamReader(filePath)) {
                while(reader.ReadLine() != null) count++;
            }
            return count;
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for(int i = 0; i < line.Length; i++) {
                char c = line[i];
                if(c == '"') {
                    inQuotes = !inQuotes;
                } else if(c == ',' && !inQuotes) {
                    fields.Add(current.ToString());
                    current.Clear();
                } else {
                    current.Append(c);
                }
            }
            fields.Add(current.ToString());

            return fields.ToArray();
        }

        public void DownloadSdrDatabase()
        {
            if(_IsDownloadingSdr) return;
            _IsDownloadingSdr = true;

            try {
                _SdrPhase = "Checking existing data...";
                _SdrProgress = 2;
                _Plugin.StatusDescription = "Checking FAA SDR data...";
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_SDR_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    _Database.EnsureSdrSchema();

                    // Find which years we already have
                    var existingYears = new HashSet<int>();
                    try {
                        var conn = _Database.GetSdrConnection();
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = "SELECT DISTINCT substr(difficulty_date, 7, 4) FROM sdr_report WHERE difficulty_date != ''";
                            using(var reader = cmd.ExecuteReader()) {
                                while(reader.Read()) {
                                    int yr;
                                    if(int.TryParse(reader.GetString(0), out yr) && yr >= 2000) existingYears.Add(yr);
                                }
                            }
                        }
                    } catch { }

                    var currentYear = DateTime.UtcNow.Year;
                    var startYear = currentYear - 4;
                    var csvFiles = new List<string>();
                    // Per-year failure detail so the user sees *why* a
                    // year failed (HTTP 404, timeout, etc.) instead of
                    // a generic "no files available".  Mirrors the
                    // per-year accounting in the GRT Data Analysis
                    // sdr.py implementation.
                    var yearFailures = new List<KeyValuePair<int,string>>();

                    for(int year = startYear; year <= currentYear; year++) {
                        // Skip years we already have, except current year (still being updated)
                        if(existingYears.Contains(year) && year != currentYear) {
                            _SdrPhase = $"SDR-{year}: already have data, skipping";
                            continue;
                        }

                        var pct = 5 + (int)((year - startYear) * 50.0 / (currentYear - startYear + 1));
                        _SdrPhase = $"Downloading SDR-{year}.csv...";
                        _SdrProgress = pct;
                        _Plugin.StatusDescription = $"Downloading SDR data for {year}...";

                        var csvPath = Path.Combine(tempDir, $"SDR-{year}.csv");
                        var url = SdrCsvBaseUrl + year + ".csv";
                        try {
                            // FAA's apic4e endpoint rejects the
                            // default plugin UA — switch to a browser
                            // UA, matching the working GRT script.
                            DownloadFile(url, csvPath, "Mozilla/5.0");
                            csvFiles.Add(csvPath);
                        } catch(WebException wex) {
                            string reason;
                            var http = wex.Response as HttpWebResponse;
                            if(http != null) {
                                reason = $"HTTP {(int)http.StatusCode} {http.StatusDescription}";
                            } else {
                                reason = $"{wex.Status}: {wex.Message}";
                            }
                            yearFailures.Add(new KeyValuePair<int,string>(year, reason));
                        } catch(Exception ex) {
                            yearFailures.Add(new KeyValuePair<int,string>(year, $"{ex.GetType().Name}: {ex.Message}"));
                        }
                    }

                    if(csvFiles.Count == 0 && existingYears.Count > 0 && yearFailures.Count == 0) {
                        _SdrPhase = "Up to date";
                        _SdrProgress = 100;
                        _Plugin.StatusDescription = "SDR data is up to date";
                        var opts = OptionsStorage.Load(_Plugin);
                        opts.LastSdrDownload = DateTime.UtcNow;
                        OptionsStorage.Save(_Plugin, opts);
                        _Plugin.ReloadOptions();
                        return;
                    }
                    if(csvFiles.Count == 0) {
                        // Build a detailed failure message instead of
                        // the bare "no files available" string.  If
                        // every year failed with the same reason
                        // (typical when an endpoint moves), collapse;
                        // otherwise list each year so a single-year
                        // glitch is distinguishable from a wholesale
                        // outage.
                        string detail;
                        if(yearFailures.Count > 0) {
                            var distinct = yearFailures.Select(kv => kv.Value).Distinct().ToList();
                            if(distinct.Count == 1) {
                                detail = $"all years failed: {distinct[0]}";
                            } else {
                                detail = string.Join("; ", yearFailures.Select(kv => $"{kv.Key}: {kv.Value}"));
                            }
                        } else {
                            detail = "no per-year errors recorded";
                        }
                        _SdrPhase = "Failed: " + detail;
                        _SdrProgress = 0;
                        _Plugin.StatusDescription = "SDR download failed: " + detail;
                        return;
                    }

                    _SdrPhase = "Importing SDR records...";
                    _SdrProgress = 60;
                    _Plugin.StatusDescription = "Importing SDR records...";

                    int totalRecords = 0;
                    for(int i = 0; i < csvFiles.Count; i++) {
                        _SdrPhase = $"Importing file {i + 1} of {csvFiles.Count}...";
                        _SdrProgress = 60 + (int)(i * 35.0 / csvFiles.Count);
                        totalRecords += ImportSdrCsv(csvFiles[i]);
                    }

                    _SdrProgress = 98;
                    var options = OptionsStorage.Load(_Plugin);
                    options.LastSdrDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    _SdrPhase = $"Complete: {totalRecords:N0} reports";
                    _SdrProgress = 100;
                    _Plugin.StatusDescription = $"SDR DB updated: {totalRecords:N0} reports";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _SdrPhase = "Failed: " + ex.Message;
                _SdrProgress = 0;
                _Plugin.StatusDescription = "SDR download failed: " + ex.Message;
            } finally {
                _IsDownloadingSdr = false;
            }
        }

        private int ImportSdrCsv(string filePath)
        {
            var count = 0;
            var conn = _Database.GetSdrConnection();

            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR IGNORE INTO sdr_report
                        (operator_control_number, difficulty_date, registry_n_number, aircraft_make,
                         aircraft_model, jasc_code, part_name, part_number,
                         nature_of_condition, stage_of_operation, how_discovered, discrepancy)
                        VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11)";

                    for(int i = 0; i <= 11; i++) {
                        cmd.AddParam("@p" + i);
                    }

                    // Map CSV column names to parameter indices
                    var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                        { "OperatorControlNumber", 0 },
                        { "DifficultyDate", 1 },
                        { "RegistryNNumber", 2 },
                        { "AircraftMake", 3 },
                        { "AircraftModel", 4 },
                        { "JASCCode", 5 },
                        { "PartName", 6 },
                        { "PartNumber", 7 },
                        { "NatureOfConditionA", 8 },
                        { "StageOfOperationCode", 9 },
                        { "HowDiscoveredCode", 10 },
                        { "Discrepancy", 11 },
                    };

                    bool headerRead = false;
                    int[] headerMap = null; // headerMap[paramIdx] = csvColIdx, or -1

                    foreach(var line in File.ReadLines(filePath, System.Text.Encoding.UTF8)) {
                        if(!headerRead) {
                            headerRead = true;
                            var headers = ParseCsvLine(line);
                            headerMap = new int[12];
                            for(int i = 0; i < 12; i++) headerMap[i] = -1;

                            for(int c = 0; c < headers.Length; c++) {
                                var h = headers[c].Trim();
                                int paramIdx;
                                if(colMap.TryGetValue(h, out paramIdx)) {
                                    headerMap[paramIdx] = c;
                                }
                            }
                            continue;
                        }

                        if(headerMap == null) continue;

                        var fields = ParseCsvLine(line);
                        for(int i = 0; i < 12; i++) {
                            var colIdx = headerMap[i];
                            cmd.Parameters["@p" + i].Value =
                                (colIdx >= 0 && colIdx < fields.Length) ? fields[colIdx].Trim() : "";
                        }

                        cmd.ExecuteNonQuery();
                        count++;

                        if(count % 5000 == 0) {
                            _SdrPhase = $"Importing... {count:N0} reports";
                            _Plugin.StatusDescription = $"Importing SDR reports... {count:N0}";
                        }
                    }
                }
                transaction.Commit();
            }

            return count;
        }

        public void DownloadCasaDatabase()
        {
            if(_IsDownloadingCasa) return;
            _IsDownloadingCasa = true;

            try {
                _CasaPhase = "Downloading...";
                _CasaProgress = 5;
                _Plugin.StatusDescription = "Downloading CASA aircraft register...";
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_CASA_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    var casaUrl = _CurrentOptions?.CasaDownloadUrl;
                    var downloadUrl = string.IsNullOrWhiteSpace(casaUrl) ? DefaultCasaUrl : casaUrl;
                    var zipPath = Path.Combine(tempDir, "acrftreg.zip");

                    using(var client = new WebClient()) {
                        client.Headers.Add("User-Agent", "Mozilla/5.0");
                        client.DownloadFile(downloadUrl, zipPath);
                    }

                    _CasaPhase = "Extracting...";
                    _CasaProgress = 20;
                    ZipFile.ExtractToDirectory(zipPath, tempDir);

                    // Find the CSV
                    var csvPath = Path.Combine(tempDir, "acrftreg.csv");
                    if(!File.Exists(csvPath)) {
                        var csvFiles = Directory.GetFiles(tempDir, "*.csv", SearchOption.AllDirectories);
                        if(csvFiles.Length > 0) csvPath = csvFiles[0];
                    }

                    _CasaPhase = "Importing...";
                    _CasaProgress = 30;
                    _Plugin.StatusDescription = "Importing CASA aircraft...";

                    _Database.DropCasaTables();
                    _Database.EnsureCasaSchema();

                    ImportCasaCsv(csvPath);

                    _CasaProgress = 98;
                    var options = OptionsStorage.Load(_Plugin);
                    options.LastCasaDownload = DateTime.UtcNow;
                    OptionsStorage.Save(_Plugin, options);
                    _Plugin.ReloadOptions();

                    var counts = _Database.GetRecordCounts();
                    long casaCount;
                    counts.TryGetValue("casa_aircraft", out casaCount);
                    _CasaPhase = $"Complete: {casaCount:N0} aircraft";
                    _CasaProgress = 100;
                    _Plugin.StatusDescription = $"CASA DB updated: {casaCount:N0} aircraft";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _CasaPhase = "Failed: " + ex.Message;
                _CasaProgress = 0;
                _Plugin.StatusDescription = "CASA download failed: " + ex.Message;
            } finally {
                _IsDownloadingCasa = false;
            }
        }

        private void ImportCasaCsv(string filePath)
        {
            var conn = _Database.GetCasaConnection();

            using(var transaction = conn.BeginTransaction()) {
                using(var cmd = conn.CreateCommand()) {
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"INSERT OR REPLACE INTO casa_aircraft
                        (mark, manufacturer, type, model, serial, json_data)
                        VALUES (@mark, @mfr, @type, @model, @serial, @json)";
                    cmd.AddParam("@mark");
                    cmd.AddParam("@mfr");
                    cmd.AddParam("@type");
                    cmd.AddParam("@model");
                    cmd.AddParam("@serial");
                    cmd.AddParam("@json");

                    bool headerRead = false;
                    string[] headers = null;
                    int count = 0;

                    foreach(var line in File.ReadLines(filePath, System.Text.Encoding.UTF8)) {
                        if(!headerRead) {
                            headerRead = true;
                            headers = ParseCsvLine(line);
                            for(int i = 0; i < headers.Length; i++) headers[i] = headers[i].Trim();
                            continue;
                        }
                        if(headers == null) continue;

                        var fields = ParseCsvLine(line);
                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for(int i = 0; i < Math.Min(headers.Length, fields.Length); i++) {
                            dict[headers[i]] = fields[i].Trim();
                        }

                        string mark; dict.TryGetValue("Mark", out mark);
                        if(string.IsNullOrEmpty(mark)) continue;

                        string mfr, type, model, serial;
                        dict.TryGetValue("Manu", out mfr);
                        dict.TryGetValue("Type", out type);
                        dict.TryGetValue("Model", out model);
                        dict.TryGetValue("Serial", out serial);

                        cmd.Parameters["@mark"].Value = mark;
                        cmd.Parameters["@mfr"].Value = mfr ?? "";
                        cmd.Parameters["@type"].Value = type ?? "";
                        cmd.Parameters["@model"].Value = model ?? "";
                        cmd.Parameters["@serial"].Value = serial ?? "";
                        cmd.Parameters["@json"].Value = Newtonsoft.Json.JsonConvert.SerializeObject(dict);

                        cmd.ExecuteNonQuery();
                        count++;

                        if(count % 5000 == 0) {
                            _CasaPhase = $"Importing... {count:N0} aircraft";
                            _CasaProgress = 30 + Math.Min(65, count / 500);
                            _Plugin.StatusDescription = $"Importing CASA aircraft... {count:N0}";
                        }
                    }
                }
                transaction.Commit();
            }
        }

        private volatile bool _IsDownloadingNzcaa;
        private volatile string _NzcaaPhase = "";
        private volatile int _NzcaaProgress;
        public bool IsDownloadingNzcaa { get { return _IsDownloadingNzcaa; } }
        public string NzcaaPhase { get { return _NzcaaPhase; } }
        public int NzcaaProgress { get { return _NzcaaProgress; } }

        public void ImportNzcaaDatabase()
        {
            DownloadNzcaaDatabase();
        }

        public void DownloadNzcaaDatabase()
        {
            if(_IsDownloadingNzcaa) return;
            _IsDownloadingNzcaa = true;

            try {
                _NzcaaPhase = "Downloading NZ CAA...";
                _NzcaaProgress = 5;
                _Plugin.StatusDescription = "Downloading NZ CAA aircraft register...";
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var tempDir = Path.Combine(Path.GetTempPath(), "RegistrationData_NZCAA_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                try {
                    var csvPath = Path.Combine(tempDir, "nzcaa.csv");
                    using(var client = new WebClient()) {
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        client.DownloadFile(DefaultNzcaaUrl, csvPath);
                    }

                    _NzcaaPhase = "Importing NZ CAA...";
                    _NzcaaProgress = 30;
                    _Plugin.StatusDescription = "Importing NZ CAA aircraft...";

                _Database.DropNzcaaTables();
                _Database.EnsureNzcaaSchema();

                var conn = _Database.GetNzcaaConnection();
                int count = 0;

                using(var transaction = conn.BeginTransaction()) {
                    using(var cmd = conn.CreateCommand()) {
                        cmd.Transaction = transaction;
                        cmd.CommandText = @"INSERT OR REPLACE INTO nzcaa_aircraft
                            (registration, manufacturer, model, serial, json_data)
                            VALUES (@reg, @mfr, @mdl, @ser, @json)";
                        cmd.AddParam("@reg");
                        cmd.AddParam("@mfr");
                        cmd.AddParam("@mdl");
                        cmd.AddParam("@ser");
                        cmd.AddParam("@json");

                        bool headerRead = false;
                        string[] headers = null;

                        foreach(var line in File.ReadLines(csvPath, System.Text.Encoding.UTF8)) {
                            if(!headerRead) {
                                headerRead = true;
                                headers = ParseCsvLine(line);
                                for(int i = 0; i < headers.Length; i++) headers[i] = headers[i].Trim();
                                continue;
                            }
                            if(headers == null) continue;

                            var fields = ParseCsvLine(line);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < Math.Min(headers.Length, fields.Length); i++) {
                                dict[headers[i]] = fields[i].Trim();
                            }

                            string reg = "";
                            foreach(var key in new[] { "Registration Mark", "Registration", "Mark" }) {
                                string v;
                                if(dict.TryGetValue(key, out v) && !string.IsNullOrEmpty(v)) { reg = v; break; }
                            }
                            if(string.IsNullOrEmpty(reg)) continue;

                            string mfr, mdl, ser;
                            dict.TryGetValue("Manufacturer", out mfr);
                            dict.TryGetValue("Model", out mdl);
                            dict.TryGetValue("Serial No.", out ser);

                            cmd.Parameters["@reg"].Value = reg.ToUpperInvariant();
                            cmd.Parameters["@mfr"].Value = mfr ?? "";
                            cmd.Parameters["@mdl"].Value = mdl ?? "";
                            cmd.Parameters["@ser"].Value = ser ?? "";
                            cmd.Parameters["@json"].Value = Newtonsoft.Json.JsonConvert.SerializeObject(dict);

                            cmd.ExecuteNonQuery();
                            count++;
                        }
                    }
                    transaction.Commit();
                }

                _NzcaaProgress = 95;
                var options = OptionsStorage.Load(_Plugin);
                options.LastNzcaaDownload = DateTime.UtcNow;
                OptionsStorage.Save(_Plugin, options);
                _Plugin.ReloadOptions();

                var counts = _Database.GetRecordCounts();
                long nzCount;
                counts.TryGetValue("nzcaa_aircraft", out nzCount);
                _NzcaaPhase = $"Complete: {nzCount:N0} aircraft";
                _NzcaaProgress = 100;
                _Plugin.StatusDescription = $"NZ CAA updated: {nzCount:N0} aircraft";
                } finally {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            } catch(Exception ex) {
                _NzcaaPhase = "Failed: " + ex.Message;
                _NzcaaProgress = 0;
                _Plugin.StatusDescription = "NZ CAA download failed: " + ex.Message;
            } finally {
                _IsDownloadingNzcaa = false;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
