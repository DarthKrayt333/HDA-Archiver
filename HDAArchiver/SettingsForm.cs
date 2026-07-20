using System;
using System.Drawing;
using System.Windows.Forms;

namespace HDAArchiver
{
    public class SettingsForm : Form
    {
        private CheckBox chkCompressed;
        private CheckBox chkConfirmDelete;
        private CheckBox chkShowSize;
        private CheckBox chkShowCompressed;
        private Button btnOK;
        private Button btnCancel;

        public SettingsForm()
        {
            BuildUI();
            LoadSettings();
        }

        private void BuildUI()
        {
            Text = "HDA Archiver — Settings";
            Size = new Size(380, 280);
            FormBorderStyle =
                FormBorderStyle.FixedDialog;
            StartPosition =
                FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            // ── Inherit icon from parent form ──
            ShowIcon = true;

            // Try to load icon
            try
            {
                var asm =
                    System.Reflection.Assembly
                          .GetExecutingAssembly();

                foreach (string name in
                         asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(
                            ".ico",
                            StringComparison
                                .OrdinalIgnoreCase))
                    {
                        using (var stream =
                            asm.GetManifestResourceStream(
                                name))
                        {
                            if (stream != null)
                                Icon = new Icon(stream);
                        }
                        break;
                    }
                }
            }
            catch { }

            // ── Title label ────────────────────
            var lblTitle = new Label
            {
                Text = "Archive Settings",
                Font = new Font(
                    "Segoe UI", 11,
                    FontStyle.Bold),
                ForeColor =
                    Color.FromArgb(0, 180, 255),
                Location = new Point(16, 16),
                AutoSize = true
            };

            // ── Checkboxes ──────────────────────
            chkCompressed = MakeCheck(
                "Compress files when packing" +
                " (Smart Compression)",
                new Point(16, 56));

            chkConfirmDelete = MakeCheck(
                "Confirm before removing" +
                " entries",
                new Point(16, 90));

            chkShowSize = MakeCheck(
                "Show uncompressed size column",
                new Point(16, 124));

            chkShowCompressed = MakeCheck(
                "Show compressed size column",
                new Point(16, 158));

            // ── Buttons ─────────────────────────
            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(90, 32),
                Location = new Point(170, 210),
                BackColor =
                    Color.FromArgb(0, 120, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOK.FlatAppearance
                 .BorderSize = 0;
            btnOK.Click += (s, e) =>
            {
                SaveSettings();
                Close();
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 32),
                Location = new Point(268, 210),
                BackColor =
                    Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance
                      .BorderSize = 0;

            Controls.AddRange(new Control[]
            {
                lblTitle,
                chkCompressed,
                chkConfirmDelete,
                chkShowSize,
                chkShowCompressed,
                btnOK,
                btnCancel
            });
        }

        private CheckBox MakeCheck(
            string text, Point loc)
        {
            return new CheckBox
            {
                Text = text,
                Location = loc,
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
        }

        private void LoadSettings()
        {
            var s = AppSettings.Instance;
            chkCompressed.Checked =
                s.CompressedByDefault;
            chkConfirmDelete.Checked =
                s.ConfirmOnDelete;
            chkShowSize.Checked =
                s.ShowFileSize;
            chkShowCompressed.Checked =
                s.ShowCompressedSize;
        }

        private void SaveSettings()
        {
            var s = AppSettings.Instance;
            s.CompressedByDefault =
                chkCompressed.Checked;
            s.ConfirmOnDelete =
                chkConfirmDelete.Checked;
            s.ShowFileSize =
                chkShowSize.Checked;
            s.ShowCompressedSize =
                chkShowCompressed.Checked;
            s.Save();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.SuspendLayout();
            // 
            // SettingsForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.ResumeLayout(false);

        }
    }
}