namespace Mario2.Editor
{
    /// <summary>Mouse state handed to tools. World is in world units (scaled pixels).</summary>
    public readonly record struct ToolInput(Vector2 World, int Col, int Row, bool Shift, bool Alt)
    {
        public Vector2 Pixels => World / Game.TextureScale;
    }

    public abstract class Tool(LevelEditor editor)
    {
        protected LevelEditor Editor { get; } = editor;

        public abstract string Name { get; }
        public abstract KeyboardKey Shortcut { get; }
        public abstract EditorIcon Icon { get; }
        public abstract string Hint { get; }

        /// <summary>Whether a press/drag/release gesture is in progress.</summary>
        public virtual bool IsBusy => false;

        public virtual void OnPress(ToolInput input, MouseButton button) { }
        public virtual void OnDrag(ToolInput input) { }
        public virtual void OnRelease(ToolInput input) { }

        /// <summary>Aborts the current gesture, undoing what it did so far.</summary>
        public virtual void Cancel() { }

        /// <summary>Drawn in world space on top of the level.</summary>
        public virtual void DrawOverlay(ToolInput hover, bool isHovered) { }
    }

    /// <summary>
    /// Right-click (or Erase tool) dragging. Starting on an entity removes entities as the
    /// cursor passes over them; starting anywhere else clears tiles. That way clearing tiles
    /// never takes out an entity by accident and vice versa.
    /// </summary>
    public sealed class EraseGesture(LevelEditor editor)
    {
        private enum Mode { None, Entities, Tiles }
        private Mode _mode;
        private int _lastCol, _lastRow;

        public bool IsActive => _mode != Mode.None;

        public void Begin(ToolInput input)
        {
            editor.Document.Begin("Erase");
            if (editor.Preview.HitTest(input.World) is int id)
            {
                _mode = Mode.Entities;
                editor.RemoveEntityNow(id);
            }
            else
            {
                _mode = Mode.Tiles;
                editor.PaintLineNow(input.Col, input.Row, input.Col, input.Row, TileSpec.Air);
            }

            (_lastCol, _lastRow) = (input.Col, input.Row);
        }

        public void Drag(ToolInput input)
        {
            if (_mode == Mode.Entities)
            {
                if (editor.Preview.HitTest(input.World) is int id)
                    editor.RemoveEntityNow(id);
            }
            else if (_mode == Mode.Tiles)
            {
                editor.PaintLineNow(_lastCol, _lastRow, input.Col, input.Row, TileSpec.Air);
            }

            (_lastCol, _lastRow) = (input.Col, input.Row);
        }

        public void End()
        {
            if (_mode == Mode.None)
                return;

            editor.Document.Commit();
            _mode = Mode.None;
        }

        public void Cancel()
        {
            if (_mode == Mode.None)
                return;

            editor.Document.Cancel();
            _mode = Mode.None;
        }
    }
}
