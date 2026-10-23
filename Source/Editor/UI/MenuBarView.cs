using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;

namespace Mario2.Editor
{
    public sealed class MenuBarView
    {
        public Menu Menu { get; } = new();
        public bool IsOpen => Menu.MenuItems.Any(item => item.IsPopupVisible);

        // Space around a row's text, and between the label and the shortcut column
        private const float RowPadding = 10f;
        private const float ColumnGap = 36f;
        private const float SubmenuArrowWidth = 10f;

        private readonly List<Action> _refreshers = [];

        // The rows of each popup, so they can share one width and line their shortcuts up on the right
        private readonly Dictionary<MenuItem, List<(MenuItem Item, string? Right)>> _popups = [];
        private readonly LevelEditor _editor;
        private readonly MenuItem _recent;
        private int _recentVersion = -1;

        public MenuBarView(LevelEditor editor)
        {
            _editor = editor;

            MenuItem file = Top("File");
            Item(file, "New", Shortcuts.WithMod("N"), editor.NewLevel);
            Item(file, "Open...", Shortcuts.WithMod("O"), editor.OpenLevel);
            _recent = Submenu(file, "Open Recent");
            Item(file, "Save", Shortcuts.WithMod("S"), () => editor.Save());
            Item(file, "Save As...", Shortcuts.WithMod("Shift+S"), () => editor.SaveAs());
            Item(file, "Exit to Title", "Esc", editor.ExitToTitle);

            MenuItem edit = Top("Edit");
            Item(edit, () => editor.Document.UndoLabel is { } u ? $"Undo {u}" : "Undo", Shortcuts.WithMod("Z"), editor.Undo,
                () => editor.Document.CanUndo);
            Item(edit, () => editor.Document.RedoLabel is { } r ? $"Redo {r}" : "Redo", Shortcuts.WithMod("Shift+Z"), editor.Redo,
                () => editor.Document.CanRedo);
            Item(edit, "Cut", Shortcuts.WithMod("X"), editor.CutSelection);
            Item(edit, "Copy", Shortcuts.WithMod("C"), editor.CopySelection);
            Item(edit, "Paste", Shortcuts.WithMod("V"), editor.Paste, () => editor.Clipboard is not null);
            Item(edit, "Duplicate", Shortcuts.WithMod("D"), editor.DuplicateSelection);
            Item(edit, "Delete", "Del", editor.DeleteSelection);
            Item(edit, "Select All Entities", Shortcuts.WithMod("A"), editor.SelectAllEntities);

            MenuItem view = Top("View");
            Check(view, "Grid", "G", () => EditorPrefs.ShowGrid, v => EditorPrefs.ShowGrid = v);
            Check(view, "Screen Guides", null, () => EditorPrefs.ShowScreenGuides, v => EditorPrefs.ShowScreenGuides = v);
            Check(view, "Background", null, () => EditorPrefs.ShowBackground, v => EditorPrefs.ShowBackground = v);
            Check(view, "Collision Overlay", null, () => EditorPrefs.ShowCollision, v => EditorPrefs.ShowCollision = v);
            Check(view, "Animate Sprites", null, () => EditorPrefs.AnimateSprites, v => EditorPrefs.AnimateSprites = v);
            Item(view, "Zoom In", Shortcuts.WithMod("+"), editor.ZoomIn);
            Item(view, "Zoom Out", Shortcuts.WithMod("-"), editor.ZoomOut);
            Item(view, "Frame Player Start", "Home", editor.FrameSpawn);

            MenuItem palette = Submenu(view, "Palette");
            PaletteCategory[] categories = Enum.GetValues<PaletteCategory>();
            for (int i = 0; i < categories.Length; i++)
            {
                PaletteCategory category = categories[i];
                Item(palette, PaletteCatalog.DisplayName(category), Shortcuts.WithMod((i + 1).ToString()),
                    () => editor.Ui.Palette.ShowCategory(category));
            }

            MenuItem level = Top("Level");
            Item(level, "Play", "F5 / Enter", () => editor.Playtest(fromCursor: false));
            Item(level, "Play from Cursor", "Shift+F5", () => editor.Playtest(fromCursor: true));
            Item(level, "Insert Column", null, () => editor.InsertColumns(1));
            Item(level, "Insert Screen (16 Columns)", null, () => editor.InsertColumns(16));
            Item(level, "Delete Column", null, () => editor.DeleteColumns(1));
            Item(level, "Delete Screen (16 Columns)", null, () => editor.DeleteColumns(16));

            MenuItem help = Top("Help");
            Item(help, "Controls...", "F1", () => ShowControls(editor));
        }

        public void Refresh()
        {
            foreach (Action refresh in _refreshers)
                refresh();

            RefreshRecent();
            AlignColumns();
        }

        /// <summary>
        /// Gives every row of a popup the same width (labels change, e.g. "Undo Paint Brick"), so the
        /// right-anchored shortcuts and submenu arrows form one column.
        /// </summary>
        private void AlignColumns()
        {
            foreach ((MenuItem popup, List<(MenuItem Item, string? Right)> rows) in _popups)
            {
                if (rows.Count == 0)
                    continue;

                float left = rows.Max(r => UiKit.TextWidth(r.Item.Header ?? string.Empty));
                float right = rows.Max(r => r.Right is { } text ? UiKit.TextWidth(text)
                    : r.Item.Items!.Count > 0 ? SubmenuArrowWidth : 0f);

                float width = MathF.Ceiling(RowPadding * 2 + left + (right > 0f ? ColumnGap + right : 0f));
                foreach ((MenuItem item, _) in rows)
                {
                    if (item.Visual.Width != width)
                        item.Visual.Width = width;
                }
            }
        }

