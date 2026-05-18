using System;
using System.Linq;
using System.Reflection;
using InterfaceFactory;
using VirtualRadar.Interface;

namespace VirtualRadar.Plugin.SnapToOwnship
{
    /// <summary>
    /// Looks up the loaded Stratux GPS plugin (if any) via VRS's plugin manager and pulls the
    /// auto-detected ownship ICAO out of it via reflection. Avoids a hard DLL reference so
    /// SnapToOwnship still loads when Stratux GPS isn't installed.
    /// </summary>
    internal static class StratuxBridge
    {
        private const string StratuxGpsPluginId = "VirtualRadarServer.Plugin.StratuxGPS";

        private static IPlugin _StratuxPlugin;
        private static MethodInfo _GetOwnshipIcaoMethod;

        public static string TryGetOwnshipIcao()
        {
            try {
                if(_StratuxPlugin == null || _GetOwnshipIcaoMethod == null) {
                    var manager = Factory.ResolveSingleton<IPluginManager>();
                    var plugin = manager?.LoadedPlugins?.FirstOrDefault(p => p.Id == StratuxGpsPluginId);
                    if(plugin == null) return null;
                    var method = plugin.GetType().GetMethod("GetOwnshipIcao", BindingFlags.Public | BindingFlags.Instance);
                    if(method == null) return null;
                    _StratuxPlugin = plugin;
                    _GetOwnshipIcaoMethod = method;
                }
                return _GetOwnshipIcaoMethod.Invoke(_StratuxPlugin, null) as string;
            } catch {
                return null;
            }
        }
    }
}
