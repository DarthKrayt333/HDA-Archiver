using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HMSTHModdingTool.IO;
using HMSTHModdingTool.IO.Compression;

namespace HDAArchiver
{
    public class MainForm : Form
    {
        // ── UI Controls ────────────────────────
        private MenuStrip menuStrip;
        private ToolStrip toolBar;
        private ListView listView;
        private StatusStrip statusBar;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel statusRight;
        private Panel dropOverlay;
        private Label dropLabel;

        // ── State ──────────────────────────────
        private string _currentHdaPath;
        private List<HdaEntry> _entries =
            new List<HdaEntry>();
        private bool _isDirty;

        // ── Slot Role Constants ────────────────
        private const int ROLE_REGULAR = 0;
        private const int ROLE_BD = 1;
        private const int ROLE_HD = 2;
        private const int ROLE_SQ = 3;

        // ── Constructor ────────────────────────
        public MainForm(string openFile = null)
        {
            BuildUI();
            WireEvents();

            if (!string.IsNullOrEmpty(openFile))
                OpenHda(openFile);
            else
                StartBlankArchive();
        }

        // ══════════════════════════════════════
        // START BLANK ARCHIVE
        // ══════════════════════════════════════

        private void StartBlankArchive()
        {
            _currentHdaPath = null;
            _entries.Clear();
            _isDirty = false;

            UpdateTitle();
            RefreshListView();
            ShowDropOverlay(true);

            SetStatus(
                "Ready — drop files here or" +
                " use Add Files to build your" +
                " archive.");
        }

        // ══════════════════════════════════════
        // UI BUILDER
        // ══════════════════════════════════════

        private void BuildUI()
        {
            Text = "HDA Archiver";
            Size = new Size(900, 600);
            MinimumSize = new Size(640, 400);
            StartPosition =
                FormStartPosition.CenterScreen;
            BackColor =
                Color.FromArgb(25, 25, 25);
            ForeColor = Color.White;
            AllowDrop = true;
            ShowIcon = true;
            ShowInTaskbar = true;
            FormBorderStyle =
                FormBorderStyle.Sizable;

            SetFormIcon();
            BuildMenuStrip();
            BuildToolBar();
            BuildListView();
            BuildDropOverlay();
            BuildStatusBar();

            Controls.AddRange(new Control[]
            {
                listView,
                dropOverlay,
                toolBar,
                menuStrip,
                statusBar
            });

            MainMenuStrip = menuStrip;
        }

        private void SetFormIcon()
        {
            try
            {
                var asm =
                    System.Reflection.Assembly
                          .GetExecutingAssembly();

                string foundName = null;
                foreach (string name in
                    asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(
                            ".ico",
                            StringComparison
                                .OrdinalIgnoreCase))
                    {
                        foundName = name;
                        break;
                    }
                }

                if (foundName != null)
                {
                    using (var stream =
                        asm.GetManifestResourceStream(
                            foundName))
                    {
                        if (stream != null)
                        {
                            Icon =
                                new Icon(stream);
                            return;
                        }
                    }
                }

                string exeDir =
                    AppDomain.CurrentDomain
                             .BaseDirectory;

                string[] tryPaths =
                {
                    Path.Combine(exeDir,
                        "Icon_64x64.ico"),
                    Path.Combine(exeDir,
                        "Resources",
                        "Icon_64x64.ico")
                };

                foreach (string p in tryPaths)
                {
                    if (File.Exists(p))
                    {
                        Icon = new Icon(p);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug
                      .WriteLine(
                          "Icon load failed: " +
                          ex.Message);
            }
        }

        // ── Menu strip ─────────────────────────
        private void BuildMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                BackColor =
                    Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                Renderer =
                    new DarkMenuRenderer()
            };

            var mnuFile =
                new ToolStripMenuItem("File");

            var mnuNew =
                new ToolStripMenuItem(
                    "New HDA Archive...",
                    null, OnNew);
            mnuNew.ShortcutKeys =
                Keys.Control | Keys.N;

            var mnuOpen =
                new ToolStripMenuItem(
                    "Open HDA...",
                    null, OnOpen);
            mnuOpen.ShortcutKeys =
                Keys.Control | Keys.O;

            var mnuSave =
                new ToolStripMenuItem(
                    "Save HDA",
                    null, OnSave);
            mnuSave.ShortcutKeys =
                Keys.Control | Keys.S;

            var mnuSaveAs =
                new ToolStripMenuItem(
                    "Save HDA As...",
                    null, OnSaveAs);

            var mnuClose =
                new ToolStripMenuItem(
                    "Close",
                    null, OnClose);

            var mnuExit =
                new ToolStripMenuItem(
                    "Exit",
                    null,
                    (s, e) => Close());
            mnuExit.ShortcutKeys =
                Keys.Alt | Keys.F4;

            mnuFile.DropDownItems.AddRange(
                new ToolStripItem[]
                {
                    mnuNew, mnuOpen,
                    mnuSave, mnuSaveAs,
                    mnuClose,
                    new ToolStripSeparator(),
                    mnuExit
                });

            var mnuArchive =
                new ToolStripMenuItem("Archive");

            var mnuAdd =
                new ToolStripMenuItem(
                    "Add Files...",
                    null, OnAddFiles);
            mnuAdd.ShortcutKeys =
                Keys.Control | Keys.A;

            var mnuExtractAll =
                new ToolStripMenuItem(
                    "Extract All...",
                    null, OnExtractAll);
            mnuExtractAll.ShortcutKeys =
                Keys.Control | Keys.E;

            var mnuExtractSel =
                new ToolStripMenuItem(
                    "Extract Selected...",
                    null, OnExtractSelected);

            var mnuRemove =
                new ToolStripMenuItem(
                    "Remove Selected",
                    null, OnRemoveSelected);
            mnuRemove.ShortcutKeys =
                Keys.Delete;

            var mnuExtractHere =
                new ToolStripMenuItem(
                    "Extract to HDA-Named Folder",
                    null, OnExtractToHdaFolder);
            mnuExtractHere.ShortcutKeys =
                Keys.Control | Keys.D;

