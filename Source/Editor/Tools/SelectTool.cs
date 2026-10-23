namespace Mario2.Editor
{
    public sealed class SelectTool(LevelEditor editor) : Tool(editor)
    {
        private enum Gesture { None, Move, Marquee }

        private Gesture _gesture;
        private ToolInput _press;
        private ToolInput _current;
        private bool _moveStarted;

        private Dictionary<int, (int X, int Y)> _origins = [];
        private (int X, int Y) _spawnOrigin;

        // The feet of what was grabbed (an entity or the player start); they snap, the rest follows
        private (int X, int Y) _anchor;

        public override string Name => "Select";
        public override KeyboardKey Shortcut => KeyboardKey.V;
        public override EditorIcon Icon => EditorIcon.Select;
        public override string Hint => "Click to select · Drag to move or box-select · Shift adds · Arrows nudge · Del deletes";
        public override bool IsBusy => _gesture != Gesture.None;

        public override void OnPress(ToolInput input, MouseButton button)
        {
            if (button != MouseButton.Left)
                return;

            _press = _current = input;
            EditorSelection selection = Editor.Selection;

            if (Editor.Preview.SpawnHit(input.World))
            {
                if (input.Shift)
                {
                    selection.ToggleSpawn();
                    return;
                }

                if (!selection.Spawn)
                    selection.SelectSpawn();

                BeginMove(grabbed: null);
                return;
            }

            if (Editor.Preview.HitTest(input.World) is int id)
            {
                if (input.Shift)
                {
                    selection.ToggleEntity(id);
                    return;
                }

                if (!selection.Contains(id))
                    selection.SelectEntity(id);

                BeginMove(grabbed: id);
                return;
            }

            _gesture = Gesture.Marquee;
        }

        public override void OnDrag(ToolInput input)
        {
            _current = input;
            if (_gesture != Gesture.Move)
                return;

            Vector2 delta = input.Pixels - _press.Pixels;
            int dx, dy;
            if (input.Alt)
            {
                // Free movement in whole pixels
                dx = (int)MathF.Round(delta.X);
                dy = (int)MathF.Round(delta.Y);
            }
            else
            {
                // The grabbed feet snap like a placement would (16 px: a tile's bottom-center)
                (int x, int y) = LevelEditor.SnapFeet(new Vector2(_anchor.X, _anchor.Y) + delta);
                dx = x - _anchor.X;
                dy = y - _anchor.Y;
            }

            if (!_moveStarted)
            {
                // Snapping can move an off-grid entity without any mouse movement, so a plain click must not count
                if ((dx == 0 && dy == 0) || !IsDrag())
                    return;

                Editor.Document.Begin("Move");
                _moveStarted = true;
            }

            Editor.MoveSelectionTo(_origins, _spawnOrigin, dx, dy);
        }

        public override void OnRelease(ToolInput input)
        {
            _current = input;

            if (_gesture == Gesture.Move && _moveStarted)
                Editor.Document.Commit();

            if (_gesture == Gesture.Marquee)
                FinishMarquee(input);

            _gesture = Gesture.None;
            _moveStarted = false;
        }

        public override void Cancel()
        {
            if (_gesture == Gesture.Move && _moveStarted)
                Editor.Document.Cancel();

            _gesture = Gesture.None;
            _moveStarted = false;
        }

        public override void DrawOverlay(ToolInput hover, bool isHovered)
        {
            if (_gesture == Gesture.Marquee && IsDrag())
            {
                TileRect rect = MarqueeRect();
                Rectangle area = rect.ToWorld();
                Raylib.DrawRectangleRec(area, Theme.Info.Alpha(0.12f));
                Editor.Viewport.DrawOutline(area, Theme.Info);
                return;
            }

            if (_gesture == Gesture.None && isHovered)
            {
                if (Editor.Preview.SpawnHit(hover.World))
                    Editor.Viewport.DrawOutline(Editor.Preview.World.Player.Rect, Theme.Hover);
                else if (Editor.Preview.HitTest(hover.World) is int id && Editor.Preview.RootOf(id) is { } entity)
                    Editor.Viewport.DrawOutline(entity.Rect, Theme.Hover);
            }
        }

        /// <param name="grabbed">The entity under the cursor, or null for the player start.</param>
        private void BeginMove(int? grabbed)
        {
            _gesture = Gesture.Move;
            _moveStarted = false;
            _origins = [];

            foreach (int id in Editor.Selection.Entities)
            {
                if (Editor.Document.Level.FindEntity(id) is { } placement)
                    _origins[id] = (placement.X, placement.Y);
            }

            LevelMeta meta = Editor.Document.Level.Meta;
            _spawnOrigin = (meta.SpawnX, meta.SpawnY);
            _anchor = grabbed is null ? _spawnOrigin : _origins.GetValueOrDefault(grabbed.Value, _spawnOrigin);
        }

        private bool IsDrag()
        {
            Vector2 screenDelta = (_current.World - _press.World) * Editor.Camera.Zoom;
            return screenDelta.Length() > 4f;
        }

        private TileRect MarqueeRect()
            => TileRect.FromCorners(_press.Col, _press.Row, _current.Col, _current.Row).ClampTo(Editor.Document.Level);

        private void FinishMarquee(ToolInput input)
        {
            EditorSelection selection = Editor.Selection;

            if (!IsDrag())
            {
                // A plain click on the level selects the tile under the cursor
                if (!input.Shift)
                {
                    if (!Editor.Document.Level.Get(input.Col, input.Row).IsAir)
                        selection.SelectTile(input.Col, input.Row);
                    else
                        selection.Clear();
                }

                return;
            }

            TileRect rect = MarqueeRect();
            Vector2 min = Vector2.Min(_press.World, input.World);
            Vector2 max = Vector2.Max(_press.World, input.World);
            List<int> inside = [.. Editor.Preview.PlacementsInside(new Rectangle(min, max - min))];

            selection.SelectEntities(inside, additive: input.Shift);
            if (rect.Width > 0 && rect.Height > 0)
                selection.SetRegion(rect);
        }
    }
}
