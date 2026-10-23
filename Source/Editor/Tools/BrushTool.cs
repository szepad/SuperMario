namespace Mario2.Editor
{
    public sealed class BrushTool(LevelEditor editor) : Tool(editor)
    {
        private readonly EraseGesture _erase = new(editor);
        private bool _painting;
        private int _lastCol, _lastRow;

        public override string Name => "Brush";
        public override KeyboardKey Shortcut => KeyboardKey.B;
        public override EditorIcon Icon => EditorIcon.Brush;
        public override string Hint => "Click or drag to paint · Right-drag to erase · Alt+click to pick";
        public override bool IsBusy => _painting || _erase.IsActive;

        public override void OnPress(ToolInput input, MouseButton button)
        {
            if (button == MouseButton.Right)
            {
                _erase.Begin(input);
                return;
            }

            if (input.Alt)
            {
                Editor.PickAt(input);
                return;
            }

            switch (Editor.Brush)
            {
                case TileBrush tile:
                    Editor.Document.Begin($"Paint {tile.Name}");
                    Editor.PaintLineNow(input.Col, input.Row, input.Col, input.Row, tile.Spec);
                    (_lastCol, _lastRow) = (input.Col, input.Row);
                    _painting = true;
                    break;

                case EntityBrush entity:
                    Editor.PlaceEntity(entity, Editor.FeetAt(input));
                    break;

                case StampBrush stamp:
                    Editor.PlaceStamp(stamp.Stamp, input.Col, input.Row);
                    break;
            }
        }

        public override void OnDrag(ToolInput input)
        {
            if (_erase.IsActive)
            {
                _erase.Drag(input);
                return;
            }

            if (_painting && Editor.Brush is TileBrush tile)
            {
                Editor.PaintLineNow(_lastCol, _lastRow, input.Col, input.Row, tile.Spec);
                (_lastCol, _lastRow) = (input.Col, input.Row);
            }
        }

        public override void OnRelease(ToolInput input)
        {
            _erase.End();
            if (_painting)
            {
                Editor.Document.Commit();
                _painting = false;
            }
        }

        public override void Cancel()
        {
            _erase.Cancel();
            if (_painting)
            {
                Editor.Document.Cancel();
                _painting = false;
            }
        }

        public override void DrawOverlay(ToolInput hover, bool isHovered)
        {
            if (!isHovered || _erase.IsActive)
                return;

            Editor.Viewport.DrawBrushGhost(Editor.Brush, hover);
        }
    }
}
