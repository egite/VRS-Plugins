using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace VirtualRadar.Plugin.RegistrationData
{
    class RegistrationDatabase : IDisposable
    {
        private readonly string _AircraftDbPath;
        private readonly string _AirmenDbPath;
        private readonly string _CcarDbPath;
        private readonly string _NtsbDbPath;
        private readonly string _SdrDbPath;
        private readonly string _CasaDbPath;
        private readonly string _NzcaaDbPath;
        private DbConnection _AircraftConnection;
        private DbConnection _AirmenConnection;
        private DbConnection _CcarConnection;
        private DbConnection _NtsbConnection;
        private DbConnection _SdrConnection;
        private DbConnection _CasaConnection;
        private DbConnection _NzcaaConnection;
        private readonly object _AircraftLock = new object();
        private readonly object _AirmenLock = new object();
        private readonly object _CcarLock = new object();
        private readonly object _NtsbLock = new object();
        private readonly object _SdrLock = new object();
        private readonly object _CasaLock = new object();
        private readonly object _NzcaaLock = new object();

        public RegistrationDatabase(string databaseFolder)
        {
            if(!Directory.Exists(databaseFolder)) {
                Directory.CreateDirectory(databaseFolder);
            }
            _AircraftDbPath = Path.Combine(databaseFolder, "faa_aircraft.db");
            _AirmenDbPath = Path.Combine(databaseFolder, "faa_airmen.db");
            _CcarDbPath = Path.Combine(databaseFolder, "ccar_aircraft.db");
            _NtsbDbPath = Path.Combine(databaseFolder, "ntsb_accidents.db");
            _SdrDbPath = Path.Combine(databaseFolder, "sdr_reports.db");
            _CasaDbPath = Path.Combine(databaseFolder, "casa_aircraft.db");
            _NzcaaDbPath = Path.Combine(databaseFolder, "nzcaa_aircraft.db");
        }

        public void EnsureAircraftSchema()
        {
            var conn = GetAircraftConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS aircraft_registration (
                        n_number            TEXT PRIMARY KEY,
                        serial_number       TEXT,
                        mfr_mdl_code        TEXT,
                        eng_mfr_mdl         TEXT,
                        year_mfr            TEXT,
                        type_registrant     TEXT,
                        name                TEXT,
                        street              TEXT,
                        street2             TEXT,
                        city                TEXT,
                        state               TEXT,
                        zip_code            TEXT,
                        region              TEXT,
                        county              TEXT,
                        country             TEXT,
                        last_action_date    TEXT,
                        cert_issue_date     TEXT,
                        certification       TEXT,
                        type_aircraft       TEXT,
                        type_engine         TEXT,
                        status_code         TEXT,
                        mode_s_code         TEXT,
                        fract_owner         TEXT,
                        air_worth_date      TEXT,
                        expiration_date     TEXT,
                        unique_id           TEXT,
                        kit_mfr             TEXT,
                        kit_model           TEXT
                    );

                    CREATE TABLE IF NOT EXISTS aircraft_reference (
                        code                TEXT PRIMARY KEY,
                        mfr                 TEXT,
                        model               TEXT,
                        type_acft           TEXT,
                        type_eng            TEXT,
                        ac_cat              TEXT,
                        build_cert_ind      TEXT,
                        no_eng              TEXT,
                        no_seats            TEXT,
                        ac_weight           TEXT,
                        speed               TEXT
                    );

                    CREATE TABLE IF NOT EXISTS engine_reference (
                        code                TEXT PRIMARY KEY,
                        mfr                 TEXT,
                        model               TEXT,
                        type                TEXT,
                        horsepower          TEXT,
                        thrust              TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                var indexes = new[] {
                    "CREATE INDEX IF NOT EXISTS idx_aircraft_mode_s ON aircraft_registration(mode_s_code)",
                    "CREATE INDEX IF NOT EXISTS idx_aircraft_name ON aircraft_registration(name)",
                };
                foreach(var sql in indexes) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void EnsureAirmenSchema()
        {
            var conn = GetAirmenConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS airmen_basic (
                        unique_id           TEXT PRIMARY KEY,
                        first_name          TEXT,
                        last_name           TEXT,
                        street1             TEXT,
                        street2             TEXT,
                        city                TEXT,
                        state               TEXT,
                        zip_code            TEXT,
                        country             TEXT,
                        region              TEXT,
                        med_class           TEXT,
                        med_date            TEXT,
                        med_exp_date        TEXT
                    );

                    CREATE TABLE IF NOT EXISTS airmen_certificate (
                        id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                        unique_id           TEXT,
                        certificate_type    TEXT,
                        level               TEXT,
                        expires             TEXT,
                        ratings             TEXT,
                        FOREIGN KEY (unique_id) REFERENCES airmen_basic(unique_id)
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                var indexes = new[] {
                    "CREATE INDEX IF NOT EXISTS idx_airmen_lastname_state ON airmen_basic(last_name, state)",
                    "CREATE INDEX IF NOT EXISTS idx_airmen_city_state ON airmen_basic(city, state)",
                    "CREATE INDEX IF NOT EXISTS idx_airmen_street_city_state ON airmen_basic(street1, city, state)",
                    "CREATE INDEX IF NOT EXISTS idx_airmen_cert_uid ON airmen_certificate(unique_id)",
                };
                foreach(var sql in indexes) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DropAircraftTables()
        {
            var conn = GetAircraftConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    DROP TABLE IF EXISTS aircraft_registration;
                    DROP TABLE IF EXISTS aircraft_reference;
                    DROP TABLE IF EXISTS engine_reference;
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public void DropAirmenTables()
        {
            var conn = GetAirmenConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    DROP TABLE IF EXISTS airmen_certificate;
                    DROP TABLE IF EXISTS airmen_basic;
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public DbConnection GetAircraftConnection()
        {
            lock(_AircraftLock) {
                if(_AircraftConnection == null) {
                    _AircraftConnection = OpenConnection(_AircraftDbPath);
                }
                return _AircraftConnection;
            }
        }

        public DbConnection GetAirmenConnection()
        {
            lock(_AirmenLock) {
                if(_AirmenConnection == null) {
                    _AirmenConnection = OpenConnection(_AirmenDbPath);
                }
                return _AirmenConnection;
            }
        }

        private static DbConnection OpenConnection(string dbPath)
        {
            var conn = SqliteFactory.NewConnection(dbPath);
            conn.Open();

            // Split into separate executes -- Mono.Data.Sqlite does not reliably
            // advance past the first statement when the first one returns a row
            // (PRAGMA journal_mode = WAL returns the new mode).
            ExecPragma(conn, "PRAGMA journal_mode = WAL");
            ExecPragma(conn, "PRAGMA synchronous = OFF");
            ExecPragma(conn, "PRAGMA temp_store = MEMORY");
            ExecPragma(conn, "PRAGMA cache_size = -65536");  // ~64 MB page cache

            return conn;
        }

        private static void ExecPragma(DbConnection conn, string sql)
        {
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = sql;
                try { cmd.ExecuteNonQuery(); } catch { }
            }
        }

        public Dictionary<string, string> GetAircraftByNNumber(string nNumber)
        {
            nNumber = (nNumber ?? "").Trim().ToUpperInvariant().TrimStart('N');
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try {
                var conn = GetAircraftConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM aircraft_registration WHERE n_number = @n LIMIT 1";
                    cmd.AddParam("@n", nNumber);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public Dictionary<string, string> GetAircraftReference(string mfrMdlCode)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if(string.IsNullOrEmpty(mfrMdlCode)) return result;

            try {
                var conn = GetAircraftConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM aircraft_reference WHERE code = @c LIMIT 1";
                    cmd.AddParam("@c", mfrMdlCode);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public Dictionary<string, string> GetEngineReference(string engMfrMdl)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if(string.IsNullOrEmpty(engMfrMdl)) return result;

            try {
                var conn = GetAircraftConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM engine_reference WHERE code = @c LIMIT 1";
                    cmd.AddParam("@c", engMfrMdl);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public List<Dictionary<string, string>> GetAirmenByLastName(string lastName)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(lastName)) return results;

            try {
                var conn = GetAirmenConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM airmen_basic WHERE last_name = @ln";
                    cmd.AddParam("@ln", lastName.ToUpperInvariant());
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            } catch { }

            return results;
        }

        public string DebugCertLookup(string uniqueId)
        {
            try {
                var conn = GetAirmenConnection();
                // Check exact match
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT COUNT(*) FROM airmen_certificate WHERE unique_id = @uid";
                    cmd.AddParam("@uid", uniqueId);
                    var exact = Convert.ToInt64(cmd.ExecuteScalar());

                    cmd.CommandText = "SELECT COUNT(*) FROM airmen_certificate WHERE unique_id LIKE @uid2";
                    cmd.AddParam("@uid2", "%" + uniqueId.Trim() + "%");
                    var like = Convert.ToInt64(cmd.ExecuteScalar());

                    cmd.CommandText = "SELECT unique_id FROM airmen_certificate LIMIT 3";
                    var samples = new List<string>();
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) samples.Add("'" + reader.GetString(0) + "'");
                    }

                    return $"exact={exact}, like={like}, queryId='{uniqueId}', samples=[{string.Join(",", samples)}]";
                }
            } catch(Exception ex) { return "error: " + ex.Message; }
        }

        public List<string> SearchAirmenByLastNameLike(string name)
        {
            var results = new List<string>();
            try {
                var conn = GetAirmenConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT first_name, last_name FROM airmen_basic WHERE last_name LIKE @ln OR first_name LIKE @fn LIMIT 10";
                    cmd.AddParam("@ln", "%" + name + "%");
                    cmd.AddParam("@fn", "%" + name + "%");
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            results.Add((reader.IsDBNull(0) ? "" : reader.GetString(0)) + " | " + (reader.IsDBNull(1) ? "" : reader.GetString(1)));
                        }
                    }
                }
            } catch { }
            return results;
        }

        public List<Dictionary<string, string>> GetAirmenByLastNameFuzzy(string lastName)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(lastName)) return results;

            var upper = lastName.ToUpperInvariant();
            var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { upper };

            // With/without trailing S
            if(upper.EndsWith("S"))
                variants.Add(upper.Substring(0, upper.Length - 1));
            else
                variants.Add(upper + "S");

            // With/without trailing ES
            if(upper.EndsWith("ES"))
                variants.Add(upper.Substring(0, upper.Length - 2));
            else
                variants.Add(upper + "ES");

            // Remove the exact match — that's already been tried
            variants.Remove(upper);

            if(variants.Count == 0) return results;

            try {
                var conn = GetAirmenConnection();
                foreach(var variant in variants) {
                    using(var cmd = conn.CreateCommand()) {
                        cmd.CommandText = "SELECT * FROM airmen_basic WHERE last_name = @ln";
                        cmd.AddParam("@ln", variant);
                        using(var reader = cmd.ExecuteReader()) {
                            while(reader.Read()) {
                                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                for(int i = 0; i < reader.FieldCount; i++) {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                                }
                                results.Add(row);
                            }
                        }
                    }
                }
            } catch { }

            return results;
        }

        /// <summary>
        /// Finds airmen who live at the same address as the aircraft registration.
        /// Queries by city+state, then filters by normalized street similarity.
        /// </summary>
        public List<Dictionary<string, string>> GetAirmenByAddress(string street, string city, string state)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(city) || string.IsNullOrEmpty(state)) return results;

            try {
                var conn = GetAirmenConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM airmen_basic WHERE city = @city AND state = @state";
                    cmd.AddParam("@city", city.Trim().ToUpperInvariant());
                    cmd.AddParam("@state", state.Trim().ToUpperInvariant());

                    var normStreet = NormalizeStreet(street);

                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }

                            // If we have a street to match, filter by normalized street
                            if(!string.IsNullOrEmpty(normStreet)) {
                                string airmanStreet;
                                row.TryGetValue("street1", out airmanStreet);
                                var normAirmanStreet = NormalizeStreet(airmanStreet);
                                if(!string.IsNullOrEmpty(normAirmanStreet) && normAirmanStreet == normStreet) {
                                    results.Add(row);
                                }
                            } else {
                                results.Add(row);
                            }
                        }
                    }
                }
            } catch { }

            return results;
        }

        /// <summary>
        /// Normalizes a street address for fuzzy comparison.
        /// Strips punctuation, collapses whitespace, and canonicalizes
        /// common abbreviations (ST/STREET, DR/DRIVE, AVE/AVENUE, etc.)
        /// </summary>
        internal static string NormalizeStreet(string street)
        {
            if(string.IsNullOrWhiteSpace(street)) return "";

            var s = street.Trim().ToUpperInvariant();

            // Remove punctuation (periods, commas, hashes, dashes)
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[.,#\-']", "");

            // Collapse multiple spaces
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();

            // Replace common abbreviations with canonical forms
            // Use word-boundary replacement to avoid partial matches
            var abbrevs = new[] {
                new[] { @"\bSTREET\b", "ST" },
                new[] { @"\bAVENUE\b", "AVE" },
                new[] { @"\bBOULEVARD\b", "BLVD" },
                new[] { @"\bDRIVE\b", "DR" },
                new[] { @"\bLANE\b", "LN" },
                new[] { @"\bCOURT\b", "CT" },
                new[] { @"\bPLACE\b", "PL" },
                new[] { @"\bCIRCLE\b", "CIR" },
                new[] { @"\bROAD\b", "RD" },
                new[] { @"\bTERRACE\b", "TER" },
                new[] { @"\bPARKWAY\b", "PKWY" },
                new[] { @"\bHIGHWAY\b", "HWY" },
                new[] { @"\bNORTH\b", "N" },
                new[] { @"\bSOUTH\b", "S" },
                new[] { @"\bEAST\b", "E" },
                new[] { @"\bWEST\b", "W" },
                new[] { @"\bNORTHEAST\b", "NE" },
                new[] { @"\bNORTHWEST\b", "NW" },
                new[] { @"\bSOUTHEAST\b", "SE" },
                new[] { @"\bSOUTHWEST\b", "SW" },
                new[] { @"\bAPARTMENT\b", "APT" },
                new[] { @"\bSUITE\b", "STE" },
                new[] { @"\bBUILDING\b", "BLDG" },
            };

            foreach(var pair in abbrevs) {
                s = System.Text.RegularExpressions.Regex.Replace(s, pair[0], pair[1]);
            }

            return s;
        }

        public string LastCertError { get; private set; }

        public List<Dictionary<string, string>> GetCertificatesForAirman(string uniqueId)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(uniqueId)) return results;

            try {
                var conn = GetAirmenConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT certificate_type, level, expires, ratings FROM airmen_certificate WHERE unique_id = @uid";
                    cmd.AddParam("@uid", uniqueId);
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                            results.Add(row);
                        }
                    }
                }
            } catch(Exception ex) {
                LastCertError = ex.ToString();
            }

            return results;
        }

        // CCAR (Canadian) methods
        public DbConnection GetCcarConnection()
        {
            lock(_CcarLock) {
                if(_CcarConnection == null) {
                    _CcarConnection = OpenConnection(_CcarDbPath);
                }
                return _CcarConnection;
            }
        }

        public void EnsureCcarSchema()
        {
            var conn = GetCcarConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ccar_aircraft (
                        mark                TEXT PRIMARY KEY,
                        common_name         TEXT,
                        model_name          TEXT,
                        serial_number       TEXT,
                        manufacturer_name   TEXT,
                        aircraft_category   TEXT,
                        engine_category     TEXT,
                        number_of_engines   TEXT,
                        number_of_seats     TEXT,
                        air_weight_kg       TEXT,
                        issue_date          TEXT,
                        effective_date      TEXT,
                        ineffective_date    TEXT,
                        registered_purpose  TEXT,
                        flight_authority    TEXT,
                        province_state      TEXT,
                        city                TEXT,
                        registration_status TEXT
                    );

                    CREATE TABLE IF NOT EXISTS ccar_owner (
                        id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                        mark                TEXT,
                        owner_name          TEXT,
                        street              TEXT,
                        city                TEXT,
                        province_state      TEXT,
                        postal_code         TEXT,
                        country             TEXT,
                        owner_type          TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_ccar_owner_mark ON ccar_owner(mark)";
                cmd.ExecuteNonQuery();
            }
        }

        public void DropCcarTables()
        {
            var conn = GetCcarConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    DROP TABLE IF EXISTS ccar_owner;
                    DROP TABLE IF EXISTS ccar_aircraft;
                ";
                cmd.ExecuteNonQuery();
            }
        }

        public Dictionary<string, string> GetCcarAircraftByMark(string mark)
        {
            mark = (mark ?? "").Trim().ToUpperInvariant();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try {
                var conn = GetCcarConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM ccar_aircraft WHERE mark = @m LIMIT 1";
                    cmd.AddParam("@m", mark);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public List<Dictionary<string, string>> GetCcarOwnersByMark(string mark)
        {
            mark = (mark ?? "").Trim().ToUpperInvariant();
            var results = new List<Dictionary<string, string>>();

            try {
                var conn = GetCcarConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT mark, owner_name, street, city, province_state, postal_code, country, owner_type FROM ccar_owner WHERE mark = @m";
                    cmd.AddParam("@m", mark);
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                            }
                            results.Add(row);
                        }
                    }
                }
            } catch { }

            return results;
        }

        /// <summary>
        /// Queries the VRS AircraftOnlineLookupCache.sqb for operator info by ICAO hex code.
        /// Returns Operator name and OperatorIcao, or empty dict if not found.
        /// </summary>
        public Dictionary<string, string> GetOperatorFromVrsCache(string icaoHex)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if(string.IsNullOrEmpty(icaoHex)) return result;

            try {
                var vrsCachePath = FindVrsCachePath();
                if(string.IsNullOrEmpty(vrsCachePath) || !File.Exists(vrsCachePath)) return result;

                using(var conn = SqliteFactory.NewConnection(vrsCachePath, readOnly: true)) {
                    conn.Open();
                    using(var cmd = conn.CreateCommand()) {
                        cmd.CommandText = "SELECT Operator, OperatorIcao, ModelIcao FROM AircraftDetail WHERE Icao = @icao LIMIT 1";
                        cmd.AddParam("@icao", icaoHex.ToUpperInvariant());
                        using(var reader = cmd.ExecuteReader()) {
                            if(reader.Read()) {
                                result["operator"] = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                result["operator_icao"] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                result["model_icao"] = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        private string _VrsCachePathCached;
        private string FindVrsCachePath()
        {
            if(_VrsCachePathCached != null) return _VrsCachePathCached;

            // Look in common VRS data locations
            var candidates = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VirtualRadar", "AircraftOnlineLookupCache.sqb"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VirtualRadar", "AircraftOnlineLookupCache.sqb"),
            };

            foreach(var path in candidates) {
                if(File.Exists(path)) {
                    _VrsCachePathCached = path;
                    return path;
                }
            }

            _VrsCachePathCached = "";
            return "";
        }

        public Dictionary<string, long> GetRecordCounts()
        {
            var counts = new Dictionary<string, long>();

            // Aircraft DB tables
            try {
                var conn = GetAircraftConnection();
                foreach(var table in new[] { "aircraft_registration", "aircraft_reference", "engine_reference" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // CCAR DB tables
            try {
                var conn = GetCcarConnection();
                foreach(var table in new[] { "ccar_aircraft", "ccar_owner" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // Airmen DB tables
            try {
                var conn = GetAirmenConnection();
                foreach(var table in new[] { "airmen_basic", "airmen_certificate" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // NTSB DB tables
            try {
                var conn = GetNtsbConnection();
                foreach(var table in new[] { "ntsb_event" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // SDR DB tables
            try {
                var conn = GetSdrConnection();
                foreach(var table in new[] { "sdr_report" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // NZ CAA DB tables
            try {
                var conn = GetNzcaaConnection();
                foreach(var table in new[] { "nzcaa_aircraft" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            // CASA DB tables
            try {
                var conn = GetCasaConnection();
                foreach(var table in new[] { "casa_aircraft" }) {
                    try {
                        using(var cmd = conn.CreateCommand()) {
                            cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                            counts[table] = (long)cmd.ExecuteScalar();
                        }
                    } catch {
                        counts[table] = 0;
                    }
                }
            } catch { }

            return counts;
        }

        // NTSB methods
        public DbConnection GetNtsbConnection()
        {
            lock(_NtsbLock) {
                if(_NtsbConnection == null) {
                    _NtsbConnection = OpenConnection(_NtsbDbPath);
                }
                return _NtsbConnection;
            }
        }

        public void EnsureNtsbSchema()
        {
            var conn = GetNtsbConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ntsb_event (
                        id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                        ev_id               TEXT,
                        ntsb_no             TEXT,
                        ev_date             TEXT,
                        ev_city             TEXT,
                        ev_state            TEXT,
                        ev_country          TEXT,
                        ev_type             TEXT,
                        ev_highest_injury   TEXT,
                        inj_tot_f           INTEGER,
                        inj_tot_s           INTEGER,
                        inj_tot_m           INTEGER,
                        inj_tot_n           INTEGER,
                        latitude            TEXT,
                        longitude           TEXT,
                        regis_no            TEXT,
                        acft_make           TEXT,
                        acft_model          TEXT,
                        acft_series         TEXT,
                        acft_category       TEXT,
                        damage              TEXT,
                        far_part            TEXT,
                        oper_name           TEXT,
                        phase_flt_spec      TEXT,
                        narr_cause          TEXT,
                        apt_name            TEXT,
                        light_cond          TEXT,
                        sky_cond_nonceil    TEXT,
                        wind_vel_kts        TEXT,
                        wx_brief_comp       TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                var indexes = new[] {
                    "CREATE INDEX IF NOT EXISTS idx_ntsb_regis ON ntsb_event(regis_no)",
                    "CREATE INDEX IF NOT EXISTS idx_ntsb_ntsb_no ON ntsb_event(ntsb_no)",
                };
                foreach(var sql in indexes) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DropNtsbTables()
        {
            var conn = GetNtsbConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "DROP TABLE IF EXISTS ntsb_event;";
                cmd.ExecuteNonQuery();
            }
        }

        public List<Dictionary<string, string>> GetNtsbByRegistration(string registration)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(registration)) return results;

            // Normalize: trim, uppercase — NTSB data stores full registration including N prefix
            var reg = registration.Trim().ToUpperInvariant();

            try {
                var conn = GetNtsbConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM ntsb_event WHERE regis_no = @reg OR regis_no = @regN ORDER BY ev_date DESC";
                    cmd.AddParam("@reg", reg);
                    cmd.AddParam("@regN", reg.StartsWith("N") ? reg.Substring(1) : "N" + reg);
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                            }
                            results.Add(row);
                        }
                    }
                }
            } catch { }

            return results;
        }

        public bool HasNtsbEvents(string registration)
        {
            if(string.IsNullOrEmpty(registration)) return false;
            var reg = registration.Trim().ToUpperInvariant();
            try {
                var conn = GetNtsbConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT 1 FROM ntsb_event WHERE regis_no = @reg OR regis_no = @regN LIMIT 1";
                    cmd.AddParam("@reg", reg);
                    cmd.AddParam("@regN", reg.StartsWith("N") ? reg.Substring(1) : "N" + reg);
                    return cmd.ExecuteScalar() != null;
                }
            } catch { return false; }
        }

        // SDR methods
        public DbConnection GetSdrConnection()
        {
            lock(_SdrLock) {
                if(_SdrConnection == null) {
                    _SdrConnection = OpenConnection(_SdrDbPath);
                }
                return _SdrConnection;
            }
        }

        public void EnsureSdrSchema()
        {
            var conn = GetSdrConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS sdr_report (
                        id                      INTEGER PRIMARY KEY AUTOINCREMENT,
                        operator_control_number  TEXT UNIQUE,
                        difficulty_date          TEXT,
                        registry_n_number        TEXT,
                        aircraft_make            TEXT,
                        aircraft_model           TEXT,
                        jasc_code                TEXT,
                        part_name                TEXT,
                        part_number              TEXT,
                        nature_of_condition      TEXT,
                        stage_of_operation       TEXT,
                        how_discovered           TEXT,
                        discrepancy              TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_sdr_regis ON sdr_report(registry_n_number)";
                cmd.ExecuteNonQuery();
            }
        }

        public void DropSdrTables()
        {
            var conn = GetSdrConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "DROP TABLE IF EXISTS sdr_report;";
                cmd.ExecuteNonQuery();
            }
        }

        public List<Dictionary<string, string>> GetSdrByRegistration(string registration)
        {
            var results = new List<Dictionary<string, string>>();
            if(string.IsNullOrEmpty(registration)) return results;

            var reg = registration.Trim().ToUpperInvariant();

            try {
                var conn = GetSdrConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM sdr_report WHERE registry_n_number = @reg OR registry_n_number = @regN ORDER BY difficulty_date DESC";
                    cmd.AddParam("@reg", reg);
                    cmd.AddParam("@regN", reg.StartsWith("N") ? reg.Substring(1) : "N" + reg);
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for(int i = 0; i < reader.FieldCount; i++) {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                            }
                            results.Add(row);
                        }
                    }
                }
            } catch { }

            return results;
        }

        public Dictionary<string, string> GetVrsCacheByRegistration(string registration)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if(string.IsNullOrEmpty(registration)) return result;

            try {
                var vrsCachePath = FindVrsCachePath();
                if(string.IsNullOrEmpty(vrsCachePath) || !File.Exists(vrsCachePath)) return result;

                using(var conn = SqliteFactory.NewConnection(vrsCachePath, readOnly: true)) {
                    conn.Open();
                    using(var cmd = conn.CreateCommand()) {
                        cmd.CommandText = "SELECT Icao, Registration, Country, Manufacturer, Model, ModelIcao, Operator, OperatorIcao, Serial, YearBuilt FROM AircraftDetail WHERE Registration = @reg LIMIT 1";
                        cmd.AddParam("@reg", registration.Trim());
                        using(var reader = cmd.ExecuteReader()) {
                            if(reader.Read()) {
                                for(int i = 0; i < reader.FieldCount; i++) {
                                    result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
                                }
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public bool HasSdrReports(string registration)
        {
            if(string.IsNullOrEmpty(registration)) return false;
            var reg = registration.Trim().ToUpperInvariant();
            try {
                var conn = GetSdrConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT 1 FROM sdr_report WHERE registry_n_number = @reg OR registry_n_number = @regN LIMIT 1";
                    cmd.AddParam("@reg", reg);
                    cmd.AddParam("@regN", reg.StartsWith("N") ? reg.Substring(1) : "N" + reg);
                    return cmd.ExecuteScalar() != null;
                }
            } catch { return false; }
        }

        // CASA (Australian) methods
        public DbConnection GetCasaConnection()
        {
            lock(_CasaLock) {
                if(_CasaConnection == null) {
                    _CasaConnection = OpenConnection(_CasaDbPath);
                }
                return _CasaConnection;
            }
        }

        public void EnsureCasaSchema()
        {
            var conn = GetCasaConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS casa_aircraft (
                        mark                TEXT PRIMARY KEY,
                        manufacturer        TEXT,
                        type                TEXT,
                        model               TEXT,
                        serial              TEXT,
                        json_data           TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_casa_mark ON casa_aircraft(mark)";
                cmd.ExecuteNonQuery();
            }
        }

        public void DropCasaTables()
        {
            var conn = GetCasaConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "DROP TABLE IF EXISTS casa_aircraft;";
                cmd.ExecuteNonQuery();
            }
        }

        // NZ CAA methods
        public DbConnection GetNzcaaConnection()
        {
            lock(_NzcaaLock) {
                if(_NzcaaConnection == null) {
                    _NzcaaConnection = OpenConnection(_NzcaaDbPath);
                }
                return _NzcaaConnection;
            }
        }

        public void EnsureNzcaaSchema()
        {
            var conn = GetNzcaaConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS nzcaa_aircraft (
                        registration        TEXT PRIMARY KEY,
                        manufacturer        TEXT,
                        model               TEXT,
                        serial              TEXT,
                        json_data           TEXT
                    );
                ";
                cmd.ExecuteNonQuery();
            }
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_nzcaa_reg ON nzcaa_aircraft(registration)";
                cmd.ExecuteNonQuery();
            }
        }

        public void DropNzcaaTables()
        {
            var conn = GetNzcaaConnection();
            using(var cmd = conn.CreateCommand()) {
                cmd.CommandText = "DROP TABLE IF EXISTS nzcaa_aircraft;";
                cmd.ExecuteNonQuery();
            }
        }

        public Dictionary<string, string> GetNzcaaAircraft(string registration)
        {
            registration = (registration ?? "").Trim().ToUpperInvariant();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try {
                var conn = GetNzcaaConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM nzcaa_aircraft WHERE registration = @r LIMIT 1";
                    cmd.AddParam("@r", registration);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }
            return result;
        }

        public Dictionary<string, string> GetCasaAircraftByMark(string mark)
        {
            mark = (mark ?? "").Trim().ToUpperInvariant();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try {
                var conn = GetCasaConnection();
                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT * FROM casa_aircraft WHERE mark = @m LIMIT 1";
                    cmd.AddParam("@m", mark);
                    using(var reader = cmd.ExecuteReader()) {
                        if(reader.Read()) {
                            for(int i = 0; i < reader.FieldCount; i++) {
                                result[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetString(i);
                            }
                        }
                    }
                }
            } catch { }

            return result;
        }

        public void Dispose()
        {
            lock(_AircraftLock) {
                if(_AircraftConnection != null) {
                    try { _AircraftConnection.Close(); } catch { }
                    _AircraftConnection = null;
                }
            }
            lock(_AirmenLock) {
                if(_AirmenConnection != null) {
                    try { _AirmenConnection.Close(); } catch { }
                    _AirmenConnection = null;
                }
            }
            lock(_CcarLock) {
                if(_CcarConnection != null) {
                    try { _CcarConnection.Close(); } catch { }
                    _CcarConnection = null;
                }
            }
            lock(_NtsbLock) {
                if(_NtsbConnection != null) {
                    try { _NtsbConnection.Close(); } catch { }
                    _NtsbConnection = null;
                }
            }
            lock(_SdrLock) {
                if(_SdrConnection != null) {
                    try { _SdrConnection.Close(); } catch { }
                    _SdrConnection = null;
                }
            }
            lock(_CasaLock) {
                if(_CasaConnection != null) {
                    try { _CasaConnection.Close(); } catch { }
                    _CasaConnection = null;
                }
            }
            lock(_NzcaaLock) {
                if(_NzcaaConnection != null) {
                    try { _NzcaaConnection.Close(); } catch { }
                    _NzcaaConnection = null;
                }
            }
        }
    }
}
