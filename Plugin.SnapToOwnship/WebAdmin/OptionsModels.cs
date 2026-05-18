using System;

namespace VirtualRadar.Plugin.SnapToOwnship.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public bool AutoDetectIcao { get; set; }
        public string OwnshipIcao { get; set; }

        public ViewModel()
        {
            OwnshipIcao = "";
        }

        public ViewModel(Options options) : this()
        {
            RefreshFromSettings(options);
        }

        public void RefreshFromSettings(Options options)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            AutoDetectIcao = options.AutoDetectIcao;
            OwnshipIcao = options.OwnshipIcao ?? "";
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.AutoDetectIcao = AutoDetectIcao;
            options.OwnshipIcao = (OwnshipIcao ?? "").Trim().ToUpperInvariant();
        }
    }

    public class DetectedIcaoModel
    {
        public bool HasIcao { get; set; }
        public string Icao { get; set; }
    }

    public class SaveOutcomeModel
    {
        public string Outcome { get; set; }
        public ViewModel ViewModel { get; set; }

        public SaveOutcomeModel(string outcome, ViewModel viewModel)
        {
            Outcome = outcome;
            ViewModel = viewModel;
        }
    }
}
