using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.LiveATC.WinForms
{
    /// <summary>
    /// The WinForms options dialog for the Live ATC plugin.
    /// </summary>
    public partial class OptionsView : BaseForm
    {
        /// <summary>
        /// Gets or sets the options being edited.
        /// </summary>
        public Options Options { get; set; }

        /// <summary>
        /// Creates a new object.
        /// </summary>
        public OptionsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// See base docs.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if(!DesignMode) {
                checkBoxEnabled.Checked = Options.Enabled;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
