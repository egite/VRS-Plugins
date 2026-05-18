using System;
using System.Windows.Forms;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.StratuxGPS.WebAdmin
{
    class OptionsView : IView
    {
        public DialogResult ShowView() { return DialogResult.OK; }
        public void Dispose() { }

        [WebAdminMethod]
        public ViewModel GetState()
        {
            var options = OptionsStorage.Load(Plugin.Singleton);
            var detectedAddress = Plugin.DetectStratuxAddress();
            return new ViewModel(options, detectedAddress);
        }

        [WebAdminMethod(DeferExecution = true)]
        public SaveOutcomeModel Save(ViewModel viewModel)
        {
            var outcome = "";

            var options = new Options();
            viewModel.CopyToSettings(options);

            try {
                OptionsStorage.Save(Plugin.Singleton, options);
                Plugin.Singleton?.ApplyOptions(options);
                outcome = "Saved";
            } catch(Exception) {
                outcome = "ConflictingUpdate";
            }

            options = OptionsStorage.Load(Plugin.Singleton);
            var detectedAddress = Plugin.DetectStratuxAddress();
            viewModel.RefreshFromSettings(options, detectedAddress);

            return new SaveOutcomeModel(outcome, viewModel);
        }
    }
}
