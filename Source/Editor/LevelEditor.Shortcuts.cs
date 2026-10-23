using Gum.Wireframe;

namespace Mario2.Editor
{
    // Keyboard shortcuts. Not called while a text field has focus.
    public sealed partial class LevelEditor
    {
        private const float KeyboardPanSpeed = 900f;
        private const float KeyboardZoomStep = 1.25f;

        private void HandleShortcuts()
        {
            bool mod = Shortcuts.IsModDown();
            bool shift = Shortcuts.IsShiftDown();

            if (mod)
            {
                HandleCommandShortcuts(shift);
                return;
            }

            // Other UI controls (a focused combo box, a button) keep keys like arrows and enter to themselves
            bool uiHasFocus = InteractiveGue.CurrentInputReceiver is not null;

            if (Shortcuts.Pressed(KeyboardKey.F5) || (!uiHasFocus && (Shortcuts.Pressed(KeyboardKey.Enter) || Shortcuts.Pressed(KeyboardKey.KpEnter))))
            {
                Playtest(fromCursor: shift);
                return;
            }

            if (Shortcuts.Pressed(KeyboardKey.Escape))
            {
                HandleEscape();
                return;
            }

            if (Shortcuts.Pressed(KeyboardKey.F1))
            {
                MenuBarView.ShowControls(this);
                return;
            }

            if (Viewport.IsBusy)
                return;

            foreach (Tool tool in Tools)
            {
                if (Shortcuts.Pressed(tool.Shortcut))
                    SetTool(tool);
            }

            if (Shortcuts.Pressed(KeyboardKey.G))
                EditorPrefs.ShowGrid = !EditorPrefs.ShowGrid;

            if (Shortcuts.Pressed(KeyboardKey.Home))
                FrameSpawn();

            if (uiHasFocus)
                return;

            if (Shortcuts.Pressed(KeyboardKey.Delete) || Shortcuts.Pressed(KeyboardKey.Backspace))
                DeleteSelection();

            HandleArrows(shift);
        }

        private void HandleCommandShortcuts(bool shift)
        {
            if (Shortcuts.Pressed(KeyboardKey.N)) NewLevel();
            else if (Shortcuts.Pressed(KeyboardKey.O)) OpenLevel();
            else if (Shortcuts.Pressed(KeyboardKey.S))
            {
                if (shift) SaveAs();
                else Save();
            }
            else if (Shortcuts.PressedOrRepeated(KeyboardKey.Z))
            {
                if (shift) Redo();
                else Undo();
            }
            else if (Shortcuts.PressedOrRepeated(KeyboardKey.Y)) Redo();
            else if (Shortcuts.Pressed(KeyboardKey.C)) CopySelection();
            else if (Shortcuts.Pressed(KeyboardKey.X)) CutSelection();
            else if (Shortcuts.Pressed(KeyboardKey.V)) Paste();
            else if (Shortcuts.Pressed(KeyboardKey.D)) DuplicateSelection();
            else if (Shortcuts.Pressed(KeyboardKey.A)) SelectAllEntities();
            else if (Shortcuts.Pressed(KeyboardKey.Zero)) FrameSpawn();
            else if (PaletteCategoryPressed() is { } category) Ui.Palette.ShowCategory(category);
            else if (Shortcuts.PressedOrRepeated(KeyboardKey.Equal) || Shortcuts.PressedOrRepeated(KeyboardKey.KpAdd)) ZoomIn();
            else if (Shortcuts.PressedOrRepeated(KeyboardKey.Minus) || Shortcuts.PressedOrRepeated(KeyboardKey.KpSubtract)) ZoomOut();
        }

        /// <summary>Cmd/Ctrl+1 to 8 pick the palette categories in order.</summary>
        private static PaletteCategory? PaletteCategoryPressed()
        {
            PaletteCategory[] categories = Enum.GetValues<PaletteCategory>();
            for (int i = 0; i < categories.Length && i < 9; i++)
            {
                if (Shortcuts.Pressed(KeyboardKey.One + i))
                    return categories[i];
            }

            return null;
        }

        public void ZoomIn() => Camera.ZoomCentered(KeyboardZoomStep);
        public void ZoomOut() => Camera.ZoomCentered(1f / KeyboardZoomStep);

        private void HandleEscape()
        {
            if (Ui.MenuBar.IsOpen)
            {
                Ui.MenuBar.CloseAll();
            }
            else if (Viewport.IsBusy)
            {
                Viewport.CancelGesture();
            }
            else if (InteractiveGue.CurrentInputReceiver is not null)
            {
                InteractiveGue.CurrentInputReceiver = null;
            }
            else if (!Selection.IsEmpty)
            {
                Selection.Clear();
            }
            else if (Brush is StampBrush { Source: null })
            {
                // Leave paste mode
                SetBrush(PaletteCatalog.Default.CreateBrush());
                Notify("Paste cancelled.");
            }
            else
            {
                ExitToTitle();
            }
        }

        private void HandleArrows(bool shift)
        {
            int dx = 0, dy = 0;
            if (Shortcuts.PressedOrRepeated(KeyboardKey.Left)) dx--;
            if (Shortcuts.PressedOrRepeated(KeyboardKey.Right)) dx++;
            if (Shortcuts.PressedOrRepeated(KeyboardKey.Up)) dy--;
            if (Shortcuts.PressedOrRepeated(KeyboardKey.Down)) dy++;

            if (Selection.HasMovables)
            {
                if (dx != 0 || dy != 0)
                {
                    int step = shift ? TilesetInfo.TileSizePx : (int)EditorPrefs.Snap;
                    Nudge(dx * step, dy * step);
                }

                return;
            }

            // Nothing to move: arrows pan the view
            Vector2 pan = Vector2.Zero;
            if (Raylib.IsKeyDown(KeyboardKey.Left)) pan.X--;
            if (Raylib.IsKeyDown(KeyboardKey.Right)) pan.X++;
            if (Raylib.IsKeyDown(KeyboardKey.Up)) pan.Y--;
            if (Raylib.IsKeyDown(KeyboardKey.Down)) pan.Y++;

            if (pan != Vector2.Zero)
                Camera.Pan(-pan * KeyboardPanSpeed * (shift ? 3f : 1f) * Time.DeltaTime);
        }

        public void Undo()
        {
            Viewport.CancelGesture();
            string? label = Document.UndoLabel;
            if (Document.Undo())
            {
                Selection.Prune(Document.Level);
                Notify($"Undid {label}.");
            }
        }

        public void Redo()
        {
            Viewport.CancelGesture();
            string? label = Document.RedoLabel;
            if (Document.Redo())
            {
                Selection.Prune(Document.Level);
                Notify($"Redid {label}.");
            }
        }
    }
}
