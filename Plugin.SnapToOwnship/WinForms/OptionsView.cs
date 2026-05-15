using System;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.SnapToOwnship.WinForms
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
                textBoxIcao.Text = Options.OwnshipIcao ?? "";
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.OwnshipIcao = textBoxIcao.Text.Trim().ToUpperInvariant();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