        /// <summary>Rebuilds File → Open Recent, only when the list has changed.</summary>
        private void RefreshRecent()
        {
            if (_recentVersion == EditorPrefs.RecentVersion)
                return;

            _recentVersion = EditorPrefs.RecentVersion;
            _recent.Items!.Clear();
            _popups[_recent] = [];

            foreach (string path in EditorPrefs.RecentFiles)
            {
                string folder = Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty;
                MenuItem item = Row(_recent, Path.GetFileName(path), $"({folder})");
                item.IsEnabled = File.Exists(path);
                item.Clicked += (_, _) =>
                {
                    CloseAll();
                    _editor.OpenRecent(path);
                };
            }

            if (EditorPrefs.RecentFiles.Count == 0)
            {
                Row(_recent, "No recent files", null).IsEnabled = false;
                return;
            }

            MenuItem clear = Row(_recent, "Clear Recent", null);
            clear.Clicked += (_, _) =>
            {
                CloseAll();
                EditorPrefs.ClearRecent();
            };
        }

        public static void ShowControls(LevelEditor editor)
        {
            string mod = Shortcuts.Mod;
            editor.Ui.Dialogs.Show("Controls",
                "Tools: V Select, B Brush, R Rectangle, E Erase, I Pick\n" +
                "Paint: left mouse   Erase: right mouse   Pick: Alt+click\n" +
                $"Palette categories: {mod}+1 (Terrain) to {mod}+8 (Tileset)\n" +
                "Pan: two-finger scroll, middle mouse, Space+drag or arrow keys\n" +
                $"Zoom: pinch, {mod}+scroll, mouse wheel or {mod}+plus / minus   Sideways: Shift+wheel\n" +
                "Move selection: drag or arrow keys (Shift = one tile)\n" +
                $"Playtest: F5 or Enter   From the cursor: Shift+F5\n" +
                $"Undo/Redo: {mod}+Z / {mod}+Shift+Z   Copy/Cut/Paste: {mod}+C / X / V\n" +
                $"Duplicate: {mod}+D   Delete: Del   Grid: G   Frame start: Home",
                ("Close", null));
        }

        private MenuItem Top(string header)
        {
            MenuItem item = new() { Header = $"   {header}   " };
            Menu.Items!.Add(item);
            return item;
        }

        /// <summary>A menu item that opens its own list of items.</summary>
        private MenuItem Submenu(MenuItem parent, string header)
        {
            MenuItem item = Row(parent, header, null);

            // Gum puts the arrow right after the text; move it to the right column
            if (item.Visual.GetGraphicalUiElementByName("SubmenuIndicatorInstance") is { } arrow)
            {
                arrow.Parent = item.Visual;
                arrow.Anchor(Anchor.Right);
                arrow.X = -RowPadding;
            }

            return item;
        }

        /// <summary>
        /// A row of a popup: the label on the left, and an optional muted text (a shortcut) aligned to the
        /// right edge. The row's width is set by <see cref="AlignColumns"/>.
        /// </summary>
        private MenuItem Row(MenuItem parent, string label, string? right)
        {
            MenuItem item = new() { Header = label };
            item.Visual.WidthUnits = DimensionUnitType.Absolute;
            parent.Items!.Add(item);

            if (right is not null)
            {
                Label text = UiKit.Label(right, Theme.TextMuted);
                text.Anchor(Anchor.Right);
                text.X = -RowPadding;
                text.Visual.HasEvents = false;
                item.Visual.AddChild(text.Visual);
            }

            if (!_popups.TryGetValue(parent, out List<(MenuItem, string?)>? rows))
                _popups[parent] = rows = [];

            rows.Add((item, right));
            return item;
        }

        private void Item(MenuItem parent, string label, string? shortcut, Action action, Func<bool>? isEnabled = null)
            => Item(parent, () => label, shortcut, action, isEnabled);

        private void Item(MenuItem parent, Func<string> label, string? shortcut, Action action, Func<bool>? isEnabled = null)
        {
            MenuItem item = Row(parent, label(), shortcut);
            item.Clicked += (_, _) =>
            {
                CloseAll();
                action();
            };

            _refreshers.Add(() =>
            {
                string header = label();
                if (item.Header != header)
                    item.Header = header;

                bool enabled = isEnabled?.Invoke() ?? true;
                if (item.IsEnabled != enabled)
                    item.IsEnabled = enabled;
            });
        }

        private void Check(MenuItem parent, string label, string? shortcut, Func<bool> get, Action<bool> set)
            => Item(parent, () => (get() ? "[x] " : "[  ] ") + label, shortcut, () => set(!get()));

        public void CloseAll()
        {
            foreach (MenuItem item in Menu.MenuItems)
                item.HidePopupRecursively();

            InteractiveGue.CurrentInputReceiver = null;
        }
    }
}
