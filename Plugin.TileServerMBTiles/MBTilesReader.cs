using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace VirtualRadar.Plugin.TileServerMBTiles
{
    /// <summary>
    /// Reads map tiles from a single MBTiles (SQLite) file.
    /// MBTiles spec: https://github.com/mapbox/mbtiles-spec
    /// </summary>
    class MBTilesReader : IDisposable
    {
        private readonly object _SyncLock = new object();
        private DbConnection _Connection;
        private string _FilePath;
        private bool _IsTms;
        private int _MaxZoom = -1;

        public int MaxZoom
        {
            get {
                if(_MaxZoom < 0) {
                    var meta = GetMetadata();
                    string val;
                    if(meta.TryGetValue("maxzoom", out val)) {
                        int parsed;
                        if(int.TryParse(val, out parsed)) _MaxZoom = parsed;
                    }
                    if(_MaxZoom < 0) _MaxZoom = 30; // no limit
                }
                return _MaxZoom;
            }
        }

        public MBTilesReader(string filePath, bool isTms)
        {
            _FilePath = filePath;
            _IsTms = isTms;
        }

        /// <summary>
        /// Fetches a tile from the MBTiles database.
        /// Returns null if the tile is not found.
        /// </summary>
        public byte[] GetTile(int z, int x, int y)
        {
            // Leaflet sends XYZ coordinates (y=0 at top).
            // MBTiles/TMS stores y=0 at bottom, so flip when DB is TMS.
            int tmsY = _IsTms ? ((1 << z) - 1 - y) : y;

            try {
                var conn = GetConnection();
                if(conn == null) return null;

                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level = "
                        + z + " AND tile_column = " + x + " AND tile_row = " + tmsY + " LIMIT 1";

                    var result = cmd.ExecuteScalar();
                    return result as byte[];
                }
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Gets metadata from the MBTiles file.
        /// </summary>
        public Dictionary<string, string> GetMetadata()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try {
                var conn = GetConnection();
                if(conn == null) return result;

                using(var cmd = conn.CreateCommand()) {
                    cmd.CommandText = "SELECT name, value FROM metadata";
                    using(var reader = cmd.ExecuteReader()) {
                        while(reader.Read()) {
                            var name = reader.GetString(0);
                            var value = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            result[name] = value;
                        }
                    }
                }
            } catch {
                // Metadata table may not exist or be malformed
            }

            return result;
        }

        private DbConnection GetConnection()
        {
            if(_Connection != null) return _Connection;

            lock(_SyncLock) {
                if(_Connection != null) return _Connection;

                try {
                    var conn = SqliteFactory.NewConnection(_FilePath, readOnly: true);
                    conn.Open();
                    _Connection = conn;
                } catch {
                    return null;
                }
            }

            return _Connection;
        }

        public void Dispose()
        {
            lock(_SyncLock) {
                if(_Connection != null) {
                    try { _Connection.Close(); } catch { }
                    _Connection = null;
                }
            }
        }
    }
}
