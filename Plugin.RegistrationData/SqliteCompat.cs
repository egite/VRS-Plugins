using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace VirtualRadar.Plugin.RegistrationData
{
    // Resolves whichever SQLite ADO.NET provider VRS has loaded into the host AppDomain:
    // System.Data.SQLite on Windows VRS, Mono.Data.Sqlite on Mono / Pi VRS. The plugin
    // therefore has no static reference to either provider and ships as a single DLL
    // that runs unchanged on both targets.
    static class SqliteFactory
    {
        private static readonly Type _ConnectionType = ResolveConnectionType();

        private static Type ResolveConnectionType()
        {
            var mono = Type.GetType("Mono.Runtime") != null;
            var ordered = mono
                ? new[] { ("Mono.Data.Sqlite", "Mono.Data.Sqlite.SqliteConnection"),
                          ("System.Data.SQLite", "System.Data.SQLite.SQLiteConnection") }
                : new[] { ("System.Data.SQLite", "System.Data.SQLite.SQLiteConnection"),
                          ("Mono.Data.Sqlite", "Mono.Data.Sqlite.SqliteConnection") };

            foreach(var pair in ordered) {
                var t = TryLoad(pair.Item1, pair.Item2);
                if(t != null) return t;
            }
            throw new InvalidOperationException(
                "Neither System.Data.SQLite nor Mono.Data.Sqlite could be loaded. " +
                "VRS normally supplies one of these in the host process.");
        }

        private static Type TryLoad(string assemblyName, string typeName)
        {
            try {
                foreach(var loaded in AppDomain.CurrentDomain.GetAssemblies()) {
                    if(loaded.GetName().Name == assemblyName) {
                        var t = loaded.GetType(typeName, throwOnError: false);
                        if(t != null) return t;
                    }
                }
                var asm = Assembly.Load(assemblyName);
                return asm == null ? null : asm.GetType(typeName, throwOnError: false);
            } catch {
                return null;
            }
        }

        public static DbConnection NewConnection(string dbPath, bool readOnly = false)
        {
            // Both providers accept the same connection-string keywords for these basics.
            // Journal mode is set via PRAGMA after Open() because the typed
            // SQLiteConnectionStringBuilder.JournalMode property is provider-specific.
            var connStr = "Data Source=" + dbPath + ";Version=3";
            if(readOnly) connStr += ";Read Only=True";
            return (DbConnection)Activator.CreateInstance(_ConnectionType, connStr);
        }
    }

    static class DbExtensions
    {
        public static DbParameter AddParam(this DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
            return p;
        }

        public static DbParameter AddParam(this DbCommand cmd, string name)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            cmd.Parameters.Add(p);
            return p;
        }
    }
}
