using System;

namespace VirtualRadar.Plugin.StratuxGPS.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public string StratuxAddress { get; set; }
        public int StratuxPort { get; set; }
        public int PollIntervalMilliseconds { get; set; }
        public string DetectedAddress { get; set; }

        public ViewModel()
        {
        }

        public ViewModel(Options options, string detectedAddress) : this()
        {
            RefreshFromSettings(options, detectedAddress);
        }

        public void RefreshFromSettings(Options options, string detectedAddress)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            StratuxAddress = options.StratuxAddress;
            StratuxPort = options.StratuxPort;
            PollIntervalMilliseconds = options.PollIntervalMilliseconds;
            DetectedAddress = detectedAddress;
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.StratuxAddress = (StratuxAddress ?? "").Trim();
            options.StratuxPort = StratuxPort;
            options.PollIntervalMilliseconds = PollIntervalMilliseconds;
        }
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
