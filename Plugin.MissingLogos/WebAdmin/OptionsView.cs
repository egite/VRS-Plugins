using System;
using System.IO;
using System.Windows.Forms;
using VirtualRadar.Interface.View;
using VirtualRadar.Interface.WebSite;

namespace VirtualRadar.Plugin.MissingLogos.WebAdmin
{
    class OptionsView : IView
    {
        public DialogResult ShowView() { return DialogResult.OK; }
        public void Dispose() { }

        private static string ReadLogFile(string path, int maxLines = 500)
        {
            if(string.IsNullOrEmpty(path) || !File.Exists(path)) return "";
            try {
                var lines = File.ReadAllLines(path);
                var start = Math.Max(0, lines.Length - maxLines);
                return string.Join("\n", lines, start, lines.Length - start);
            } catch {
                return "(Unable to read file)";
            }
        }

        [WebAdminMethod]
        public ViewModel GetState()
        {
            var options = OptionsStorage.Load(Plugin.Singleton);
            var viewModel = new ViewModel(options);
            var logPath = !string.IsNullOrEmpty(options.LogFileName) ? options.LogFileName
                : Path.Combine(Plugin.Singleton?.PluginFolder ?? "", "MissingLogos.log");
            var modelLogPath = !string.IsNullOrEmpty(options.ModelLogFileName) ? options.ModelLogFileName
                : Path.Combine(Plugin.Singleton?.PluginFolder ?? "", "MissingModels.log");
            viewModel.LogContents = ReadLogFile(logPath);
            viewModel.ModelLogContents = ReadLogFile(modelLogPath);
            return viewModel;
        }

        [WebAdminMethod]
        public ViewModel RefreshLogs()
        {
            return GetState();
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
