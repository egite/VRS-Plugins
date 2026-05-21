using System;

namespace VirtualRadar.Plugin.DeselectAircraft.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }

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
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
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
