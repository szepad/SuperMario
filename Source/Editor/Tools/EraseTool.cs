namespace Mario2.Editor
{
    public sealed class EraseTool(LevelEditor editor) : Tool(editor)
    {
        private readonly EraseGesture _erase = new(editor);

        public override string Name => "Erase";
        public override KeyboardKey Shortcut => KeyboardKey.E;
        public override EditorIcon Icon => EditorIcon.Erase;
        public override string Hint => "Drag over entities or tiles to erase them";
        public override bool IsBusy => _erase.IsActive;

        public override void OnPress(ToolInput input, MouseButton button) => _erase.Begin(input);
        public override void OnDrag(ToolInput input) => _erase.Drag(input);
        public override void OnRelease(ToolInput input) => _erase.End();
        public override void Cancel() => _erase.Cancel();

        public override void DrawOverlay(ToolInput hover, bool isHovered)
        {
            if (!isHovered || _erase.IsActive)
                return;

            Rectangle area = Editor.Preview.HitTest(hover.World) is int id && Editor.Preview.RootOf(id) is { } entity
                ? entity.Rect
                : new TileRect(hover.Col, hover.Row, 1, 1).ToWorld();

            Raylib.DrawRectangleRec(area, Theme.EraseFill);
            Editor.Viewport.DrawOutline(area, Theme.Danger);
        }
    }

    public sealed class PickTool(LevelEditor editor) : Tool(editor)
    {
        public override string Name => "Pick";
        public override KeyboardKey Shortcut => KeyboardKey.I;
        public override EditorIcon Icon => EditorIcon.Pick;
        public override string Hint => "Click a tile or entity to paint with it";

        public override void OnPress(ToolInput input, MouseButton button)
        {
            if (button == MouseButton.Left && Editor.PickAt(input))
                Editor.SetTool(Editor.BrushTool);
        }

        public override void DrawOverlay(ToolInput hover, bool isHovered)
        {
            if (!isHovered)
                return;

            Rectangle area = Editor.Preview.HitTest(hover.World) is int id && Editor.Preview.RootOf(id) is { } entity
                ? entity.Rect
                : new TileRect(hover.Col, hover.Row, 1, 1).ToWorld();

            Editor.Viewport.DrawOutline(area, Theme.Info);
        }
    }
}
