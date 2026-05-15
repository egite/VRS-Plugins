using System;
using System.Windows.Forms;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.LogoMarkers.WebAdmin
{
    class OptionsView : IView
    {
        public DialogResult ShowView() { return DialogResult.OK; }
        public void Dispose() { }

        [WebAdminMethod]
        public ViewModel GetState()
        {
            var options = OptionsStorage.Load(Plugin.Singleton);
            var viewModel = new ViewModel(options);
            return viewModel;
        }

        [WebAdminMethod(DeferExecution = true)]
        public SaveOutcomeModel Save(ViewModel viewModel)
        {
            var outcome = "";

            var options = new Options();
            viewModel.CopyToSettings(options);

            try {
                OptionsStorage.Save(Plugin.Singleton, options);
                outcome = "Saved";
            } catch(Exception) {
                outcome = "ConflictingUpdate";
            }

            options = OptionsStorage.Load(Plugin.Singleton);
            viewModel.RefreshFromSettings(options);

            return new SaveOutcomeModel(outcome, viewModel);
        }
    }
}
