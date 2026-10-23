using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;

namespace Mario2.Editor
{
    public sealed class ToolbarView
    {
        private const float BarHeight = 40f;
        private const float RowHeight = 26f;

        public ContainerRuntime Root { get; }

        private readonly LevelEditor _editor;
        private readonly Dictionary<Tool, ToggleButton> _toolButtons = [];
        private Color? _idleColor;
        private readonly ToggleButton _gridButton;
        private readonly ComboBox _snap;
        private readonly SnapMode[] _snapModes = [SnapMode.Tile, SnapMode.HalfTile, SnapMode.Pixel];
        private bool _updating;

        public ToolbarView(LevelEditor editor)
        {
            _editor = editor;

            Root = UiKit.Container();
            UiKit.FillWidth(Root);
            Root.Height = BarHeight;
            UiKit.Background(Root, Theme.Panel);
            UiKit.Line(Root, Anchor.Bottom);

            StackPanel row = UiKit.Row(4f);
            row.Anchor(Anchor.Left);
            row.X = 8;
            Root.AddChild(row);

            // Shortcuts live in the tooltips (and the menus), which keeps the bar narrow enough for a small window
            foreach (Tool tool in editor.Tools)
            {
                ToggleButton button = UiKit.Toggle(tool.Name, () => editor.SetTool(tool), tool.Icon);
                button.ToolTip = $"{tool.Name} ({tool.Shortcut})";
                _toolButtons[tool] = button;
                row.AddChild(button);
            }

            row.AddChild(Separator());

            _gridButton = UiKit.Toggle("Grid", () => EditorPrefs.ShowGrid = !EditorPrefs.ShowGrid, EditorIcon.Grid);
            _gridButton.ToolTip = "Show the grid (G)";
            row.AddChild(_gridButton);
            row.AddChild(Icon(EditorIcon.Snap));

            _snap = new ComboBox { Width = 120 };
            foreach (SnapMode mode in _snapModes)
                _snap.Items!.Add($"Snap: {(int)mode} px");

            _snap.SelectionChanged += (_, _) =>
            {
                if (!_updating && _snap.SelectedIndex >= 0)
                    EditorPrefs.Snap = _snapModes[_snap.SelectedIndex];
            };
            row.AddChild(_snap);

            row.AddChild(Separator());

            Button play = UiKit.Button("Play (F5)", () => editor.Playtest(fromCursor: false), EditorIcon.Play);
            play.ToolTip = "Play the level as it is now (F5 or Enter)";
            row.AddChild(play);

            Button playHere = UiKit.Button("Play Here", () => editor.Playtest(fromCursor: true), EditorIcon.PlayHere);
            playHere.ToolTip = "Play with Mario starting at the cursor (Shift+F5)";
            row.AddChild(playHere);
        }

        public void Refresh()
        {
            _updating = true;

            foreach ((Tool tool, ToggleButton button) in _toolButtons)
                SetActive(button, tool == _editor.Tool);

            SetActive(_gridButton, EditorPrefs.ShowGrid);

            int snapIndex = Array.IndexOf(_snapModes, EditorPrefs.Snap);
            if (_snap.SelectedIndex != snapIndex && !_snap.IsDropDownOpen)
                _snap.SelectedIndex = snapIndex;

            _updating = false;
        }

        /// <summary>The theme doesn't style checked toggle buttons, so tint the active ones ourselves.</summary>
        private void SetActive(ToggleButton button, bool active)
        {
            if (button.IsChecked != active)
                button.IsChecked = active;

            if (button.Visual is not ToggleButtonVisual visual)
                return;

            _idleColor ??= visual.BackgroundColor;
            Color color = active ? Theme.Selection : _idleColor.Value;
            if (!visual.BackgroundColor.Equals(color))
                visual.BackgroundColor = color;
        }

        /// <summary>A thin vertical line between groups of buttons.</summary>
        private static ContainerRuntime Separator()
        {
            ContainerRuntime box = UiKit.Container();
            box.Width = 17;
            box.Height = RowHeight;

            RectangleRuntime line = UiKit.Filled(Theme.Border);
            line.Anchor(Anchor.Center);
            line.Width = 1;
            line.Height = RowHeight - 6;
            box.AddChild(line);
            return box;
        }

        /// <summary>An icon on its own, centered in the row (labels a control without a text of its own).</summary>
        private static ContainerRuntime Icon(EditorIcon icon)
        {
            ContainerRuntime box = UiKit.Container();
            box.Width = EditorIcons.Size + 4;
            box.Height = RowHeight;

            SpriteRuntime sprite = EditorIcons.Sprite(icon);
            sprite.Anchor(Anchor.Center);
            box.AddChild(sprite);
            return box;
        }
    }
}