            mnuArchive.DropDownItems.AddRange(
                new ToolStripItem[]
                {
                    mnuAdd,
                    mnuExtractAll,
                    mnuExtractSel,
                    mnuExtractHere,
                    mnuRemove
                });

            var mnuTools =
                new ToolStripMenuItem("Tools");

            var mnuSettings =
                new ToolStripMenuItem(
                    "Settings...",
                    null, OnSettings);

            var mnuRegister =
                new ToolStripMenuItem(
                    "Register as .HDA Handler...",
                    null, OnRegisterHandler);

            mnuTools.DropDownItems.Add(
                mnuRegister);
            mnuTools.DropDownItems.Add(
                new ToolStripSeparator());
            mnuTools.DropDownItems.Add(
                mnuSettings);

            var mnuHelp =
                new ToolStripMenuItem("Help");

            var mnuAbout =
                new ToolStripMenuItem(
                    "About HDA Archiver",
                    null, OnAbout);

            mnuHelp.DropDownItems.Add(mnuAbout);

            menuStrip.Items.AddRange(
                new ToolStripItem[]
                {
                    mnuFile,
                    mnuArchive,
                    mnuTools,
                    mnuHelp
                });
        }

        private void OnRegisterHandler(
            object sender, EventArgs e)
        {
            var r = MessageBox.Show(
                "Register HDA Archiver as the" +
                " default app for .HDA files?" +
                "\r\n\r\n" +
                "This lets you double-click" +
                " .HDA files in Windows Explorer" +
                " to open them with this app." +
                "\r\n\r\n" +
                "Only affects your user account." +
                " No admin required.",
                "HDA Archiver",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (r != DialogResult.Yes) return;

            try
            {
                string exePath =
                    System.Reflection.Assembly
                          .GetExecutingAssembly()
                          .Location;

                using (var key =
                    Microsoft.Win32.Registry
                        .CurrentUser.CreateSubKey(
                            @"Software\Classes\.HDA"))
                {
                    key.SetValue(
                        "", "HDAArchiver.File");
                }

                using (var key =
                    Microsoft.Win32.Registry
                        .CurrentUser.CreateSubKey(
                            @"Software\Classes\" +
                            "HDAArchiver.File"))
                {
                    key.SetValue(
                        "", "HDA Archive");

                    using (var iconKey =
                        key.CreateSubKey(
                            "DefaultIcon"))
                    {
                        iconKey.SetValue(
                            "",
                            "\"" + exePath +
                            "\",0");
                    }

                    using (var cmdKey =
                        key.CreateSubKey(
                            @"shell\open\command"))
                    {
                        cmdKey.SetValue(
                            "",
                            "\"" + exePath +
                            "\" \"%1\"");
                    }
                }

                ShowInfo(
                    "Success!\r\n\r\n" +
                    "You can now double-click" +
                    " .HDA files in Explorer to" +
                    " open them with HDA Archiver.");

                SetStatus(
                    "Registered as .HDA handler.");
            }
            catch (Exception ex)
            {
                ShowError(
                    "Failed to register:\r\n" +
                    ex.Message);
            }
        }

        // ── Toolbar ────────────────────────────
        private void BuildToolBar()
        {
            toolBar = new ToolStrip
            {
                BackColor =
                    Color.FromArgb(40, 40, 40),
                GripStyle =
                    ToolStripGripStyle.Hidden,
                Renderer =
                    new DarkToolStripRenderer()
            };

            toolBar.Items.AddRange(
                new ToolStripItem[]
                {
                    MakeTBtn("New",    OnNew),
                    MakeTBtn("Open",   OnOpen),
                    MakeTBtn("Save",   OnSave),
                    new ToolStripSeparator(),
                    MakeTBtn("Add Files",
                             OnAddFiles),
                    MakeTBtn("Extract All",
                             OnExtractAll),
                    MakeTBtn("Extract Sel.",
                             OnExtractSelected),
                    MakeTBtn("Extract Here",
                             OnExtractToHdaFolder),
                    new ToolStripSeparator(),
                    MakeTBtn("Remove",
                             OnRemoveSelected),
                    new ToolStripSeparator(),
                    MakeTBtn("Settings",
                             OnSettings)
                });
        }

        private ToolStripButton MakeTBtn(
            string text,
            EventHandler handler)
        {
            var btn = new ToolStripButton(text)
            {
                ForeColor = Color.White,
                BackColor =
                    Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Padding = new Padding(6, 2, 6, 2)
            };
            btn.Click += handler;
            return btn;
        }

        // ── ListView ───────────────────────────
        private void BuildListView()
        {
            listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                MultiSelect = true,
                AllowDrop = true,
                BackColor =
                    Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill
            };

            listView.Columns.Add("Name", 320);
            listView.Columns.Add("Type", 80);
            listView.Columns.Add(
                "Size", 100,
                HorizontalAlignment.Right);
            listView.Columns.Add(
                "Packed", 100,
                HorizontalAlignment.Right);
            listView.Columns.Add(
                "Ratio", 80,
                HorizontalAlignment.Right);
            listView.Columns.Add("Status", 100);
        }

