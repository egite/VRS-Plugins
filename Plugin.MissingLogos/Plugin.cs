using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InterfaceFactory;
using VirtualRadar.Interface;
using VirtualRadar.Interface.Listener;
using VirtualRadar.Interface.Settings;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.MissingLogos
{
    public class Plugin : IPlugin
    {
        private Options _Options;
        private HashSet<string> _LoggedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _LoggedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private object _SyncLock = new object();
        private int _MissingCount;
        private int _MissingModelCount;
        private IHeartbeatService _HeartbeatService;

        public static Plugin Singleton { get; private set; }

        public string Id { get { return "VirtualRadarServer.Plugin.MissingLogos"; } }
        public string PluginFolder { get; set; }
        public string Name { get { return "Missing Logos"; } }
        public string Version { get { return "3.0.0"; } }

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

        public bool HasOptions { get { return true; } }

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
            var options = OptionsStorage.Load(this);

            _HeartbeatService = Factory.ResolveSingleton<IHeartbeatService>();
            _HeartbeatService.SlowTick += HeartbeatService_SlowTick;

            ApplyOptions(options);
        }

        public void GuiThreadStartup()
        {
            try {
                var webAdminViewManager = Factory.ResolveSingleton<IWebAdminViewManager>();
                webAdminViewManager.AddWebAdminView(new WebAdminView(
                    "/WebAdmin/",
                    "MissingLogosPluginOptions.html",
                    "Missing Logos",
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
            if(_HeartbeatService != null) {
                _HeartbeatService.SlowTick -= HeartbeatService_SlowTick;
            }
        }

        public void ShowWinFormsOptionsUI()
        {
            using(var dialog = new WinForms.OptionsView()) {
                dialog.Options = OptionsStorage.Load(this);

                if(dialog.ShowDialog() == DialogResult.OK) {
                    OptionsStorage.Save(this, dialog.Options);
                    ApplyOptions(dialog.Options);
                }
            }
        }

        private void ApplyOptions(Options options)
        {
            _Options = options;

            if(options.Enabled) {
                LoadExistingLogEntries(GetLogFilePath());
            }
            if(options.TrackMissingModels) {
                LoadExistingModelLogEntries(GetModelLogFilePath());
            }

            UpdateStatus();
        }

        private string GetLogFilePath()
        {
            var options = _Options;
            if(options != null && !String.IsNullOrEmpty(options.LogFileName)) {
                return options.LogFileName;
            }
            return Path.Combine(PluginFolder, "MissingLogos.log");
        }

        private string GetModelLogFilePath()
        {
            var options = _Options;
            if(options != null && !String.IsNullOrEmpty(options.ModelLogFileName)) {
                return options.ModelLogFileName;
            }
            return Path.Combine(PluginFolder, "MissingModels.log");
        }

        private void LoadExistingLogEntries(string logFilePath)
        {
            lock(_SyncLock) {
                _LoggedCodes.Clear();
                _MissingCount = 0;

                try {
                    if(File.Exists(logFilePath)) {
                        var lines = File.ReadAllLines(logFilePath);
                        foreach(var line in lines) {
                            var parts = line.Split('|');
                            if(parts.Length >= 2) {
                                var code = parts[1].Trim();
                                if(!String.IsNullOrEmpty(code)) {
                                    _LoggedCodes.Add(code);
                                }
                            }
                        }
                        _MissingCount = _LoggedCodes.Count;
                    }
                } catch {
                    // If we can't read the file, start fresh
                }
            }
        }

        private void LoadExistingModelLogEntries(string logFilePath)
        {
            lock(_SyncLock) {
                _LoggedModels.Clear();
                _MissingModelCount = 0;

                try {
                    if(File.Exists(logFilePath)) {
                        var lines = File.ReadAllLines(logFilePath);
                        foreach(var line in lines) {
                            // Format: timestamp | mfr | mdl
                            var parts = line.Split('|');
                            if(parts.Length >= 3) {
                                var key = parts[1].Trim() + " | " + parts[2].Trim();
                                _LoggedModels.Add(key);
                            }
                        }
                        _MissingModelCount = _LoggedModels.Count;
                    }
                } catch {
                }
            }
        }

        private void UpdateStatus()
        {
            var options = _Options;
            if(options == null || !options.Enabled) {
                Status = "Disabled";
                StatusDescription = null;
            } else {
                var config = Factory.ResolveSingleton<ISharedConfiguration>().Get();
                var flagsFolder = config.BaseStationSettings.OperatorFlagsFolder;

                if(String.IsNullOrEmpty(flagsFolder) || !Directory.Exists(flagsFolder)) {
                    Status = "Enabled \u2014 operator flags folder not configured";
                    StatusDescription = null;
                } else {
                    lock(_SyncLock) {
                        var status = String.Format("Enabled \u2014 {0} missing logo(s)", _MissingCount);
                        if(_Options.TrackMissingModels) {
                            status += String.Format(", {0} missing model(s)", _MissingModelCount);
                        }
                        Status = status;
                    }
                    StatusDescription = null;
                }
            }
        }

        private void HeartbeatService_SlowTick(object sender, EventArgs args)
        {
            var options = _Options;
            if(options == null || !options.Enabled) {
                return;
            }

            try {
                var config = Factory.ResolveSingleton<ISharedConfiguration>().Get();
                var flagsFolder = config.BaseStationSettings.OperatorFlagsFolder;
                var hasFlagsFolder = !String.IsNullOrEmpty(flagsFolder) && Directory.Exists(flagsFolder);

                if(!hasFlagsFolder && !options.TrackMissingModels) {
                    return;
                }

                var logFilePath = GetLogFilePath();
                var newEntries = new List<string>();
                var statusChanged = false;

                var feedManager = Factory.ResolveSingleton<IFeedManager>();
                if(hasFlagsFolder) {
                    foreach(var feed in feedManager.Feeds) {
                        if(feed.AircraftList == null) continue;

                        long unused1, unused2;
                        var aircraft = feed.AircraftList.TakeSnapshot(out unused1, out unused2);
                        foreach(var ac in aircraft) {
                            var operatorIcao = ac.OperatorIcao;
                            if(String.IsNullOrEmpty(operatorIcao)) continue;

                            lock(_SyncLock) {
                                if(_LoggedCodes.Contains(operatorIcao)) continue;

                                var logoPath = Path.Combine(flagsFolder, operatorIcao + ".bmp");
                                if(!File.Exists(logoPath)) {
                                    _LoggedCodes.Add(operatorIcao);
                                    _MissingCount++;
                                    newEntries.Add(String.Format("{0} | {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), operatorIcao));
                                } else {
                                    // Logo exists, remember so we don't check again
                                    _LoggedCodes.Add(operatorIcao);
                                }
                            }
                        }
                    }
                }

                if(newEntries.Count > 0) {
                    var logDir = Path.GetDirectoryName(logFilePath);
                    if(!String.IsNullOrEmpty(logDir) && !Directory.Exists(logDir)) {
                        Directory.CreateDirectory(logDir);
                    }
                    File.AppendAllText(logFilePath, String.Join(Environment.NewLine, newEntries) + Environment.NewLine);
                    statusChanged = true;
                }

                // Track aircraft with no ModelIcao (manufacturer + model)
                if(options.TrackMissingModels) {
                    var modelLogPath = GetModelLogFilePath();
                    var newModelEntries = new List<string>();

                    foreach(var feed in feedManager.Feeds) {
                        if(feed.AircraftList == null) continue;

                        long mu1, mu2;
                        var acList = feed.AircraftList.TakeSnapshot(out mu1, out mu2);
                        foreach(var ac in acList) {
                            if(!String.IsNullOrEmpty(ac.Type)) continue;

                            var mfr = (ac.Manufacturer ?? "").Trim();
                            var mdl = (ac.Model ?? "").Trim();
                            if(String.IsNullOrEmpty(mfr) && String.IsNullOrEmpty(mdl)) continue;

                            var key = mfr + " | " + mdl;

                            lock(_SyncLock) {
                                if(_LoggedModels.Contains(key)) continue;
                                _LoggedModels.Add(key);
                                _MissingModelCount++;
                                newModelEntries.Add(String.Format("{0} | {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), key));
                            }
                        }
                    }

                    if(newModelEntries.Count > 0) {
                        var modelLogDir = Path.GetDirectoryName(modelLogPath);
                        if(!String.IsNullOrEmpty(modelLogDir) && !Directory.Exists(modelLogDir)) {
                            Directory.CreateDirectory(modelLogDir);
                        }
                        File.AppendAllText(modelLogPath, String.Join(Environment.NewLine, newModelEntries) + Environment.NewLine);
                        statusChanged = true;
                    }
                }

                if(statusChanged) {
                    UpdateStatus();
                }
            } catch {
                // Don't let exceptions from the heartbeat tick crash VRS
            }
        }
    }
}
