using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;

namespace Mario2.Editor
{
    public sealed class StatusBarView
    {
        private const float BarHeight = 26f;
        private const float Margin = 10f;

        public ContainerRuntime Root { get; }

        private readonly LevelEditor _editor;
        private readonly Label _message;
        private readonly Label _info;

        public StatusBarView(LevelEditor editor)
        {
            _editor = editor;

            Root = UiKit.Container();
            UiKit.FillWidth(Root);
            Root.Height = BarHeight;
            UiKit.Background(Root, Theme.StatusBar);

            _message = UiKit.Label(string.Empty);
            _message.Anchor(Anchor.Left);
            _message.X = Margin;
            Root.AddChild(_message);

            // A right-anchored label that sizes itself to its text is positioned from its previous
            // width on the frame the text changes, so it jumped around while the cursor moved.
            // Give it a fixed box (the bar minus margins) and let the text right-align inside it.
            _info = UiKit.Label(string.Empty, Theme.StatusBarMuted);
            _info.Anchor(Anchor.Right);
            _info.X = -Margin;
            UiKit.FillWidth(_info, Margin * 2);
            Root.AddChild(_info);
        }

        public void Refresh()
        {
            bool hasMessage = _editor.StatusMessage != string.Empty;
            UiKit.SetText(_message, hasMessage ? _editor.StatusMessage : _editor.Tool.Hint);
            UiKit.SetColor(_message, hasMessage ? Theme.Text : Theme.StatusBarMuted);
            UiKit.SetText(_info, Describe());
        }

        private string Describe()
        {
            // 100% is the size the level has in game
            string zoom = $"Zoom {MathF.Round(_editor.Camera.Zoom * 100f)}%";
            EditorViewport viewport = _editor.Viewport;
            if (!viewport.HasHoverCell)
                return zoom;

            ToolInput hover = viewport.Hover;
            Vector2 px = hover.Pixels;
            string what = _editor.Preview.SpawnHit(hover.World) ? "Player Start"
                : _editor.Preview.HitTest(hover.World) is int id && _editor.Document.Level.FindEntity(id)?.Def is { } def ? def.Name
                : TileNames.Describe(_editor.Document.Level.Get(hover.Col, hover.Row));

            return $"{what}   |   Tile {hover.Col}, {hover.Row}   |   Px {(int)px.X}, {(int)px.Y}   |   {zoom}";
        }
    }
}
