using System;

namespace VirtualRadar.Plugin.MissingLogos.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public string LogFileName { get; set; }
        public bool TrackMissingModels { get; set; }
        public string ModelLogFileName { get; set; }
        public string LogContents { get; set; }
        public string ModelLogContents { get; set; }

        public ViewModel()
        {
        }

        public ViewModel(Options options) : this()
        {
            RefreshFromSettings(options);
        }

        public void RefreshFromSettings(Options options)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            LogFileName = options.LogFileName ?? "";
            TrackMissingModels = options.TrackMissingModels;
            ModelLogFileName = options.ModelLogFileName ?? "";
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.LogFileName = LogFileName;
            options.TrackMissingModels = TrackMissingModels;
            options.ModelLogFileName = ModelLogFileName;
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
