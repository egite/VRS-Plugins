using System;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.PilotsView.WinForms
{
    public partial class OptionsView : BaseForm
    {
        public Options Options { get; set; }

        public OptionsView()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                checkBoxEnabled.Checked = Options.Enabled;
                numericRefreshInterval.Value = Math.Max(1, Math.Min(10, Options.RefreshInterval));
                numericCameraTilt.Value = Math.Max(0, Math.Min(90, Options.CameraTilt));
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.RefreshInterval = (int)numericRefreshInterval.Value;
            Options.CameraTilt = (int)numericCameraTilt.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
