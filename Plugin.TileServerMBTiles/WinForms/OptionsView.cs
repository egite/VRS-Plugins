using System;
using System.IO;
using System.Windows.Forms;
using VirtualRadar.WinForms;

namespace VirtualRadar.Plugin.TileServerMBTiles.WinForms
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
                textBoxFolder.Text = Options.FolderPath ?? "";
                checkBoxIsTms.Checked = Options.IsTms;
                UpdateFileCount();
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using(var dialog = new FolderBrowserDialog()) {
                dialog.Description = "Select folder containing .mbtiles files";
                var currentPath = textBoxFolder.Text.Trim();
                if(Directory.Exists(currentPath)) {
                    dialog.SelectedPath = currentPath;
                }

                if(dialog.ShowDialog() == DialogResult.OK) {
                    textBoxFolder.Text = dialog.SelectedPath;
                    UpdateFileCount();
                }
            }
        }

        private void textBoxFolder_TextChanged(object sender, EventArgs e)
        {
            UpdateFileCount();
        }

        private void UpdateFileCount()
        {
            var folder = textBoxFolder.Text.Trim();
            if(Directory.Exists(folder)) {
                var count = Directory.GetFiles(folder, "*.mbtiles").Length;
                labelFileCount.Text = $"{count} .mbtiles file(s) found";
            } else {
                labelFileCount.Text = string.IsNullOrEmpty(folder) ? "" : "Folder not found";
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Options.Enabled = checkBoxEnabled.Checked;
            Options.FolderPath = textBoxFolder.Text.Trim();
            Options.IsTms = checkBoxIsTms.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
