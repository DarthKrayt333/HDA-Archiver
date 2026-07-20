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
        private List<HdaEntry> _entries
            = new List<HdaEntry>();
        private bool _isDirty;

        // ── Constructor ────────────────────────
        public MainForm(string openFile = null)
        {
            BuildUI();
            WireEvents();

            if (!string.IsNullOrEmpty(openFile))
            {
                // User double-clicked an .HDA
                // in Explorer → open it
                OpenHda(openFile);
            }
            else
            {
                // Fresh app launch → start with
                // a blank untitled archive so
                // user can immediately drag
                // files in without dialogs
                StartBlankArchive();
            }
        }

        // ══════════════════════════════════════
        // START BLANK ARCHIVE
        // Called when the app launches with no
        // file arg — creates an untitled empty
        // HDA in memory so user can immediately
        // drag files in. No dialogs.
        //
        // _currentHdaPath = null means "untitled"
        // User will be asked where to save only
        // when they hit File → Save.
        // ══════════════════════════════════════

        private void StartBlankArchive()
        {
            _currentHdaPath = null;   // untitled
            _entries.Clear();
            _isDirty = false;

            UpdateTitle();
            RefreshListView();
            ShowDropOverlay(true);

            SetStatus(
                "Ready — drop files here or use" +
                " Add Files to build your archive.");
        }

        // ══════════════════════════════════════
        // UI BUILDER
        // ══════════════════════════════════════

        private void BuildUI()
        {
            // ── Form basic settings ────────────
            Text = "HDA Archiver";
            Size = new Size(900, 600);
            MinimumSize = new Size(640, 400);
            StartPosition =
                FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(25, 25, 25);
            ForeColor = Color.White;
            AllowDrop = true;

            // ── CRITICAL for icon ──────────────
            ShowIcon = true;
            ShowInTaskbar = true;
            FormBorderStyle =
                FormBorderStyle.Sizable;

            // ── Load icon BEFORE other UI ──────
            SetFormIcon();

            // ── Menu bar ───────────────────────
            BuildMenuStrip();

            // ── Toolbar ────────────────────────
            BuildToolBar();

            // ── ListView ───────────────────────
            BuildListView();

            // ── Drop overlay ───────────────────
            BuildDropOverlay();

            // ── Status bar ─────────────────────
            BuildStatusBar();

            // ── Layout ─────────────────────────
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

                // Debug: List all resources
                // (look in View → Output window)
                foreach (string n in
                         asm.GetManifestResourceNames())
                {
                    System.Diagnostics.Debug
                          .WriteLine("RES: " + n);
                }

                // Auto-find any .ico in resources
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
                            Icon = new Icon(stream);
                            return;
                        }
                    }
                }

                // Try common paths
                string exeDir =
                    AppDomain.CurrentDomain
                             .BaseDirectory;

                string[] tryPaths = new string[]
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
                System.Diagnostics.Debug.WriteLine(
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

            // FILE menu
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

            var mnuSep1 =
                new ToolStripSeparator();

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
                    mnuClose, mnuSep1,
                    mnuExit
                });

            // ARCHIVE menu
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



            // TOOLS menu
            var mnuTools =
                new ToolStripMenuItem("Tools");

            var mnuSettings =
                new ToolStripMenuItem(
                    "Settings...",
                    null, OnSettings);

            mnuTools.DropDownItems.Add(
                mnuSettings);

            var mnuRegister =
                new ToolStripMenuItem(
                    "Register as .HDA Handler...",
                    null, OnRegisterHandler);

            mnuTools.DropDownItems.Add(mnuRegister);
            mnuTools.DropDownItems.Add(
                new ToolStripSeparator());
            mnuTools.DropDownItems.Add(mnuSettings);

            // HELP menu
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

        // ══════════════════════════════════════
        // REGISTER AS DEFAULT .HDA HANDLER
        //
        // Writes registry entries so Windows
        // opens .HDA files with this app when
        // double-clicked in Explorer. Requires
        // admin/user permission depending on
        // which hive you write to.
        //
        // Uses HKEY_CURRENT_USER so no admin
        // needed. Only affects current user.
        // ══════════════════════════════════════

        private void OnRegisterHandler(
            object sender, EventArgs e)
        {
            var r = MessageBox.Show(
                "Register HDA Archiver as the" +
                " default app for .HDA files?" +
                "\r\n\r\n" +
                "This lets you double-click .HDA" +
                " files in Windows Explorer to" +
                " open them with this app." +
                "\r\n\r\n" +
                "Only affects your user account." +
                " No admin required.",
                "HDA Archiver",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (r != DialogResult.Yes)
                return;

            try
            {
                string exePath =
                    System.Reflection.Assembly
                          .GetExecutingAssembly()
                          .Location;

                // Register file type in
                // HKEY_CURRENT_USER
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

            // Columns — NO slot column
            listView.Columns.Add(
                "Name", 320);
            listView.Columns.Add(
                "Type", 80);
            listView.Columns.Add(
                "Size", 100,
                HorizontalAlignment.Right);
            listView.Columns.Add(
                "Packed", 100,
                HorizontalAlignment.Right);
            listView.Columns.Add(
                "Ratio", 80,
                HorizontalAlignment.Right);
            listView.Columns.Add(
                "Status", 100);
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
                    "Drop files here to add them\r\n" +
                    "to your new HDA archive\r\n\r\n" +
                    "— or —\r\n\r\n" +
                    "Drop an .HDA file to open it\r\n\r\n" +
                    "File → Save to write to disk",
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
            // Form-level drag/drop
            DragEnter += OnFormDragEnter;
            DragDrop += OnFormDragDrop;

            // Drop overlay drag/drop
            dropOverlay.DragEnter += OnFormDragEnter;
            dropOverlay.DragDrop += OnFormDragDrop;
            dropLabel.AllowDrop = false;
            // ↑ Label swallows drops otherwise

            // ListView drag/drop
            listView.DragEnter +=
                OnListDragEnter;
            listView.DragDrop +=
                OnListDragDrop;

            // Drag files OUT of list
            listView.ItemDrag +=
                OnListViewItemDrag;

            // Right-click menu
            listView.MouseClick +=
                OnListViewMouseClick;

            // Delete key
            listView.KeyDown +=
                OnListViewKeyDown;

            // Double-click to open
            listView.DoubleClick +=
                OnListViewDoubleClick;

            // Form closing check
            FormClosing += OnFormClosing;
        }

        // ══════════════════════════════════════
        // DRAG OUT — Extract selected to temp
        // and start drag operation
        // ══════════════════════════════════════

        private void OnListViewItemDrag(
            object sender, ItemDragEventArgs e)
        {
            var selected = GetSelectedEntries();
            if (selected.Count == 0) return;

            try
            {
                // Create temp folder for drag
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
                    if (en.IsEmpty ||
                        en.Data == null)
                        continue;

                    string outPath = Path.Combine(
                        tmpDir, en.FileName);

                    File.WriteAllBytes(
                        outPath, en.Data);

                    filePaths.Add(outPath);
                }

                if (filePaths.Count == 0)
                    return;

                // Create DataObject with file paths
                var dataObj =
                    new DataObject(
                        DataFormats.FileDrop,
                        filePaths.ToArray());

                // Start the drag operation
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
        // Handles dropping .HDA files
        // onto the form or overlay
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

            // Ignore self-drags from drag-out
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

            // Accept ANY file type
            e.Effect = DragDropEffects.Copy;
        }

        private void OnFormDragDrop(
            object sender, DragEventArgs e)
        {
            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files == null || files.Length == 0)
                return;

            // ── ALWAYS add dropped files ───────
            // Never open .HDA files via drag &
            // drop — that would delete user's
            // work. If they want to open one
            // they use File → Open menu or
            // double-click in Explorer.
            //
            // This lets users add nested .HDA
            // files (e.g. HAYATO_02.HDA inside
            // HAYATO.HDA) which is a real thing
            // in the game.
            AddFilesToArchive(files);
        }

        // ══════════════════════════════════════
        // CREATE NEW ARCHIVE FROM FILES
        // Called when user drops non-HDA files
        // with no archive currently open. Asks
        // where to save the new archive and
        // adds the dropped files to it.
        // ══════════════════════════════════════

        private void CreateNewArchiveWithFiles(
            string[] files)
        {
            var dlg = new SaveFileDialog
            {
                Title =
                    "Save New HDA Archive As",
                Filter =
                    "HDA Archives (*.HDA)|*.HDA" +
                    "|All files (*.*)|*.*",
                DefaultExt = "HDA",
                FileName = "NEW.HDA"
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

            // Create new empty archive in memory
            _currentHdaPath = dlg.FileName;
            _entries.Clear();
            _isDirty = false;

            UpdateTitle();
            ShowDropOverlay(false);

            // Add the files
            AddFilesToArchive(files);

            SetStatus(
                "New archive created with " +
                files.Length + " file(s). " +
                "Use Save to write to disk.");
        }

        // ══════════════════════════════════════
        // DRAG & DROP — LISTVIEW LEVEL
        // Handles dropping regular files
        // INTO an open archive (add/replace)
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

            // Ignore self-drags from drag-out
            // (files we just extracted to temp
            // shouldn't be re-added by accident)
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

            // Accept everything else
            e.Effect = DragDropEffects.Copy;
        }

        private void OnListDragDrop(
            object sender, DragEventArgs e)
        {
            string[] files = (string[])
                e.Data.GetData(
                    DataFormats.FileDrop);

            if (files == null ||
                files.Length == 0)
                return;

            // ALWAYS add — never open via drop
            AddFilesToArchive(files);
        }

        // ══════════════════════════════════════
        // QUICK EXTRACT — Creates a folder
        // named after the .HDA file right
        // next to the .HDA and extracts
        // all files there instantly.
        //
        // Example:
        //   BOY.HDA → extracts to BOY\ folder
        //   in the same directory as BOY.HDA
        // ══════════════════════════════════════

        private void OnExtractToHdaFolder(
            object sender, EventArgs e)
        {
            if (_currentHdaPath == null)
            {
                ShowInfo("No archive is open.");
                return;
            }

            var realEntries = _entries
                .Where(en => !en.IsEmpty
                          && en.Data != null)
                .ToList();

            if (realEntries.Count == 0)
            {
                ShowInfo("Archive is empty.");
                return;
            }

            // Build folder path next to the HDA
            string hdaDir =
                Path.GetDirectoryName(
                    _currentHdaPath);

            string hdaName =
                Path.GetFileNameWithoutExtension(
                    _currentHdaPath);

            string outFolder =
                Path.Combine(hdaDir, hdaName);

            // If folder exists, ask user
            if (Directory.Exists(outFolder))
            {
                var r = MessageBox.Show(
                    string.Format(
                        "Folder already exists:\r\n" +
                        "{0}\r\n\r\n" +
                        "Overwrite files inside?",
                        outFolder),
                    "HDA Archiver",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                    return;
            }

            // Extract
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            int ok = 0;
            int failed = 0;

            foreach (var en in realEntries)
            {
                try
                {
                    string outPath =
                        Path.Combine(
                            outFolder,
                            en.FileName);

                    File.WriteAllBytes(
                        outPath, en.Data);
                    ok++;
                }
                catch
                {
                    failed++;
                }
            }

            // Status and notification
            string msg = string.Format(
                "Extracted {0} file(s) to:\r\n{1}",
                ok, outFolder);

            if (failed > 0)
                msg += string.Format(
                    "\r\n{0} file(s) failed.",
                    failed);

            SetStatus(
                "Extracted " + ok +
                " file(s) → " + outFolder);

            ShowInfo(msg);
        }

        // ══════════════════════════════════════
        // LISTVIEW EVENTS
        // ══════════════════════════════════════

        private void OnListViewMouseClick(
            object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (_entries.Count == 0)
                return;

            var ctx = new ContextMenuStrip
            {
                BackColor =
                    Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Renderer =
                    new DarkMenuRenderer()
            };

            bool hasSelection =
                listView.SelectedItems.Count > 0;
            bool singleSelection =
                listView.SelectedItems.Count == 1;

            // ── REPLACE (only for single sel.) ──
            if (singleSelection)
            {
                var mnuReplace =
                    new ToolStripMenuItem(
                        "Replace with File...",
                        null, OnReplaceFile);
                ctx.Items.Add(mnuReplace);

                var mnuRename =
                    new ToolStripMenuItem(
                        "Rename...",
                        null, OnRenameFile);
                ctx.Items.Add(mnuRename);

                ctx.Items.Add(
                    new ToolStripSeparator());
            }

            // ── EXTRACT ──
            if (hasSelection)
            {
                var mnuExtractSel =
                    new ToolStripMenuItem(
                        "Extract Selected...",
                        null, OnExtractSelected);
                ctx.Items.Add(mnuExtractSel);
            }

            var mnuExtractAll =
                new ToolStripMenuItem(
                    "Extract All...",
                    null, OnExtractAll);
            ctx.Items.Add(mnuExtractAll);

            var mnuExtractHere =
                new ToolStripMenuItem(
                    "Extract to HDA-Named Folder",
                    null, OnExtractToHdaFolder);
            ctx.Items.Add(mnuExtractHere);

            // ── REMOVE ──
            if (hasSelection)
            {
                ctx.Items.Add(
                    new ToolStripSeparator());

                var mnuRemove =
                    new ToolStripMenuItem(
                        "Remove Selected",
                        null, OnRemoveSelected);
                ctx.Items.Add(mnuRemove);
            }

            ctx.Show(listView, e.Location);
        }

        // ══════════════════════════════════════
        // REPLACE — Right-click → Replace with
        // browser dialog to pick new file
        // ══════════════════════════════════════

        private void OnReplaceFile(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count != 1)
                return;

            var entry =
                listView.SelectedItems[0].Tag
                as HdaEntry;

            if (entry == null || entry.IsEmpty)
                return;

            var dlg = new OpenFileDialog
            {
                Title = "Replace " + entry.FileName +
                        " with...",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() !=
                DialogResult.OK)
                return;

            try
            {
                byte[] newData =
                    File.ReadAllBytes(
                        dlg.FileName);

                // ── Update entry data ──
                entry.Data = newData;
                entry.DecompressedSize =
                    newData.Length;

                // ── Detect new extension ──
                string newExt =
                    DetectExt(newData);
                if (newExt == ".bin")
                    newExt = Path.GetExtension(
                        dlg.FileName);
                entry.Extension = newExt;

                // ── Recalc compression info ──
                if (AppSettings.Instance
                               .CompressedByDefault
                    && newData.Length > 64)
                {
                    byte[] comp =
                        HarvestCompression
                            .Compress(newData);
                    bool ok =
                        HarvestCompression
                            .VerifyRoundTrip(
                                newData, comp);

                    if (ok &&
                        comp.Length < newData.Length)
                    {
                        entry.StoredSize =
                            comp.Length;
                        entry.IsCompressed = true;
                    }
                    else
                    {
                        entry.StoredSize =
                            newData.Length;
                        entry.IsCompressed = false;
                    }
                }
                else
                {
                    entry.StoredSize =
                        newData.Length;
                    entry.IsCompressed = false;
                }

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

        // ══════════════════════════════════════
        // RENAME — Right-click → Rename entry
        // ══════════════════════════════════════

        private void OnRenameFile(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count != 1)
                return;

            var entry =
                listView.SelectedItems[0].Tag
                as HdaEntry;

            if (entry == null || entry.IsEmpty)
                return;

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

        // ══════════════════════════════════════
        // Simple input prompt dialog
        // ══════════════════════════════════════

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
                    DialogResult = DialogResult.OK,
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
                {
                    return txt.Text.Trim();
                }
            }

            return null;
        }

        private void OnListViewKeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                OnRemoveSelected(sender,
                                 EventArgs.Empty);
        }

        private void OnListViewDoubleClick(
            object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count == 0)
                return;

            var item =
                listView.SelectedItems[0];
            var entry =
                item.Tag as HdaEntry;

            if (entry == null ||
                entry.IsEmpty)
                return;

            // Extract to temp folder and
            // open with default app
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

            // Just start a fresh untitled archive
            // — no dialog. User picks save location
            // later when they hit Save.
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
                // Untitled → ask where to save
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

            // Start fresh untitled archive
            // instead of showing overlay only
            StartBlankArchive();
        }

        private void OnAddFiles(
            object sender, EventArgs e)
        {
            // No archive prompt needed anymore
            // — we always have a working archive
            // (untitled or saved)

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

            string folder =
                PickExtractFolder();
            if (folder == null) return;

            ExtractEntries(
                _entries.Where(
                    en => !en.IsEmpty).ToList(),
                folder);
        }

        private void OnExtractSelected(
            object sender, EventArgs e)
        {
            var selected = GetSelectedEntries();

            if (selected.Count == 0)
            {
                ShowInfo(
                    "No entries selected.");
                return;
            }

            string folder =
                PickExtractFolder();
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

            // ── Actually remove from list ──
            foreach (var entry in selected)
            {
                _entries.Remove(entry);
            }

            // ── Re-number slot indexes ──
            for (int i = 0; i < _entries.Count; i++)
                _entries[i].SlotIndex = i;

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
                "HMSTHModdingTool by DarthKrayt333\r\n\r\n" +
                "Supports:\r\n" +
                "  • Open / Create / Save .HDA archives\r\n" +
                "  • Smart Compression (V1/V2 LZO)\r\n" +
                "  • Drag & Drop files in/out\r\n" +
                "  • Auto file type detection\r\n" +
                "  • RDTB, GDTB, SRDB, ELF, audio\r\n\r\n" +
                "Windows XP / Vista / 7 / 8 / 10 / 11\r\n" +
                "32-bit and 64-bit compatible",
                "About HDA Archiver",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ══════════════════════════════════════
        // CORE LOGIC — OPEN HDA
        // ══════════════════════════════════════

        private void OpenHda(string path)
        {
            if (!File.Exists(path))
            {
                ShowError(
                    "File not found:\r\n" + path);
                return;
            }

            SetStatus("Opening " +
                Path.GetFileName(path) +
                " ...");

            try
            {
                _entries = LoadEntriesFromHda(
                    path);
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

                SetStatus(
                    "Opened: " +
                    Path.GetFileName(path) +
                    " — " +
                    _entries.Count(
                        en => !en.IsEmpty) +
                    " file(s)");
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
        // CORE LOGIC — LOAD ENTRIES FROM HDA
        // Reads the HDA in-memory and builds
        // the HdaEntry list without temp files
        // ══════════════════════════════════════

        private List<HdaEntry> LoadEntriesFromHda(
            string path)
        {
            var result = new List<HdaEntry>();
            string archiveName =
                Path.GetFileNameWithoutExtension(
                    path).ToUpper();

            using (var fs =
                new FileStream(
                    path, FileMode.Open,
                    FileAccess.Read))
            using (var br =
                new BinaryReader(fs))
            {
                // Read HDA header
                uint baseOffset =
                    br.ReadUInt32();

                // Validate
                if (baseOffset == 0 ||
                    baseOffset > 0x1000)
                    baseOffset = 0x10;

                fs.Seek(
                    baseOffset,
                    SeekOrigin.Begin);

                uint firstRel =
                    br.ReadUInt32();

                if (firstRel == 0)
                    return result;

                int maxSlots =
                    (int)(firstRel / 4);

                uint[] table =
                    new uint[maxSlots];
                table[0] = firstRel;

                for (int i = 1;
                     i < maxSlots; i++)
                    table[i] =
                        br.ReadUInt32();

                // Trim trailing zeros
                int lastReal = 0;
                for (int i = 0;
                     i < maxSlots; i++)
                    if (table[i] != 0)
                        lastReal = i;

                int tableSlots = lastReal + 1;
                int fileIndex = 0;

                // Pre-scan for HD file
                // (audio archive detection)
                bool archiveHasHD =
                    PeekForHDFile(
                        fs, br,
                        baseOffset,
                        table,
                        tableSlots);

                for (int i = 0;
                     i < tableSlots; i++)
                {
                    uint relOff = table[i];

                    if (relOff == 0)
                    {
                        result.Add(
                            new HdaEntry
                            {
                                SlotIndex = i,
                                FileName =
                                    "[EMPTY SLOT "
                                    + i + "]",
                                Extension = "",
                                IsEmpty = true
                            });
                        continue;
                    }

                    long absPos =
                        (long)baseOffset
                        + relOff;

                    if (absPos + 0x10
                        > fs.Length)
                        continue;

                    fs.Seek(
                        absPos,
                        SeekOrigin.Begin);

                    uint compFlag =
                        br.ReadUInt32();
                    uint decompSize =
                        br.ReadUInt32();
                    uint storedSize =
                        br.ReadUInt32();
                    br.ReadUInt32();

                    if (storedSize == 0 ||
                        absPos + 0x10 + (long)storedSize
                        > fs.Length)
                        continue;

                    byte[] raw =
                        br.ReadBytes(
                            (int)storedSize);

                    bool isComp =
                        (compFlag == 1);
                    byte[] data = raw;

                    if (isComp)
                    {
                        try
                        {
                            bool v2;
                            data =
                                HarvestCompression
                                    .Decompress(
                                        raw,
                                        (int)decompSize,
                                        out v2);
                        }
                        catch
                        {
                            data = raw;
                        }
                    }

                    string ext =
                        DetectExt(
                            data,
                            archiveHasHD);

                    string fileName;
                    if (ext == ".BD" ||
                        ext == ".HD" ||
                        ext == ".SQ")
                    {
                        fileName =
                            archiveName + ext;
                    }
                    else if (ext == ".HDA")
                    {
                        fileName = string.Format(
                            "{0}_{1:D2}{2}",
                            archiveName,
                            fileIndex,
                            ext);
                    }
                    else
                    {
                        fileName = string.Format(
                            "{0}_{1:D5}{2}",
                            archiveName,
                            fileIndex,
                            ext);
                    }

                    result.Add(new HdaEntry
                    {
                        SlotIndex = i,
                        FileName = fileName,
                        Extension = ext,
                        DecompressedSize =
                            data.Length,
                        StoredSize = storedSize,
                        IsCompressed = isComp,
                        IsEmpty = false,
                        Data = data
                    });

                    fileIndex++;
                }
            }

            return result;
        }

        // ══════════════════════════════════════
        // CORE LOGIC — SAVE HDA
        // ══════════════════════════════════════

        private void SaveHda(string path)
        {
            bool compress =
                AppSettings.Instance
                           .CompressedByDefault;

            SetStatus("Saving " +
                Path.GetFileName(path) +
                " ...");

            try
            {
                using (var fs =
                    new FileStream(
                        path, FileMode.Create))
                using (var bw =
                    new BinaryWriter(fs))
                {
                    int totalSlots =
                        _entries.Count;

                    // Phase 1: compress
                    byte[][] rawArr =
                        new byte[totalSlots][];
                    byte[][] storedArr =
                        new byte[totalSlots][];
                    bool[] compFlags =
                        new bool[totalSlots];

                    for (int i = 0;
                         i < totalSlots; i++)
                    {
                        var en = _entries[i];

                        if (en.IsEmpty ||
                            en.Data == null)
                        {
                            rawArr[i] = null;
                            storedArr[i] = null;
                            compFlags[i] = false;
                            continue;
                        }

                        rawArr[i] = en.Data;

                        if (!compress ||
                            en.Data.Length <= 64)
                        {
                            storedArr[i] =
                                en.Data;
                            compFlags[i] = false;
                        }
                        else
                        {
                            byte[] comp =
                                HarvestCompression
                                    .Compress(
                                        en.Data);

                            bool ok =
                                HarvestCompression
                                    .VerifyRoundTrip(
                                        en.Data,
                                        comp);

                            if (!ok ||
                                comp.Length >=
                                en.Data.Length)
                            {
                                storedArr[i] =
                                    en.Data;
                                compFlags[i] =
                                    false;
                            }
                            else
                            {
                                storedArr[i] =
                                    comp;
                                compFlags[i] =
                                    true;
                            }
                        }
                    }

                    // Phase 2: offsets
                    int tableSize =
                        totalSlots * 4;
                    int dataStart =
                        Align(tableSize);

                    uint[] offsets =
                        new uint[totalSlots];
                    int cursor = dataStart;

                    for (int i = 0;
                         i < totalSlots; i++)
                    {
                        if (storedArr[i] == null)
                        {
                            offsets[i] = 0;
                        }
                        else
                        {
                            offsets[i] =
                                (uint)cursor;
                            cursor += Align(
                                0x10 +
                                storedArr[i]
                                    .Length);
                        }
                    }

                    // Phase 3: write
                    bw.Write(0x10u);
                    bw.Write(0u);
                    bw.Write(0u);
                    bw.Write(0u);

                    fs.Seek(
                        0x10,
                        SeekOrigin.Begin);

                    for (int i = 0;
                         i < totalSlots; i++)
                        bw.Write(offsets[i]);

                    for (int i = 0;
                         i < totalSlots; i++)
                    {
                        if (storedArr[i] == null)
                            continue;

                        long abs =
                            0x10L + offsets[i];
                        fs.Seek(
                            abs,
                            SeekOrigin.Begin);

                        bw.Write(
                            compFlags[i]
                                ? 1u : 0u);
                        bw.Write(
                            (uint)rawArr[i]
                                .Length);
                        bw.Write(
                            (uint)storedArr[i]
                                .Length);
                        bw.Write(0u);

                        fs.Write(
                            storedArr[i],
                            0,
                            storedArr[i].Length);

                        while ((fs.Position
                                & 0xF) != 0)
                            fs.WriteByte(0);
                    }
                }

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
        //
        // Accepts ANY file format:
        //   • Game files (.rdtb .gdtb .srdb)
        //   • Nested archives (.HDA)
        //   • Audio (.BD .HD .SQ)
        //   • Any other format (.jpg .png .txt
        //     .mp3 .zip whatever the user wants)
        //
        // Compression is attempted on all files
        // that are large enough (> 64 bytes).
        // Already-compressed formats like .jpg
        // will store RAW since compression makes
        // them larger — that's expected behavior.
        // ══════════════════════════════════════

        private void AddFilesToArchive(
            string[] filePaths)
        {
            int added = 0;
            int replaced = 0;

            foreach (string fp in filePaths)
            {
                if (!File.Exists(fp))
                    continue;

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

                // ── Preserve user's original
                // filename exactly as-is. Do NOT
                // override the extension. Users
                // may add any file format they
                // want (jpg, png, txt, nested
                // HDA, whatever).
                string fname =
                    Path.GetFileName(fp);
                string ext =
                    Path.GetExtension(fp);

                // ── Try to find existing entry
                // with same filename → replace ──
                var existing =
                    _entries.FirstOrDefault(
                        en =>
                        string.Equals(
                            en.FileName, fname,
                            StringComparison
                                .OrdinalIgnoreCase)
                        && !en.IsEmpty);

                if (existing != null)
                {
                    // ── REPLACE existing entry ──
                    existing.Data = data;
                    existing.Extension = ext;
                    existing.DecompressedSize =
                        data.Length;

                    CalculateCompression(
                        data,
                        out long storedSize,
                        out bool isCompressed);

                    existing.StoredSize = storedSize;
                    existing.IsCompressed =
                        isCompressed;
                    existing.IsEmpty = false;
                    replaced++;
                }
                else
                {
                    // ── NEW entry ───────────────
                    CalculateCompression(
                        data,
                        out long storedSize,
                        out bool isCompressed);

                    // Try to reuse empty slot
                    var emptySlot =
                        _entries.FirstOrDefault(
                            en => en.IsEmpty);

                    if (emptySlot != null)
                    {
                        emptySlot.Data = data;
                        emptySlot.FileName = fname;
                        emptySlot.Extension = ext;
                        emptySlot.DecompressedSize =
                            data.Length;
                        emptySlot.StoredSize =
                            storedSize;
                        emptySlot.IsCompressed =
                            isCompressed;
                        emptySlot.IsEmpty = false;
                    }
                    else
                    {
                        _entries.Add(
                            new HdaEntry
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
                                IsEmpty = false
                            });
                    }

                    added++;
                }
            }

            if (added > 0 || replaced > 0)
            {
                _isDirty = true;

                // Hide overlay now that archive
                // has content
                ShowDropOverlay(false);

                // Immediate visual refresh
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
        // CORE LOGIC — EXTRACT
        // ══════════════════════════════════════

        private void ExtractEntries(
            List<HdaEntry> entries,
            string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(
                    folder);

            int ok = 0;
            int failed = 0;

            foreach (var en in entries)
            {
                if (en.IsEmpty ||
                    en.Data == null)
                    continue;

                try
                {
                    string outPath =
                        Path.Combine(
                            folder,
                            en.FileName);

                    File.WriteAllBytes(
                        outPath, en.Data);
                    ok++;
                }
                catch
                {
                    failed++;
                }
            }

            string msg = string.Format(
                "Extracted {0} file(s)" +
                " to:\r\n{1}",
                ok, folder);

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
        // ══════════════════════════════════════

        private void RefreshListView()
        {
            listView.BeginUpdate();
            listView.Items.Clear();

            int rowIdx = 0;
            foreach (var en in _entries)
            {
                if (en.IsEmpty) continue;

                var item = new ListViewItem(
                    en.FileName);

                // Show extension in uppercase
                // without the dot. Works for
                // ANY format: JPG, PNG, HDA,
                // RDTB, GDTB, BIN, TXT, etc.
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
                        ? Color.FromArgb(30, 30, 30)
                        : Color.FromArgb(38, 38, 38);

                item.Tag = en;
                listView.Items.Add(item);
                rowIdx++;
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
                if (en != null &&
                    !en.IsEmpty)
                    list.Add(en);
            }
            return list;
        }

        private string PickExtractFolder()
        {
            var dlg =
                new FolderBrowserDialog
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
                // Untitled archive
                Text = "HDA Archiver — Untitled" +
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

            int real = _entries.Count(
                en => !en.IsEmpty);

            long totalRaw = _entries
                .Where(en => !en.IsEmpty)
                .Sum(en => en.DecompressedSize);

            long totalStored = _entries
                .Where(en => !en.IsEmpty)
                .Sum(en => en.StoredSize);

            statusRight.Text = string.Format(
                "{0} file(s)  Raw: {1}  Packed: {2}",
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
                    bytes /
                    (1024.0 * 1024.0));

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
        // EXTENSION DETECTION
        // (mirrors HarvestDataArchive logic
        //  but works on byte[] directly)
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
            { 0x49, 0x45, 0x43, 0x53,
              0x73, 0x72, 0x65, 0x56 };
        private static readonly byte[] MagicSq2 =
            { 0x49, 0x45, 0x43, 0x53,
              0x75, 0x71, 0x65, 0x53 };

        private static string DetectExt(
            byte[] d, bool hdPresent = false)
        {
            if (d == null ||
                d.Length < 4)
                return ".bin";

            if (SW(d, MagicGdtb)) return ".gdtb";
            if (SW(d, MagicRdtb)) return ".rdtb";
            if (SW(d, MagicSrdb)) return ".srdb";
            if (SW(d, MagicElf)) return ".elf";

            if (d.Length >= 16 &&
                SW(d, MagicHda)) return ".HDA";

            if (IsSQ(d)) return ".SQ";
            if (IsHD(d)) return ".HD";

            if (IsBD(d)) return ".BD";

            if (hdPresent &&
                IsLikelyBD(d)) return ".BD";

            return ".bin";
        }

        private static bool SW(
            byte[] d, byte[] m)
        {
            if (d.Length < m.Length)
                return false;
            for (int i = 0; i < m.Length; i++)
                if (d[i] != m[i]) return false;
            return true;
        }

        private static bool IsHD(byte[] d)
        {
            if (d == null ||
                d.Length < 8) return false;
            if (!SW(d, MagicHd)) return false;
            if (IsSQ(d)) return false;
            return true;
        }

        private static bool IsSQ(byte[] d)
        {
            if (d == null ||
                d.Length < 0x18) return false;
            if (!SW(d, MagicHd)) return false;
            for (int i = 0;
                 i < MagicSq2.Length; i++)
                if (d[0x10 + i] != MagicSq2[i])
                    return false;
            return true;
        }

        private static bool IsBD(byte[] d)
        {
            if (d == null ||
                d.Length < 256) return false;
            if ((d.Length & 0xF) != 0)
                return false;

            int tot = d.Length / 16;
            int vf = 0, vp = 0, samp = 0;
            int step = Math.Max(1, tot / 500);

            for (int b = 0; b < tot; b += step)
            {
                int off = b * 16;
                byte pred = d[off];
                if ((pred >> 4) <= 4 &&
                    (pred & 0xF) <= 12) vp++;
                if (d[off + 1] <= 0x07) vf++;
                samp++;
            }

            if (samp == 0) return false;

            return ((double)vf / samp) >= 0.85
                && ((double)vp / samp) >= 0.85;
        }

        private static bool IsLikelyBD(
            byte[] d)
        {
            if (d == null ||
                d.Length < 32) return false;
            if ((d.Length & 0xF) != 0)
                return false;

            int tot = d.Length / 16;
            int vf = 0, samp = 0;
            int step = Math.Max(1, tot / 200);

            for (int b = 0; b < tot; b += step)
            {
                if (d[b * 16 + 1] <= 0x07) vf++;
                samp++;
            }

            return samp > 0 &&
                   ((double)vf / samp) >= 0.50;
        }

        // ══════════════════════════════════════
        // PEEK FOR HD FILE — pre-scan helper
        // ══════════════════════════════════════

        private bool PeekForHDFile(
            FileStream fs,
            BinaryReader br,
            uint baseOffset,
            uint[] table,
            int tableSlots)
        {
            long savedPos = fs.Position;

            try
            {
                for (int j = 0;
                     j < tableSlots; j++)
                {
                    uint off = table[j];
                    if (off == 0) continue;

                    long abs =
                        (long)baseOffset + off;
                    if (abs + 0x10 > fs.Length)
                        continue;

                    fs.Seek(
                        abs, SeekOrigin.Begin);

                    uint comp = br.ReadUInt32();
                    uint decomp = br.ReadUInt32();
                    uint stored = br.ReadUInt32();
                    br.ReadUInt32();

                    if (stored == 0) continue;

                    int sniff = (int)Math.Min(
                        64u, stored);
                    byte[] buf = br.ReadBytes(
                        sniff);

                    if (IsHD(buf)) return true;

                    if (comp == 1)
                    {
                        try
                        {
                            fs.Seek(
                                abs + 0x10,
                                SeekOrigin.Begin);
                            byte[] full =
                                br.ReadBytes(
                                    (int)stored);
                            bool v2;
                            byte[] dec =
                                HarvestCompression
                                    .Decompress(
                                        full,
                                        (int)decomp,
                                        out v2);
                            if (IsHD(dec))
                                return true;
                        }
                        catch { }
                    }
                }
            }
            finally
            {
                fs.Seek(
                    savedPos,
                    SeekOrigin.Begin);
            }

            return false;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        // ══════════════════════════════════════
        // COMPRESSION HELPER
        //
        // Central method to decide whether to
        // compress a file and calculate its
        // stored size. Uses the game's default
        // smart-compression behavior:
        //
        //   • Files <= 64 bytes → stored RAW
        //     (game engine does the same)
        //
        //   • Files > 64 bytes → try compress,
        //     use compressed version only if
        //     it's actually smaller than raw
        //     (matches original HMSTH behavior)
        //
        //   • Files that don't shrink (like
        //     .jpg .png .mp3 which are already
        //     compressed) → stored RAW
        //
        // This matches how the original game
        // packs its .HDA files for best in-game
        // load performance.
        // ══════════════════════════════════════

        private void CalculateCompression(
            byte[] data,
            out long storedSize,
            out bool isCompressed)
        {
            // Default: store raw
            storedSize = data.Length;
            isCompressed = false;

            // ── Compression disabled? ──────────
            if (!AppSettings.Instance
                            .CompressedByDefault)
                return;

            // ── Too small to bother? ───────────
            // Game engine skips compression on
            // files <= 64 bytes since the LZO
            // overhead makes them bigger.
            if (data.Length <= 64)
                return;

            // ── Attempt compression ────────────
            byte[] comp =
                HarvestCompression.Compress(data);

            bool verified =
                HarvestCompression.VerifyRoundTrip(
                    data, comp);

            if (!verified)
            {
                // Compression is broken — never
                // store bad data, always fall
                // back to raw
                return;
            }

            // ── Use compressed only if smaller
            // (game-accurate behavior — matches
            // original HMSTH .HDA files)
            if (comp.Length < data.Length)
            {
                storedSize = comp.Length;
                isCompressed = true;
            }
            // else: keep defaults (raw)
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
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

        public override Color MenuStripGradientBegin =>
            Color.FromArgb(35, 35, 35);

        public override Color MenuStripGradientEnd =>
            Color.FromArgb(35, 35, 35);

        public override Color
            ToolStripDropDownBackground =>
            Color.FromArgb(40, 40, 40);

        public override Color ImageMarginGradientBegin =>
            Color.FromArgb(40, 40, 40);

        public override Color ImageMarginGradientMiddle =>
            Color.FromArgb(40, 40, 40);

        public override Color ImageMarginGradientEnd =>
            Color.FromArgb(40, 40, 40);

        public override Color SeparatorDark =>
            Color.FromArgb(70, 70, 70);

        public override Color SeparatorLight =>
            Color.FromArgb(70, 70, 70);
    }
}