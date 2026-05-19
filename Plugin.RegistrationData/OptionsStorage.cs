using System;
using System.IO;
using System.Text;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Settings;

namespace VirtualRadar.Plugin.RegistrationData
{
    static class OptionsStorage
    {
        private const string Key = "Options";
        private static readonly object _SaveLock = new object();

        public static Options Load(Plugin plugin)
        {
            var pluginStorage = Factory.ResolveSingleton<IPluginSettingsStorage>();
            var pluginSettings = pluginStorage.Load();
            var serialisedOptions = pluginSettings.ReadString(plugin, Key);

            var isFreshInstall = serialisedOptions == null;
            Options result = isFreshInstall ? new Options() : null;
            if(result == null) {
                try {
                    using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(serialisedOptions))) {
                        var serialiser = Factory.Resolve<IXmlSerialiser>();
                        result = serialiser.Deserialise<Options>(stream);
                    }
                } catch {
                    result = new Options();
                }
            }

            if(string.IsNullOrEmpty(result.DatabaseFolder)) {
                var appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VirtualRadar", "RegistrationData");
                result.DatabaseFolder = appData;
            }

            // On a Stratux host the root filesystem is typically on a tmpfs overlay
            // when persistent logging is off, so anything we download is wiped on
            // reboot. Default automatic downloads to OFF for fresh installs there;
            // the user can flip it on if they've pointed DatabaseFolder at persistent
            // storage (e.g. a USB drive). Existing installs keep their saved choice.
            if(isFreshInstall && IsStratux()) {
                result.EnableAutomaticDownloads = false;
            }

            return result;
        }

        internal static bool IsStratux()
        {
            try {
                return Directory.Exists("/opt/stratux");
            } catch {
                return false;
            }
        }

        public static void Save(Plugin plugin, Options options)
        {
            lock(_SaveLock) {
                var currentOptions = Load(plugin);
                if(options.DataVersion != currentOptions.DataVersion) {
                    // Reload and merge — another thread may have saved since we loaded
                    options.DataVersion = currentOptions.DataVersion;
                }
                ++options.DataVersion;

                using(var stream = new MemoryStream()) {
                    var serialiser = Factory.Resolve<IXmlSerialiser>();
                    serialiser.Serialise(options, stream);

                    var pluginStorage = Factory.ResolveSingleton<IPluginSettingsStorage>();
                    var pluginSettings = pluginStorage.Load();
                    pluginSettings.Write(plugin, Key, Encoding.UTF8.GetString(stream.ToArray()));
                    pluginStorage.Save(pluginSettings);
                }
            }
        }
    }
}