        // ── Drop overlay ───────────────────────
        private void BuildDropOverlay()
        {
            dropOverlay = new Panel
            {
                BackColor =
                    Color.FromArgb(25, 25, 25),
                Dock = DockStyle.Fill,
                Visible = true,
                AllowDrop = true
            };

            dropLabel = new Label
            {
                Text =
                    "Drop files here to add them" +
                    "\r\nto your new HDA archive" +
                    "\r\n\r\n" +
                    "Use File → Open to open" +
                    " an existing .HDA file" +
                    "\r\n\r\nFile → Save to write" +
                    " to disk",
                ForeColor =
                    Color.FromArgb(100, 100, 100),
                Font = new Font(
                    "Segoe UI", 14,
                    FontStyle.Regular),
                TextAlign =
                    ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            dropOverlay.Controls.Add(dropLabel);
        }

        // ── Status bar ─────────────────────────
        private void BuildStatusBar()
        {
            statusBar = new StatusStrip
            {
                BackColor =
                    Color.FromArgb(35, 35, 35),
                SizingGrip = false
            };

            statusLabel =
                new ToolStripStatusLabel
                {
                    Text = "Ready",
                    ForeColor = Color.White,
                    Spring = true,
                    TextAlign =
                        ContentAlignment.MiddleLeft
                };

            statusRight =
                new ToolStripStatusLabel
                {
                    Text = "",
                    ForeColor =
                        Color.FromArgb(
                            130, 130, 130),
                    TextAlign =
                        ContentAlignment.MiddleRight
                };

            statusBar.Items.AddRange(
                new ToolStripItem[]
                {
                    statusLabel,
                    statusRight
                });
        }

        // ══════════════════════════════════════
        // EVENT WIRING
        // ══════════════════════════════════════

        private void WireEvents()
        {
            DragEnter += OnFormDragEnter;
            DragDrop += OnFormDragDrop;

            dropOverlay.DragEnter +=
                OnFormDragEnter;
            dropOverlay.DragDrop +=
                OnFormDragDrop;
            dropLabel.AllowDrop = false;

            listView.DragEnter +=
                OnListDragEnter;
            listView.DragDrop +=
                OnListDragDrop;

            listView.ItemDrag +=
                OnListViewItemDrag;
            listView.MouseClick +=
                OnListViewMouseClick;
            listView.KeyDown +=
                OnListViewKeyDown;
            listView.DoubleClick +=
                OnListViewDoubleClick;

            FormClosing += OnFormClosing;
        }

        // ══════════════════════════════════════
        // DRAG OUT
        // ══════════════════════════════════════

        private void OnListViewItemDrag(
            object sender, ItemDragEventArgs e)
        {
            var selected = GetSelectedEntries();
            if (selected.Count == 0) return;

            try
            {
                string tmpDir = Path.Combine(
                    Path.GetTempPath(),
                    "HDAArchiver_DragOut");

                if (Directory.Exists(tmpDir))
                {
                    try
                    {
                        Directory.Delete(
                            tmpDir, true);
                    }
                    catch { }
                }

                Directory.CreateDirectory(tmpDir);

                var filePaths =
                    new List<string>();

                foreach (var en in selected)
                {
                    if (en.Data == null) continue;

                    string outPath =
                        Path.Combine(
                            tmpDir, en.FileName);

                    File.WriteAllBytes(
                        outPath, en.Data);

                    filePaths.Add(outPath);
                }

                if (filePaths.Count == 0) return;

                var dataObj = new DataObject(
                    DataFormats.FileDrop,
                    filePaths.ToArray());

                listView.DoDragDrop(
                    dataObj,
                    DragDropEffects.Copy);

                SetStatus(
                    "Dragged out " +
                    filePaths.Count +
                    " file(s).");
            }
            catch (Exception ex)
            {
                ShowError(
                    "Drag out failed:\r\n" +
                    ex.Message);
            }
        }

        // ══════════════════════════════════════
        // DRAG & DROP — FORM LEVEL
        // ══════════════════════════════════════

        private void OnFormDragEnter(
            object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(
                    DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files != null &&
                files.Length > 0 &&
                files[0].Contains(
                    "HDAArchiver_DragOut"))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void OnFormDragDrop(
            object sender, DragEventArgs e)
        {
            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files == null ||
                files.Length == 0) return;

            AddFilesToArchive(files);
        }

        // ══════════════════════════════════════
        // DRAG & DROP — LISTVIEW LEVEL
        // ══════════════════════════════════════

        private void OnListDragEnter(
            object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(
                    DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files != null &&
                files.Length > 0 &&
                files[0].Contains(
                    "HDAArchiver_DragOut"))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            e.Effect = DragDropEffects.Copy;
        }

        private void OnListDragDrop(
            object sender, DragEventArgs e)
        {
            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files == null ||
                files.Length == 0) return;

            AddFilesToArchive(files);
        }

        // ══════════════════════════════════════
        // EXTRACT TO HDA-NAMED FOLDER
        // ══════════════════════════════════════

        private void OnExtractToHdaFolder(
            object sender, EventArgs e)
        {
            if (_currentHdaPath == null)
            {
                ShowInfo("No archive is open.");
                return;
            }

            if (_entries.Count == 0)
            {
                ShowInfo("Archive is empty.");
                return;
            }

            string hdaDir =
                Path.GetDirectoryName(
                    _currentHdaPath);

            string hdaName =
                Path.GetFileNameWithoutExtension(
                    _currentHdaPath);

            string outFolder =
                Path.Combine(hdaDir, hdaName);

            if (Directory.Exists(outFolder))
            {
                var r = MessageBox.Show(
                    string.Format(
                        "Folder already exists:" +
                        "\r\n{0}\r\n\r\n" +
                        "Overwrite files inside?",
                        outFolder),
                    "HDA Archiver",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                    return;
            }

            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(
                    outFolder);

            ExtractEntries(_entries, outFolder);
        }

        // ══════════════════════════════════════
        // LISTVIEW EVENTS
        // ══════════════════════════════════════

        private void OnListViewMouseClick(
            object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            if (_entries.Count == 0) return;

            var ctx = new ContextMenuStrip
            {
                BackColor =
                    Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Renderer =
                    new DarkMenuRenderer()
            };

            bool hasSel =
                listView.SelectedItems.Count > 0;
            bool single =
                listView.SelectedItems.Count == 1;

            if (single)
            {
                var selEntry =
                    listView.SelectedItems[0].Tag
                    as HdaEntry;

                if (selEntry != null &&
                    !selEntry.IsManifest)
                {
                    ctx.Items.Add(
                        new ToolStripMenuItem(
                            "Replace with File...",
                            null, OnReplaceFile));
                    ctx.Items.Add(
                        new ToolStripMenuItem(
                            "Rename...",
                            null, OnRenameFile));
                    ctx.Items.Add(
                        new ToolStripSeparator());
                }
            }

            if (hasSel)
                ctx.Items.Add(
                    new ToolStripMenuItem(
                        "Extract Selected...",
                        null, OnExtractSelected));

            ctx.Items.Add(
                new ToolStripMenuItem(
                    "Extract All...",
                    null, OnExtractAll));

            ctx.Items.Add(
                new ToolStripMenuItem(
                    "Extract to HDA-Named Folder",
                    null, OnExtractToHdaFolder));

            if (hasSel)
            {
                ctx.Items.Add(
                    new ToolStripSeparator());
                ctx.Items.Add(
                    new ToolStripMenuItem(
                        "Remove Selected",
                        null, OnRemoveSelected));
            }

            ctx.Show(listView, e.Location);
        }

