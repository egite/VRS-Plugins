using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.CustomLinks.WinForms
{
    /// <summary>
    /// The WinForms options dialog for the Custom Links plugin.
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

                dataGridViewLinks.Rows.Clear();
                foreach(var link in Options.Links) {
                    dataGridViewLinks.Rows.Add(link.Name, link.Url);
                }
            }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            dataGridViewLinks.Rows.Add("", "");
            var newRowIndex = dataGridViewLinks.Rows.Count - 2;
            if(newRowIndex >= 0) {
                dataGridViewLinks.CurrentCell = dataGridViewLinks.Rows[newRowIndex].Cells[0];
                dataGridViewLinks.BeginEdit(true);
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if(dataGridViewLinks.CurrentRow != null && !dataGridViewLinks.CurrentRow.IsNewRow) {
                dataGridViewLinks.Rows.Remove(dataGridViewLinks.CurrentRow);
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.Links.Clear();

            foreach(DataGridViewRow row in dataGridViewLinks.Rows) {
                if(row.IsNewRow) continue;
                var name = (row.Cells[0].Value ?? "").ToString().Trim();
                var url  = (row.Cells[1].Value ?? "").ToString().Trim();
                if(name.Length > 0 || url.Length > 0) {
                    Options.Links.Add(new LinkDefinition { Name = name, Url = url });
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
