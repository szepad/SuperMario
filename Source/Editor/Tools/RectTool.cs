namespace Mario2.Editor
{
    public sealed class RectTool(LevelEditor editor) : Tool(editor)
    {
        private (int Col, int Row)? _start;
        private (int Col, int Row) _end;
        private bool _clearing;

        public override string Name => "Rectangle";
        public override KeyboardKey Shortcut => KeyboardKey.R;
        public override EditorIcon Icon => EditorIcon.Rectangle;
        public override string Hint => "Drag to fill an area with the tile brush · Right-drag to clear an area";
        public override bool IsBusy => _start is not null;

        private TileRect? CurrentRect => _start is { } s ? TileRect.FromCorners(s.Col, s.Row, _end.Col, _end.Row) : null;

        public override void OnPress(ToolInput input, MouseButton button)
        {
            _clearing = button == MouseButton.Right;
            if (!_clearing && !Editor.Brush.PaintsTiles)
            {
                Editor.Notify("The rectangle tool fills with tile brushes. Pick a tile from the palette.");
                return;
            }

            _start = (input.Col, input.Row);
            _end = _start.Value;
        }

        public override void OnDrag(ToolInput input)
        {
            if (_start is not null)
                _end = (input.Col, input.Row);
        }

        public override void OnRelease(ToolInput input)
        {
            if (CurrentRect is not { } rect)
                return;

            _start = null;
            TileSpec spec = _clearing || Editor.Brush is not TileBrush tile ? TileSpec.Air : tile.Spec;
            Editor.FillRect(rect, spec, _clearing ? "Clear Area" : $"Fill {Editor.Brush.Name}");
        }

        public override void Cancel()
        {
            _start = null;
        }

        public override void DrawOverlay(ToolInput hover, bool isHovered)
        {
            if (CurrentRect is { } rect)
            {
                Rectangle area = rect.ToWorld();
                Color color = _clearing ? Theme.Danger : Theme.Info;

                if (!_clearing && Editor.Brush is TileBrush tile)
                    Editor.Viewport.DrawTileGhostArea(tile.Spec, rect);

                Raylib.DrawRectangleRec(area, color.Alpha(0.15f));
                Editor.Viewport.DrawOutline(area, color);
                return;
            }

            if (isHovered)
                Editor.Viewport.DrawBrushGhost(Editor.Brush, hover);
        }
    }
}