        private void OnReplaceFile(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count != 1)
                return;

            var entry =
                listView.SelectedItems[0].Tag
                as HdaEntry;

            if (entry == null ||
                entry.IsManifest) return;

            var dlg = new OpenFileDialog
            {
                Title =
                    "Replace " +
                    entry.FileName + " with...",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return;

            try
            {
                byte[] newData =
                    File.ReadAllBytes(dlg.FileName);

                entry.Data = newData;
                entry.DecompressedSize =
                    newData.Length;

                string newExt =
                    DetectExt(newData);
                if (newExt == ".bin")
                    newExt = Path.GetExtension(
                        dlg.FileName);
                entry.Extension = newExt;

                CalculateCompression(
                    newData,
                    out long storedSize,
                    out bool isCompressed);

                entry.StoredSize = storedSize;
                entry.IsCompressed = isCompressed;

                _isDirty = true;
                RefreshListView();
                UpdateStatusBar();
                UpdateTitle();

                SetStatus(
                    "Replaced with: " +
                    Path.GetFileName(dlg.FileName));
            }
            catch (Exception ex)
            {
                ShowError(
                    "Replace failed:\r\n" +
                    ex.Message);
            }
        }

        private void OnRenameFile(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count != 1)
                return;

            var entry =
                listView.SelectedItems[0].Tag
                as HdaEntry;

            if (entry == null ||
                entry.IsManifest) return;

            string newName = PromptForName(
                "Rename file",
                "New file name:",
                entry.FileName);

            if (string.IsNullOrEmpty(newName))
                return;

            entry.FileName = newName;
            entry.Extension =
                Path.GetExtension(newName);

            _isDirty = true;
            RefreshListView();
            UpdateTitle();
            SetStatus("Renamed to: " + newName);
        }

        private string PromptForName(
            string title, string prompt,
            string defaultValue)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.Size = new Size(400, 160);
                form.StartPosition =
                    FormStartPosition.CenterParent;
                form.FormBorderStyle =
                    FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor =
                    Color.FromArgb(30, 30, 30);
                form.ForeColor = Color.White;

                var lbl = new Label
                {
                    Text = prompt,
                    Location = new Point(12, 15),
                    AutoSize = true,
                    ForeColor = Color.White
                };

                var txt = new TextBox
                {
                    Text = defaultValue,
                    Location = new Point(12, 40),
                    Size = new Size(360, 25),
                    BackColor =
                        Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White,
                    BorderStyle =
                        BorderStyle.FixedSingle
                };

                var btnOK = new Button
                {
                    Text = "OK",
                    DialogResult =
                        DialogResult.OK,
                    Location = new Point(196, 80),
                    Size = new Size(85, 28),
                    BackColor =
                        Color.FromArgb(0, 120, 215),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOK.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult =
                        DialogResult.Cancel,
                    Location = new Point(287, 80),
                    Size = new Size(85, 28),
                    BackColor =
                        Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnCancel.FlatAppearance
                         .BorderSize = 0;

                form.Controls.Add(lbl);
                form.Controls.Add(txt);
                form.Controls.Add(btnOK);
                form.Controls.Add(btnCancel);
                form.AcceptButton = btnOK;
                form.CancelButton = btnCancel;

                txt.SelectAll();
                txt.Focus();

                if (form.ShowDialog(this) ==
                    DialogResult.OK)
                    return txt.Text.Trim();
            }

