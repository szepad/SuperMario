using NativeFileDialogSharp;

namespace Mario2.Editor
{
    // New/open/save, leaving the editor and playtesting.
    public sealed partial class LevelEditor
    {
        private const string FileFilter = "lvl";

        public void NewLevel() => ConfirmDiscard(() =>
        {
            LoadDocument(new LevelData(), string.Empty);
            Notify("New level.");
        });

        public void OpenLevel()
        {
            DialogResult result = Dialog.FileOpen(FileFilter, DialogFolder());
            if (result.IsOk)
                ConfirmDiscard(() => OpenPath(result.Path));
        }

        /// <summary>Opens a level from File → Open Recent (asking to save the current one first).</summary>
        public void OpenRecent(string path) => ConfirmDiscard(() => OpenPath(path));

        public bool OpenPath(string path, bool quiet = false)
        {
            List<string> warnings = [];
            try
            {
                LevelData level = LevelLoader.ReadData(path, out bool wasLegacy, warnings);
                LoadDocument(level, path);
                EditorPrefs.AddRecent(path);

                if (wasLegacy)
                {
                    Document.MarkUnsaved();
                    Ui.Dialogs.Show("Level converted",
                        $"'{Path.GetFileName(path)}' was saved in the old level format and has been converted.\n" +
                        "Saving will write it in the new format.",
                        ("OK", null));
                }
                else if (warnings.Count > 0)
                {
                    Ui.Dialogs.Show("Level loaded with warnings", string.Join("\n", warnings.Take(8)), ("OK", null));
                }
                else
                {
                    Notify($"Opened {Path.GetFileName(path)}.");
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or LevelFormatException
                or FormatException or IndexOutOfRangeException or ArgumentException)
            {
                if (!quiet)
                    Ui.Dialogs.Show("Couldn't open level", $"'{Path.GetFileName(path)}' couldn't be opened.\n{ex.Message}", ("OK", null));

                return false;
            }
        }

        public bool Save()
        {
            if (Document.FilePath == string.Empty)
                return SaveAs();

            return SaveTo(Document.FilePath);
        }

        public bool SaveAs()
        {
            DialogResult result = Dialog.FileSave(FileFilter, DialogFolder());
            if (!result.IsOk)
                return false;

            string path = result.Path;
            if (!path.EndsWith(LevelFormat.Extension, StringComparison.OrdinalIgnoreCase))
                path += LevelFormat.Extension;

            return SaveTo(path);
        }

        /// <summary>
        /// Leaves the editor at the end of this frame. Switching scenes right away would unload
        /// the editor while its update is still running.
        /// </summary>
        public void ExitToTitle() => ConfirmDiscard(() => _exitRequested = true);

        /// <summary>
        /// Plays the level as it is right now, without saving it. Leaving the level (finishing,
        /// dying or pressing escape) comes back to the editor exactly as it was.
        /// </summary>
        public void Playtest(bool fromCursor)
        {
            if (Scene.SubScene is not null)
                return;

            LevelData level = Document.Level.Clone();
            if (fromCursor)
            {
                if (!Viewport.HasHoverCell)
                {
                    Notify("Hover the level where Mario should start, then press Shift+F5.");
                    return;
                }

                // Mario stands in the hovered tile
                const int Px = TilesetInfo.TileSizePx;
                level.Meta = level.Meta with
                {
                    SpawnX = Viewport.Hover.Col * Px + Px / 2,
                    SpawnY = (Viewport.Hover.Row + 1) * Px,
                    PipeIntro = false
                };
            }

            Viewport.CancelGesture();
            Document.Commit();

            string path = Document.FilePath == string.Empty ? LevelPaths.Unsaved : LevelPaths.Key(Document.FilePath);
            World world = LevelBuilder.Build(level, path);

            SessionManager.Reset();
            Scene.LoadSubScene(new GameScene(world));
        }

        private bool SaveTo(string path)
        {
            try
            {
                LevelFormat.Save(Document.Level, path);
                Document.FilePath = path;
                Document.MarkSaved();
                EditorPrefs.AddRecent(path);
                Notify($"Saved {Path.GetFileName(path)}.");
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Ui.Dialogs.Show("Couldn't save level", $"'{path}' couldn't be written.\n{ex.Message}", ("OK", null));
                return false;
            }
        }

        private void ConfirmDiscard(Action next)
        {
            Viewport.CancelGesture();
            Document.Commit();

            if (!Document.IsDirty)
            {
                next();
                return;
            }

            string name = Document.FilePath == string.Empty ? "this level" : $"'{Path.GetFileName(Document.FilePath)}'";
            Ui.Dialogs.Show("Unsaved changes", $"Do you want to save the changes to {name}?",
                ("Save", () =>
                {
                    if (Save())
                        next();
                }),
                ("Don't Save", next),
                ("Cancel", null));
        }

        private void LoadDocument(LevelData level, string path)
        {
            Viewport.CancelGesture();
            Document = new EditorDocument(level, path);
            Selection.Clear();
            Ghost.Clear();
            Preview.Sync(Document.Level, Document.Version);
            _frameOnNextUpdate = true;
        }

        private string DialogFolder()
        {
            string? folder = Document.FilePath != string.Empty ? Path.GetDirectoryName(Document.FilePath) : null;
            return folder is not null && Directory.Exists(folder) ? folder : AppPaths.DefaultLevelsFolder;
        }
    }
}
