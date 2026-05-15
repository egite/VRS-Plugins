using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VirtualRadar.Plugin.RegistrationData
{
    // Reads tables from a Microsoft Access (.mdb) file.
    // Two implementations: OleDb-backed for Windows, mdbtools-backed for Mono.
    internal interface INtsbMdbReader : IDisposable
    {
        HashSet<string> GetColumns(string tableName);
        IEnumerable<IDictionary<string, string>> ReadRows(string tableName);
    }

    internal static class NtsbMdbReader
    {
        // Throws with a helpful message if the platform's reader can't be initialized
        // (e.g. ACE/Jet missing on Windows, mdbtools not installed on Linux).
        public static INtsbMdbReader Open(string mdbPath)
        {
            if(Type.GetType("Mono.Runtime") != null) {
                MdbToolsReader.EnsureInstalled();
                return new MdbToolsReader(mdbPath);
            }
            return OleDbReader.Open(mdbPath);
        }
    }

    // Windows path -- uses Microsoft Jet/ACE via System.Data.OleDb. Same behaviour
    // as before the Mono refactor; constructed only when not running under Mono.
    internal sealed class OleDbReader : INtsbMdbReader
    {
        private readonly OleDbConnection _Conn;

        private OleDbReader(OleDbConnection conn) { _Conn = conn; }

        public static OleDbReader Open(string mdbPath)
        {
            string[] providers = {
                "Microsoft.ACE.OLEDB.16.0",
                "Microsoft.ACE.OLEDB.12.0",
                "Microsoft.Jet.OLEDB.4.0",
            };
            foreach(var provider in providers) {
                try {
                    var connStr = "Provider=" + provider + ";Data Source=" + mdbPath + ";";
                    var conn = new OleDbConnection(connStr);
                    conn.Open();
                    return new OleDbReader(conn);
                } catch {
                    // try next provider
                }
            }
            throw new Exception("Cannot open NTSB Access database (.mdb). " +
                "Install 'Microsoft Access Database Engine 2016 Redistributable' from " +
                "https://www.microsoft.com/en-us/download/details.aspx?id=54920");
        }

        public HashSet<string> GetColumns(string tableName)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try {
                var schema = _Conn.GetOleDbSchemaTable(OleDbSchemaGuid.Columns,
                    new object[] { null, null, tableName, null });
                if(schema != null) {
                    foreach(System.Data.DataRow row in schema.Rows) {
                        result.Add(row["COLUMN_NAME"].ToString());
                    }
                }
            } catch { }
            return result;
        }

        public IEnumerable<IDictionary<string, string>> ReadRows(string tableName)
        {
            using(var cmd = _Conn.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM [" + tableName + "]";
                using(var reader = cmd.ExecuteReader()) {
                    while(reader.Read()) {
                        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        for(int i = 0; i < reader.FieldCount; i++) {
                            row[reader.GetName(i)] = reader.IsDBNull(i)
                                ? "" : reader.GetValue(i).ToString().Trim();
                        }
                        yield return row;
                    }
                }
            }
        }

        public void Dispose()
        {
            try { _Conn.Close(); } catch { }
            try { _Conn.Dispose(); } catch { }
        }
    }

    // Mono / Linux path -- shells out to `mdb-export` from the mdbtools apt package.
    // Output uses ASCII Unit-Separator (US, 0x1F) and Record-Separator (RS, 0x1E)
    // as field/row delimiters, with -Q to skip quoting; these control bytes never
    // appear in NTSB's textual data so the output is unambiguous to split.
    internal sealed class MdbToolsReader : INtsbMdbReader
    {
        private const char US = '';
        private const char RS = '';

        private readonly string _MdbPath;
        private readonly Dictionary<string, string[]> _ColumnCache =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public MdbToolsReader(string mdbPath) { _MdbPath = mdbPath; }

        public static void EnsureInstalled()
        {
            try {
                using(var p = StartProcess("mdb-export", "--help")) {
                    p.WaitForExit(5000);
                    // Some mdb-export builds return non-zero on --help; that's OK as
                    // long as the binary started. The Process.Start above would have
                    // thrown if the executable was missing.
                }
            } catch(Exception ex) {
                throw new Exception(
                    "Cannot find 'mdb-export' (mdbtools). Install it on the Pi with " +
                    "'sudo apt install mdbtools' and restart VRS. Underlying error: " +
                    ex.Message, ex);
            }
        }

        public HashSet<string> GetColumns(string tableName)
        {
            var arr = LoadColumns(tableName);
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var c in arr) set.Add(c);
            return set;
        }

        private string[] LoadColumns(string tableName)
        {
            string[] cached;
            if(_ColumnCache.TryGetValue(tableName, out cached)) return cached;

            using(var p = StartMdbExport(tableName)) {
                var header = ReadOneRecord(p.StandardOutput);
                try { p.Kill(); } catch { }
                p.WaitForExit(2000);
                cached = header == null ? new string[0] : header.Split(US);
                _ColumnCache[tableName] = cached;
                return cached;
            }
        }

        public IEnumerable<IDictionary<string, string>> ReadRows(string tableName)
        {
            var columns = LoadColumns(tableName);
            using(var p = StartMdbExport(tableName)) {
                // Skip the header record we already cached
                ReadOneRecord(p.StandardOutput);

                string record;
                while((record = ReadOneRecord(p.StandardOutput)) != null) {
                    var fields = record.Split(US);
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var n = Math.Min(fields.Length, columns.Length);
                    for(int i = 0; i < n; i++) {
                        row[columns[i]] = (fields[i] ?? "").Trim();
                    }
                    yield return row;
                }
                p.WaitForExit();
            }
        }

        public void Dispose() { /* nothing to close -- subprocesses are scoped */ }

        // Read until the next RS byte (or end-of-stream). Returns null at EOF.
        private static string ReadOneRecord(StreamReader reader)
        {
            var sb = new StringBuilder();
            int ch;
            while((ch = reader.Read()) != -1) {
                if(ch == RS) return sb.ToString();
                sb.Append((char)ch);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }

        private Process StartMdbExport(string tableName)
        {
            // -d <US> -R <RS> -Q  : single-byte field/row delimiters, no quoting.
            // The Arguments string is parsed by Process into argv tokens split on
            // whitespace; control bytes pass through as single-char tokens unchanged.
            var args = "-d " + US + " -R " + RS + " -Q \"" + _MdbPath + "\" " + tableName;
            return StartProcess("mdb-export", args);
        }

        private static Process StartProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return Process.Start(psi);
        }
    }
}