            return null;
        }

        private void OnListViewKeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                OnRemoveSelected(
                    sender, EventArgs.Empty);
        }

        private void OnListViewDoubleClick(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;

            var entry =
                listView.SelectedItems[0].Tag
                as HdaEntry;

            if (entry == null ||
                entry.Data == null) return;

            try
            {
                string tmp = Path.Combine(
                    Path.GetTempPath(),
                    "HDAArchiver");

                if (!Directory.Exists(tmp))
                    Directory.CreateDirectory(tmp);

                string outPath = Path.Combine(
                    tmp, entry.FileName);

                File.WriteAllBytes(
                    outPath, entry.Data);

                System.Diagnostics.Process
                      .Start(outPath);
            }
            catch (Exception ex)
            {
                ShowError(
                    "Could not open file:\r\n" +
                    ex.Message);
            }
        }

        private void OnFormClosing(
            object sender,
            FormClosingEventArgs e)
        {
            if (!_isDirty) return;

            var r = MessageBox.Show(
                "You have unsaved changes.\r\n" +
                "Save before closing?",
                "HDA Archiver",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (r == DialogResult.Yes)
                OnSave(sender, EventArgs.Empty);
            else if (r == DialogResult.Cancel)
                e.Cancel = true;
        }

        // ══════════════════════════════════════
        // MENU / TOOLBAR HANDLERS
        // ══════════════════════════════════════

        private void OnNew(
            object sender, EventArgs e)
        {
            if (!ConfirmCloseCurrentArchive())
                return;

            StartBlankArchive();

            SetStatus(
                "New blank archive created." +
                " Add files and press Save.");
        }

        private void OnOpen(
            object sender, EventArgs e)
        {
            if (!ConfirmCloseCurrentArchive())
                return;

            var dlg = new OpenFileDialog
            {
                Title = "Open HDA Archive",
                Filter =
                    "HDA Archives (*.HDA)|*.HDA" +
                    "|All files (*.*)|*.*",
                DefaultExt = "HDA"
            };

            if (!string.IsNullOrEmpty(
                    AppSettings.Instance
                               .LastOpenFolder))
                dlg.InitialDirectory =
                    AppSettings.Instance
                               .LastOpenFolder;

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return;

            AppSettings.Instance.LastOpenFolder =
                Path.GetDirectoryName(
                    dlg.FileName);
            AppSettings.Instance.Save();

            OpenHda(dlg.FileName);
        }

        private void OnSave(
            object sender, EventArgs e)
        {
            if (_currentHdaPath == null)
            {
                OnSaveAs(sender, e);
                return;
            }

            SaveHda(_currentHdaPath);
        }

        private void OnSaveAs(
            object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save HDA Archive As",
                Filter =
                    "HDA Archives (*.HDA)|*.HDA" +
                    "|All files (*.*)|*.*",
                DefaultExt = "HDA"
            };

            if (_currentHdaPath != null)
                dlg.FileName =
                    Path.GetFileName(
                        _currentHdaPath);

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return;

            _currentHdaPath = dlg.FileName;
            SaveHda(_currentHdaPath);
            UpdateTitle();
        }

        private void OnClose(
            object sender, EventArgs e)
        {
            if (!ConfirmCloseCurrentArchive())
                return;

            StartBlankArchive();
        }

        private void OnAddFiles(
            object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Add Files to Archive",
                Filter = "All files (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return;

            AddFilesToArchive(dlg.FileNames);
        }

        private void OnExtractAll(
            object sender, EventArgs e)
        {
            if (_entries.Count == 0)
            {
                ShowInfo("Archive is empty.");
                return;
            }

            string folder = PickExtractFolder();
            if (folder == null) return;

            ExtractEntries(_entries, folder);
        }

        private void OnExtractSelected(
            object sender, EventArgs e)
        {
            var selected = GetSelectedEntries();

            if (selected.Count == 0)
            {
                ShowInfo("No entries selected.");
                return;
            }

            string folder = PickExtractFolder();
            if (folder == null) return;

            ExtractEntries(selected, folder);
        }

        private void OnRemoveSelected(
            object sender, EventArgs e)
        {
            var selected = GetSelectedEntries();
            if (selected.Count == 0) return;

            if (AppSettings.Instance.ConfirmOnDelete)
            {
                var r = MessageBox.Show(
                    string.Format(
                        "Remove {0} file(s)" +
                        " from the archive?",
                        selected.Count),
                    "HDA Archiver",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                    return;
            }

            foreach (var entry in selected)
                _entries.Remove(entry);

            _isDirty = true;
            RefreshListView();
            UpdateStatusBar();
            UpdateTitle();

            SetStatus(
                selected.Count +
                " file(s) removed.");
        }

        private void OnSettings(
            object sender, EventArgs e)
        {
            using (var sf = new SettingsForm())
                sf.ShowDialog(this);
        }

        private void OnAbout(
            object sender, EventArgs e)
        {
            MessageBox.Show(
                "HDA Archiver v1.0\r\n\r\n" +
                "Part of HMSTHModdingTool\r\n" +
                "Original HDATextTool by gdkchan\r\n" +
                "HMSTHModdingTool by DarthKrayt333" +
                "\r\n\r\n" +
                "Supports:\r\n" +
                "  • Open / Create / Save" +
                " .HDA archives\r\n" +
                "  • Gap slot manifest .bin" +
                " auto-detection\r\n" +
                "  • Smart Compression" +
                " (V1/V2 LZO)\r\n" +
                "  • Drag & Drop files in/out\r\n" +
                "  • Auto file type detection\r\n" +
                "  • RDTB, GDTB, SRDB, ELF," +
                " audio\r\n\r\n" +
                "Windows XP / Vista / 7 / 8 /" +
                " 10 / 11\r\n" +
                "32-bit and 64-bit compatible",
                "About HDA Archiver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ══════════════════════════════════════
        // CORE LOGIC — OPEN HDA
        //
        // Opens the HDA file by unpacking it
        // to a temporary folder using the same
        // HarvestDataArchive.Unpack() method
        // that the command line tool uses.
        //
        // This means:
        //   - All real files are extracted
        //   - If the HDA has gap slots, a
        //     manifest .bin is written as the
        //     LAST file in the temp folder
        //     automatically
        //
        // All files in the temp folder are then
        // loaded into _entries including the
        // manifest .bin as the last entry.
        //
        // The manifest is shown in the ListView
        // with a special "MANIFEST" type label
        // so the user knows what it is.
        //
        // On Save, if a manifest entry exists
        // in _entries, SaveHda() detects it and
        // uses PackWithManifest logic to rebuild
        // the HDA with correct gap positions.
        // ══════════════════════════════════════

        private void OpenHda(string path)
        {
            if (!File.Exists(path))
            {
                ShowError(
                    "File not found:\r\n" + path);
                return;
            }

            SetStatus(
                "Opening " +
                Path.GetFileName(path) +
                " ...");

            try
            {
                // ── Extract HDA to temp folder ─
                string archiveName =
                    Path.GetFileNameWithoutExtension(
                        path).ToUpper();

                string tempFolder =
                    Path.Combine(
                        Path.GetTempPath(),
                        "HDAArchiver_Open_" +
                        archiveName);

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(
                            tempFolder, true);
                    }
                    catch { }
                }

                Directory.CreateDirectory(
                    tempFolder);

                // Use HarvestDataArchive.Unpack
                // which writes all real files AND
                // the manifest .bin automatically
                HarvestDataArchive.Unpack(
                    path, tempFolder);

                // ── Load all files from temp ───
                // Sort by filename so manifest
                // .bin (highest index) is last
                string[] filesOnDisk =
                    Directory.GetFiles(tempFolder);

                Array.Sort(filesOnDisk,
                    (a, b) => string.Compare(
                        Path.GetFileName(a),
                        Path.GetFileName(b),
                        StringComparison
                            .OrdinalIgnoreCase));

                _entries.Clear();

                foreach (string fp in filesOnDisk)
                {
                    byte[] data =
                        File.ReadAllBytes(fp);

                    string fname =
                        Path.GetFileName(fp);
                    string ext =
                        Path.GetExtension(fp);

                    // ── Detect if manifest ─────
                    bool isManifest =
                        IsManifestContent(data);

                    CalculateCompression(
                        data,
                        out long storedSize,
                        out bool isCompressed);

                    _entries.Add(new HdaEntry
                    {
                        SlotIndex =
                            _entries.Count,
                        FileName = fname,
                        Extension = ext,
                        Data = data,
                        DecompressedSize =
                            data.Length,
                        StoredSize =
                            storedSize,
                        IsCompressed =
                            isCompressed,
                        IsEmpty = false,
                        IsManifest = isManifest
                    });
                }

                _currentHdaPath = path;
                _isDirty = false;

                AppSettings.Instance
                           .LastOpenFolder =
                    Path.GetDirectoryName(path);
                AppSettings.Instance.Save();

                ShowDropOverlay(false);
                RefreshListView();
                UpdateTitle();
                UpdateStatusBar();

                int realCount =
                    _entries.Count(
                        en => !en.IsManifest);

                bool hasManifest =
                    _entries.Any(
                        en => en.IsManifest);

                string statusMsg =
                    "Opened: " +
                    Path.GetFileName(path) +
                    " — " + realCount +
                    " file(s)";

                if (hasManifest)
                    statusMsg +=
                        "  [gap slot manifest" +
                        " detected]";

                SetStatus(statusMsg);
            }
            catch (Exception ex)
            {
                ShowError(
                    "Failed to open archive:\r\n"
                    + ex.Message);
                SetStatus("Error opening file.");
            }
        }

        // ══════════════════════════════════════
        // CORE LOGIC — SAVE HDA
        //
        // If _entries contains a manifest .bin
        // entry, SaveHda() writes all files to
        // a temp folder including the manifest,
        // then calls HarvestDataArchive.Pack()
        // which auto-detects the manifest and
        // uses PackWithManifest to preserve the
        // gap slot layout.
        //
        // If no manifest exists, files are
        // packed consecutively as normal using
        // PackCompressedLegacy or PackLegacy.
        // ══════════════════════════════════════

        private void SaveHda(string path)
        {
            bool compress =
                AppSettings.Instance
                           .CompressedByDefault;

            SetStatus(
                "Saving " +
                Path.GetFileName(path) +
                " ...");

            try
            {
                // ── Write all entries to temp ──
                string tempFolder =
                    Path.Combine(
                        Path.GetTempPath(),
                        "HDAArchiver_Save_" +
                        Path.GetFileNameWithoutExtension(
                            path).ToUpper());

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(
                            tempFolder, true);
                    }
                    catch { }
                }

                Directory.CreateDirectory(
                    tempFolder);

                // Write every entry to temp
                // folder in its current order.
                // Manifest .bin will be there
                // too if one exists — Pack()
                // will detect it automatically.
                foreach (var en in _entries)
                {
                    if (en.Data == null) continue;

                    File.WriteAllBytes(
                        Path.Combine(
                            tempFolder,
                            en.FileName),
                        en.Data);
                }

                // ── Pack using HarvestDataArchive
                // which auto-detects manifest ──
                if (compress)
                    HarvestDataArchive
                        .PackCompressed(
                            path, tempFolder);
                else
                    HarvestDataArchive.Pack(
                        path, tempFolder);

                _isDirty = false;
                UpdateTitle();
                SetStatus(
                    "Saved: " +
                    Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                ShowError(
                    "Failed to save:\r\n" +
                    ex.Message);
                SetStatus("Save failed.");
            }
        }

        // ══════════════════════════════════════
        // ADD FILES TO ARCHIVE
        // ══════════════════════════════════════

        private void AddFilesToArchive(
            string[] filePaths)
        {
            int added = 0, replaced = 0;

            foreach (string fp in filePaths)
            {
                if (!File.Exists(fp)) continue;

                byte[] data;
                try
                {
                    data = File.ReadAllBytes(fp);
                }
                catch (Exception ex)
                {
                    SetStatus(
                        "Skipped " +
                        Path.GetFileName(fp) +
                        ": " + ex.Message);
                    continue;
                }

                string fname =
                    Path.GetFileName(fp);
                string ext =
                    Path.GetExtension(fp);

                bool isManifest =
                    IsManifestContent(data);

                CalculateCompression(
                    data,
                    out long storedSize,
                    out bool isCompressed);

                var existing =
                    _entries.FirstOrDefault(
                        en =>
                        string.Equals(
                            en.FileName,
                            fname,
                            StringComparison
                                .OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Data = data;
                    existing.Extension = ext;
                    existing.DecompressedSize =
                        data.Length;
                    existing.StoredSize =
                        storedSize;
                    existing.IsCompressed =
                        isCompressed;
                    existing.IsManifest =
                        isManifest;
                    replaced++;
                    continue;
                }

                _entries.Add(new HdaEntry
                {
                    SlotIndex = _entries.Count,
                    FileName = fname,
                    Extension = ext,
                    Data = data,
                    DecompressedSize = data.Length,
                    StoredSize = storedSize,
                    IsCompressed = isCompressed,
                    IsEmpty = false,
                    IsManifest = isManifest
                });

                added++;
            }

            if (added > 0 || replaced > 0)
            {
                _isDirty = true;
                ShowDropOverlay(false);
                RefreshListView();
                UpdateStatusBar();
                UpdateTitle();

                string msg = "";
                if (added > 0)
                    msg += added + " added";
                if (replaced > 0)
                {
                    if (msg.Length > 0)
                        msg += ", ";
                    msg += replaced + " replaced";
                }

                SetStatus(msg + ".");
            }
        }

        // ══════════════════════════════════════
        // EXTRACT ENTRIES
        // ══════════════════════════════════════

        private void ExtractEntries(
            List<HdaEntry> entries,
            string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            int ok = 0, failed = 0;

            foreach (var en in entries)
            {
                if (en.Data == null) continue;

                try
                {
                    File.WriteAllBytes(
                        Path.Combine(
                            folder, en.FileName),
                        en.Data);
                    ok++;
                }
                catch { failed++; }
            }

            string msg = string.Format(
                "Extracted {0} file(s) to:" +
                "\r\n{1}", ok, folder);

            if (failed > 0)
                msg += string.Format(
                    "\r\n{0} file(s) failed.",
                    failed);

            ShowInfo(msg);
            SetStatus(
                "Extracted " + ok +
                " file(s) → " + folder);
        }

        // ══════════════════════════════════════
        // LISTVIEW REFRESH
        //
        // Shows all entries including manifest.
        // Manifest entry shown in distinct color
        // with "MANIFEST" type label so user can
        // identify it but not confuse it with a
        // real game file.
        // ══════════════════════════════════════

        private void RefreshListView()
        {
            listView.BeginUpdate();
            listView.Items.Clear();

            int rowIdx = 0;

            foreach (var en in _entries)
            {
                var item =
                    new ListViewItem(en.FileName);

                if (en.IsManifest)
                {
                    // ── Manifest entry ────────
                    item.SubItems.Add("MANIFEST");
                    item.SubItems.Add(
                        FormatSize(
                            en.DecompressedSize));
                    item.SubItems.Add("—");
                    item.SubItems.Add("—");
                    item.SubItems.Add(
                        "Slot layout");

                    // Distinct color so user
                    // knows this is special
                    item.ForeColor =
                        Color.FromArgb(
                            0, 180, 255);
                    item.BackColor =
                        Color.FromArgb(
                            28, 35, 42);
                }
                else
                {
                    // ── Regular file entry ────
                    string typeDisplay =
                        string.IsNullOrEmpty(
                            en.Extension)
                            ? "FILE"
                            : en.Extension
                                .TrimStart('.')
                                .ToUpper();

                    item.SubItems.Add(typeDisplay);
                    item.SubItems.Add(
                        en.SizeDisplay);
                    item.SubItems.Add(
                        en.StoredDisplay);
                    item.SubItems.Add(
                        en.RatioDisplay);
                    item.SubItems.Add(
                        en.IsCompressed
                            ? "Compressed"
                            : "Stored");

                    item.ForeColor = Color.White;
                    item.BackColor =
                        rowIdx % 2 == 0
                            ? Color.FromArgb(
                                30, 30, 30)
                            : Color.FromArgb(
                                38, 38, 38);

                    rowIdx++;
                }

                item.Tag = en;
                listView.Items.Add(item);
            }

            listView.EndUpdate();
        }

        // ══════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════

        private bool ConfirmCloseCurrentArchive()
        {
            if (!_isDirty) return true;

            var r = MessageBox.Show(
                "Current archive has unsaved" +
                " changes. Close anyway?",
                "HDA Archiver",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            return r == DialogResult.Yes;
        }

        private List<HdaEntry>
            GetSelectedEntries()
        {
            var list = new List<HdaEntry>();
            foreach (ListViewItem item in
                listView.SelectedItems)
            {
                var en = item.Tag as HdaEntry;
                if (en != null)
                    list.Add(en);
            }
            return list;
        }

        private string PickExtractFolder()
        {
            var dlg = new FolderBrowserDialog
            {
                Description =
                    "Select extraction folder",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(
                    AppSettings.Instance
                               .LastExtractFolder))
                dlg.SelectedPath =
                    AppSettings.Instance
                               .LastExtractFolder;

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return null;

            AppSettings.Instance
                       .LastExtractFolder =
                dlg.SelectedPath;
            AppSettings.Instance.Save();

            return dlg.SelectedPath;
        }

        private void ShowDropOverlay(bool show)
        {
            dropOverlay.Visible = show;
            dropOverlay.BringToFront();

            if (!show)
                listView.BringToFront();
        }

        private void UpdateTitle()
        {
            if (_currentHdaPath == null)
            {
                Text =
                    "HDA Archiver — Untitled" +
                    (_isDirty ? " *" : "");
            }
            else
            {
                Text = string.Format(
                    "HDA Archiver — {0}{1}",
                    Path.GetFileName(
                        _currentHdaPath),
                    _isDirty ? " *" : "");
            }
        }

        private void UpdateStatusBar()
        {
            if (_entries.Count == 0)
            {
                statusRight.Text = "";
                return;
            }

            int real =
                _entries.Count(
                    en => !en.IsManifest);

            long totalRaw = _entries
                .Where(en => !en.IsManifest)
                .Sum(en => en.DecompressedSize);

            long totalStored = _entries
                .Where(en => !en.IsManifest)
                .Sum(en => en.StoredSize);

            statusRight.Text = string.Format(
                "{0} file(s)  Raw: {1}" +
                "  Packed: {2}",
                real,
                FormatSize(totalRaw),
                FormatSize(totalStored));
        }

        private void SetStatus(string msg)
        {
            statusLabel.Text = msg;
        }

        private static string FormatSize(
            long bytes)
        {
            if (bytes >= 1024 * 1024)
                return string.Format(
                    "{0:F2} MB",
                    bytes / (1024.0 * 1024.0));

            if (bytes >= 1024)
                return string.Format(
                    "{0:F1} KB",
                    bytes / 1024.0);

            return bytes + " B";
        }

        private static int Align(int v)
        {
            if ((v & 0xF) != 0)
                v = ((v & ~0xF) + 0x10);
            return v;
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(
                msg,
                "HDA Archiver — Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void ShowInfo(string msg)
        {
            MessageBox.Show(
                msg,
                "HDA Archiver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ══════════════════════════════════════
        // MANIFEST CONTENT DETECTION
        //
        // Checks if a byte array is a manifest
        // file by reading the first line and
        // checking for the "SLOTS=" prefix.
        // Same logic as HarvestDataArchive.
        // ══════════════════════════════════════

        private static bool IsManifestContent(
            byte[] data)
        {
            if (data == null || data.Length < 6)
                return false;

            try
            {
                // Read just the first line
                using (var ms =
                    new MemoryStream(data))
                using (var sr =
                    new StreamReader(ms))
                {
                    string firstLine =
                        sr.ReadLine();

                    return firstLine != null &&
                           firstLine.Trim()
                                    .StartsWith(
                                        "SLOTS=");
                }
            }
            catch { return false; }
        }

        // ══════════════════════════════════════
        // EXTENSION DETECTION
        // Magic headers only. Never returns .BD.
        // ══════════════════════════════════════

        private static readonly byte[] MagicRdtb =
            { 0x52, 0x44, 0x54, 0x42 };
        private static readonly byte[] MagicGdtb =
            { 0x47, 0x44, 0x54, 0x42 };
        private static readonly byte[] MagicSrdb =
            { 0x53, 0x52, 0x44, 0x42 };
        private static readonly byte[] MagicElf =
            { 0x7F, 0x45, 0x4C, 0x46 };
        private static readonly byte[] MagicHda =
        {
            0x10, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        };
        private static readonly byte[] MagicHd =
        {
            0x49, 0x45, 0x43, 0x53,
            0x73, 0x72, 0x65, 0x56
        };
        private static readonly byte[] MagicSq2 =
        {
            0x49, 0x45, 0x43, 0x53,
            0x75, 0x71, 0x65, 0x53
        };

        private static string DetectExt(byte[] d)
        {
            if (d == null || d.Length < 4)
                return ".bin";

            if (SW(d, MagicGdtb)) return ".gdtb";
            if (SW(d, MagicRdtb)) return ".rdtb";
            if (SW(d, MagicSrdb)) return ".srdb";
            if (SW(d, MagicElf)) return ".elf";

            if (d.Length >= 16 &&
                SW(d, MagicHda)) return ".HDA";

            if (IsSQ(d)) return ".SQ";
            if (IsHD(d)) return ".HD";

            return ".bin";
        }

        private static bool SW(byte[] d, byte[] m)
        {
            if (d.Length < m.Length) return false;
            for (int i = 0; i < m.Length; i++)
                if (d[i] != m[i]) return false;
            return true;
        }

        private static bool IsHD(byte[] d)
        {
            if (d == null || d.Length < 8)
                return false;
            if (!SW(d, MagicHd)) return false;
            if (IsSQ(d)) return false;
            return true;
        }

        private static bool IsSQ(byte[] d)
        {
            if (d == null || d.Length < 0x18)
                return false;
            if (!SW(d, MagicHd)) return false;
            for (int i = 0;
                 i < MagicSq2.Length; i++)
                if (d[0x10 + i] != MagicSq2[i])
                    return false;
            return true;
        }

        // ══════════════════════════════════════
        // COMPRESSION HELPER
        // ══════════════════════════════════════

        private void CalculateCompression(
            byte[] data,
            out long storedSize,
            out bool isCompressed)
        {
            storedSize = data.Length;
            isCompressed = false;

            if (!AppSettings.Instance
                            .CompressedByDefault)
                return;

            if (data.Length <= 64) return;

            byte[] comp =
                HarvestCompression.Compress(data);

            bool verified =
                HarvestCompression.VerifyRoundTrip(
                    data, comp);

            if (!verified) return;

            if (comp.Length < data.Length)
            {
                storedSize = comp.Length;
                isCompressed = true;
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel
                  .ComponentResourceManager
                  resources =
                new System.ComponentModel
                    .ComponentResourceManager(
                        typeof(MainForm));

            this.SuspendLayout();
            this.ClientSize =
                new System.Drawing.Size(284, 261);
            this.Icon =
                ((System.Drawing.Icon)(
                    resources.GetObject(
                        "$this.Icon")));
            this.Name = "MainForm";
            this.Load +=
                new System.EventHandler(
                    this.MainForm_Load);
            this.ResumeLayout(false);
        }

        private void MainForm_Load(
            object sender, EventArgs e)
        { }
    }

    // ════════════════════════════════════════════
    // DARK THEME RENDERERS
    // ════════════════════════════════════════════

    class DarkMenuRenderer
        : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer()
            : base(new DarkColorTable()) { }

        protected override void
            OnRenderMenuItemBackground(
                ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                e.Graphics.FillRectangle(
                    new SolidBrush(
                        Color.FromArgb(
                            0, 120, 215)),
                    e.Item.ContentRectangle);
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }

    class DarkToolStripRenderer
        : ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer()
            : base(new DarkColorTable()) { }
    }

    class DarkColorTable
        : ProfessionalColorTable
    {
        public override Color MenuBorder =>
            Color.FromArgb(60, 60, 60);

        public override Color
            MenuItemSelectedGradientBegin =>
            Color.FromArgb(0, 120, 215);

        public override Color
            MenuItemSelectedGradientEnd =>
            Color.FromArgb(0, 120, 215);

        public override Color
            MenuItemPressedGradientBegin =>
            Color.FromArgb(0, 100, 190);

        public override Color
            MenuItemPressedGradientEnd =>
            Color.FromArgb(0, 100, 190);

        public override Color
            MenuStripGradientBegin =>
            Color.FromArgb(35, 35, 35);

        public override Color
            MenuStripGradientEnd =>
            Color.FromArgb(35, 35, 35);

        public override Color
            ToolStripDropDownBackground =>
            Color.FromArgb(40, 40, 40);

        public override Color
            ImageMarginGradientBegin =>
            Color.FromArgb(40, 40, 40);

        public override Color
            ImageMarginGradientMiddle =>
            Color.FromArgb(40, 40, 40);

        public override Color
            ImageMarginGradientEnd =>
            Color.FromArgb(40, 40, 40);

        public override Color SeparatorDark =>
            Color.FromArgb(70, 70, 70);

        public override Color SeparatorLight =>
            Color.FromArgb(70, 70, 70);
    }
}
