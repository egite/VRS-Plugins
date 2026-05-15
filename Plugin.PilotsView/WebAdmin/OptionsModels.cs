using System;

namespace VirtualRadar.Plugin.PilotsView.WebAdmin
{
    public class ViewModel
    {
        public long DataVersion { get; set; }
        public bool Enabled { get; set; }
        public int RefreshInterval { get; set; }
        public int CameraTilt { get; set; }

        public ViewModel()
        {
            RefreshInterval = 1;
            CameraTilt = 80;
        }

        public ViewModel(Options options) : this()
        {
            RefreshFromSettings(options);
        }

        public void RefreshFromSettings(Options options)
        {
            DataVersion = options.DataVersion;
            Enabled = options.Enabled;
            RefreshInterval = options.RefreshInterval;
            CameraTilt = options.CameraTilt;
        }

        public void CopyToSettings(Options options)
        {
            options.DataVersion = DataVersion;
            options.Enabled = Enabled;
            options.RefreshInterval = RefreshInterval;
            options.CameraTilt = CameraTilt;
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
