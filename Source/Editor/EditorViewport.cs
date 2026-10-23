using Gum.Wireframe;

namespace Mario2.Editor
{
    /// <summary>
    /// The level view: routes mouse input to the camera and the active tool, and draws the
    /// level, editor overlays and tool previews straight to the window (clipped to the
    /// viewport area the UI leaves free).
    /// </summary>
    public sealed class EditorViewport(LevelEditor editor)
    {
        private const float GhostOpacity = 0.55f;

        // A block's contents are drawn over it at full size, slightly see-through
        private const float ItemOverlayOpacity = 0.8f;

        /// <summary>Zoom per mouse wheel click.</summary>
        private const float WheelZoomStep = 1.15f;

        /// <summary>Zoom per point of smooth scrolling with Cmd/Ctrl held (about 2x per 150 points).</summary>
        private const float SmoothZoomStep = 1.005f;

        /// <summary>How far one wheel click pans sideways, in screen pixels.</summary>
        private const float WheelPanStep = 64f;

        public bool IsHovered { get; private set; }
        public ToolInput Hover { get; private set; }
        public bool HasHoverCell => IsHovered && editor.Document.Level.InBounds(Hover.Col, Hover.Row);
        public bool IsBusy => _toolButton is not null || _panning;

        private EditorCamera Camera => editor.Camera;

        private MouseButton? _toolButton;
        private bool _panning;
        private MouseButton _panButton;
        private bool _keyboardFree = true;

        /// <param name="scroll">
        /// Scrolling meant for the level (already routed by <see cref="ScrollLatch"/>) plus this
        /// frame's pinch gestures, which only apply while the level is hovered.
        /// </param>
        public void Update(Rectangle area, bool mouseBlocked, bool keyboardFree, ScrollInput scroll)
        {
            Camera.Viewport = area;
            _keyboardFree = keyboardFree;

            Vector2 mouse = Raylib.GetMousePosition();
            IsHovered = Raylib.CheckCollisionPointRec(mouse, area) && !mouseBlocked;
            Hover = InputAt(mouse);

            HandlePanning();
            HandleScroll(mouse, scroll);
            HandleTool();

            Camera.Clamp(editor.Document.Level.Width * Tile.Size);
        }

        /// <summary>Aborts any drag in progress (tool switch, dialog, playtest).</summary>
        public void CancelGesture()
        {
            if (_toolButton is not null)
            {
                editor.Tool.Cancel();
                _toolButton = null;
            }

            _panning = false;
        }

        private ToolInput InputAt(Vector2 screen)
        {
            Vector2 world = Camera.ScreenToWorld(screen);
            bool shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
            bool alt = Raylib.IsKeyDown(KeyboardKey.LeftAlt) || Raylib.IsKeyDown(KeyboardKey.RightAlt);

            return new ToolInput(world,
                (int)MathF.Floor(world.X / Tile.Size),
                (int)MathF.Floor(world.Y / Tile.Size),
                shift, alt);
        }

        private void HandlePanning()
        {
            if (!_panning && _toolButton is null && IsHovered)
            {
                bool spaceHeld = _keyboardFree && Raylib.IsKeyDown(KeyboardKey.Space);
                if (Raylib.IsMouseButtonPressed(MouseButton.Middle))
                    StartPan(MouseButton.Middle);
                else if (spaceHeld && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    StartPan(MouseButton.Left);
            }

            if (!_panning)
                return;

            Camera.Pan(Raylib.GetMouseDelta());
            if (!Raylib.IsMouseButtonDown(_panButton))
                _panning = false;
        }

        private void StartPan(MouseButton button)
        {
            _panning = true;
            _panButton = button;
            InteractiveGue.CurrentInputReceiver = null;
        }

        /// <summary>
        /// Trackpad: two-finger scroll pans, pinch zooms. Mouse: the wheel zooms, Shift+wheel pans
        /// sideways. Cmd/Ctrl+scroll zooms with either (Windows reports a pinch that way).
        /// </summary>
        private void HandleScroll(Vector2 mouse, ScrollInput scroll)
        {
            // Panning also works mid-stroke, so a long drag can scroll the view along
            if (Shortcuts.IsModDown())
            {
                float steps = scroll.Notches.Y;
                float points = scroll.Points.Y;
                if (steps != 0f || points != 0f)
                    Camera.ZoomAt(mouse, MathF.Pow(WheelZoomStep, steps) * MathF.Pow(SmoothZoomStep, points));
            }
            else
            {
                Camera.Pan(scroll.Points);

                // A sideways wheel (or Shift+wheel, which macOS turns into one) pans
                Vector2 notches = scroll.Notches;
                if (Shortcuts.IsShiftDown())
                    notches = new Vector2(notches.X + notches.Y, 0f);

                Camera.Pan(new Vector2(notches.X * WheelPanStep, 0f));
                if (notches.Y != 0f)
                    Camera.ZoomAt(mouse, MathF.Pow(WheelZoomStep, notches.Y));
            }

            if (!IsHovered)
                return;

            if (scroll.Magnify != 1f)
                Camera.ZoomAt(mouse, scroll.Magnify);

            if (scroll.SmartMagnify)
                Camera.ToggleSmartZoom(mouse);
        }

        private void HandleTool()
        {
            if (_toolButton is null && !_panning && IsHovered)
            {
                MouseButton? pressed = Raylib.IsMouseButtonPressed(MouseButton.Left) ? MouseButton.Left
                    : Raylib.IsMouseButtonPressed(MouseButton.Right) ? MouseButton.Right
                    : null;

                if (pressed is { } button)
                {
                    // Clicking into the level takes keyboard focus away from the UI
                    InteractiveGue.CurrentInputReceiver = null;
                    _toolButton = button;
                    editor.Tool.OnPress(Hover, button);
                }
            }

            if (_toolButton is not { } held)
                return;

            if (Raylib.IsMouseButtonDown(held))
            {
                editor.Tool.OnDrag(Hover);
            }
            else
            {
                editor.Tool.OnRelease(Hover);
                _toolButton = null;
            }
        }

        #region Drawing

        public void Draw()
        {
            Rectangle area = Camera.Viewport;
            if (area.Width <= 0 || area.Height <= 0)
                return;

            LevelData level = editor.Document.Level;
            EditorPreview preview = editor.Preview;
            float levelWidth = level.Width * Tile.Size;
            float levelHeight = LevelData.Height * Tile.Size;
            Rectangle levelArea = new(0, 0, levelWidth, levelHeight);

            Raylib.BeginScissorMode((int)area.X, (int)area.Y, (int)area.Width, (int)area.Height);
            Raylib.DrawRectangleRec(area, Theme.Outside);
            Raylib.BeginMode2D(Camera.AsCamera2D);

            Raylib.DrawRectangleRec(levelArea, preview.World.Parallax.ClearColor);
            if (EditorPrefs.ShowBackground)
                DrawBackground(preview.World.Parallax, levelArea);

            (int minCol, int maxCol) = VisibleColumns(level);

            // Same order as the game: background entities, tiles, everything else
            List<Entity> entities = [.. preview.World.Entities.OrderBy(e => e.LayerDepth)];
            foreach (Entity entity in entities.Where(e => e.LayerDepth == LayerDepth.Background))
                entity.Draw();

            DrawTiles(preview, minCol, maxCol);

            foreach (Entity entity in entities.Where(e => e.LayerDepth != LayerDepth.Background))
                entity.Draw();

            DrawTileOverlays(preview, minCol, maxCol);

            if (EditorPrefs.ShowGrid)
                DrawGrid(minCol, maxCol);

            if (EditorPrefs.ShowScreenGuides)
                DrawScreenGuides(level);

            DrawOutline(levelArea, Theme.LevelBorder);
            DrawSpawnMarker(preview);
            DrawSelection();

            editor.Tool.DrawOverlay(Hover, IsHovered && !_panning);

            Raylib.EndMode2D();
            Raylib.EndScissorMode();
        }

        private (int Min, int Max) VisibleColumns(LevelData level)
        {
            Rectangle visible = Camera.VisibleWorld;
            int min = Math.Clamp((int)MathF.Floor(visible.X / Tile.Size), 0, level.Width);
            int max = Math.Clamp((int)MathF.Ceiling((visible.X + visible.Width) / Tile.Size), 0, level.Width);
            return (min, max);
        }

        private static void DrawBackground(ParallaxBackground parallax, Rectangle levelArea)
        {
            // Tile every layer across the level, world-aligned (no parallax in the editor)
            foreach (ParallaxLayer layer in parallax.Layers)
            {
                float span = layer.Tex.Width * Game.TextureScale * Math.Max(1, layer.RepeatCount);
                if (span <= 0f)
                    continue;

                Vector2 saved = layer.Position;
                for (float x = 0f; x < levelArea.Width; x += span)
                {
                    layer.Position = new Vector2(x, 0f);
                    layer.Draw();
                }

                layer.Position = saved;
            }

            // Hide whatever the layers drew outside of the level
            const float Big = 1_000_000f;
            Raylib.DrawRectangleRec(new Rectangle(levelArea.Width, -Big, Big, Big * 2f), Theme.Outside);
            Raylib.DrawRectangleRec(new Rectangle(-Big, -Big, Big * 2f + levelArea.Width, Big), Theme.Outside);
            Raylib.DrawRectangleRec(new Rectangle(-Big, levelArea.Height, Big * 2f + levelArea.Width, Big), Theme.Outside);
        }

        private static void DrawTiles(EditorPreview preview, int minCol, int maxCol)
        {
            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = minCol; col < maxCol; col++)
                {
                    TileSpec spec = preview.ResolvedAt(col, row);
                    if (spec.IsAir)
                        continue;

                    bool hidden = spec is QuestionSpec { Hidden: true } or BrickSpec { Hidden: true };
                    preview.TileFor(spec).Draw(col, row, hidden ? 0.35f : 1f);
                }
            }
        }

        private static void DrawTileOverlays(EditorPreview preview, int minCol, int maxCol)
        {
            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = minCol; col < maxCol; col++)
                {
                    TileSpec spec = preview.ResolvedAt(col, row);
                    Rectangle cell = new(col * Tile.Size, row * Tile.Size, Tile.Size, Tile.Size);

                    if (EditorPrefs.ShowCollision && spec.IsSolid)
                        Raylib.DrawRectangleRec(cell, Theme.Collision);

                    DrawItemOverlay(spec, col, row);
                }
            }
        }

        /// <summary>
        /// What a block contains (mushroom, coins, star...), drawn over the block and slightly see-through,
        /// so the block still shows underneath.
        /// </summary>
        private static void DrawItemOverlay(TileSpec spec, int col, int row, float opacity = 1f)
        {
            ItemType item = spec switch
            {
                QuestionSpec q => q.Item,
                BrickSpec b => b.Item,
                _ => ItemType.None
            };

            if (item == ItemType.None)
                return;

            Rectangle dest = new TileRect(col, row, 1, 1).ToWorld();
            Icon icon = Icon.Item(item);
            Raylib.DrawTexturePro(Assets.GetTex(icon.Texture), icon.Source, dest, Vector2.Zero, 0f, Color.White.Alpha(ItemOverlayOpacity * opacity));
        }

        private void DrawGrid(int minCol, int maxCol)
        {
            float bottom = LevelData.Height * Tile.Size;
            for (int col = minCol; col <= maxCol; col++)
            {
                Raylib.DrawLineV(new Vector2(col * Tile.Size, 0f), new Vector2(col * Tile.Size, bottom), Theme.Grid);
            }

            for (int row = 0; row <= LevelData.Height; row++)
            {
                Raylib.DrawLineV(new Vector2(minCol * Tile.Size, row * Tile.Size), new Vector2(maxCol * Tile.Size, row * Tile.Size), Theme.Grid);
            }
        }

        private void DrawScreenGuides(LevelData level)
        {
            // One screen of the original game is 16 tiles wide
            const int ScreenTiles = Game.Width / Tile.Size;
            float thickness = 2f / Camera.Zoom;
            for (int col = ScreenTiles; col < level.Width; col += ScreenTiles)
            {
                Raylib.DrawLineEx(new Vector2(col * Tile.Size, 0f), new Vector2(col * Tile.Size, LevelData.Height * Tile.Size), thickness, Theme.ScreenGuide);
            }
        }

        private void DrawSpawnMarker(EditorPreview preview)
        {
            Rectangle rect = preview.World.Player.Rect;
            float size = 6f / Camera.Zoom;
            Vector2 feet = preview.World.Player.Feet;
            Raylib.DrawTriangle(feet + new Vector2(0f, size), feet + new Vector2(size, size * 2f), feet + new Vector2(-size, size * 2f), Theme.Spawn);

            if (Camera.Zoom > 0.12f)
                Raylib.DrawTextEx(Raylib.GetFontDefault(), "START", new Vector2(rect.X, rect.Y - 14f / Camera.Zoom), 10f / Camera.Zoom, 1f / Camera.Zoom, Theme.Spawn);
        }

        private void DrawSelection()
        {
            EditorSelection selection = editor.Selection;
            EditorPreview preview = editor.Preview;

            foreach (int id in selection.Entities)
            {
                if (preview.RootOf(id) is { } entity)
                {
                    Raylib.DrawRectangleRec(entity.Rect, Theme.Info.Alpha(0.2f));
                    DrawOutline(entity.Rect, Theme.Info);
                }
            }

            if (selection.Spawn)
                DrawOutline(preview.World.Player.Rect, Theme.Info);

            if (selection.Tile is { } tile)
                DrawOutline(new TileRect(tile.Col, tile.Row, 1, 1).ToWorld(), Theme.Info);

            if (selection.Region is { } region)
            {
                Rectangle area = region.ToWorld();
                Raylib.DrawRectangleRec(area, Theme.Info.Alpha(0.1f));
                DrawOutline(area, Theme.Info);
            }
        }

        /// <summary>A rectangle outline that stays two screen pixels thick at any zoom.</summary>
        public void DrawOutline(Rectangle area, Color color)
        {
            Raylib.DrawRectangleLinesEx(area, 2f / Camera.Zoom, color);
        }

        public void DrawBrushGhost(Brush brush, ToolInput hover)
        {
            LevelData level = editor.Document.Level;

            switch (brush)
            {
                case TileBrush tile:
                    if (level.InBounds(hover.Col, hover.Row))
                    {
                        DrawTileGhost(tile.Spec, hover.Col, hover.Row);
                        DrawOutline(new TileRect(hover.Col, hover.Row, 1, 1).ToWorld(), Theme.Hover);
                    }
                    editor.Ghost.Clear();
                    break;

                case EntityBrush entity:
                    (int x, int y) = editor.FeetAt(hover);
                    editor.Ghost.Set([new EntityPlacement(0, entity.Def.Key, x, y, entity.Props)]);
                    editor.Ghost.Draw(GhostOpacity);
                    break;

                case StampBrush stamp:
                    foreach ((int col, int row, TileSpec spec) in stamp.Stamp.ResolveCells(level, hover.Col, hover.Row))
                    {
                        if (!spec.IsAir)
                            DrawTileGhost(spec, col, row);
                    }

                    editor.Ghost.Set(stamp.Stamp.ResolveEntities(hover.Col, hover.Row));
                    editor.Ghost.Draw(GhostOpacity);
                    DrawOutline(new TileRect(hover.Col, hover.Row, stamp.Stamp.Width, stamp.Stamp.Height).ToWorld(), Theme.Hover);
                    break;
            }
        }

        public void DrawTileGhostArea(TileSpec spec, TileRect rect)
        {
            for (int row = rect.Row; row < rect.Bottom; row++)
            {
                for (int col = rect.Col; col < rect.Right; col++)
                {
                    DrawTileGhost(spec, col, row);
                }
            }
        }

        private void DrawTileGhost(TileSpec spec, int col, int row)
        {
            LevelData level = editor.Document.Level;
            if (spec is AutoSpec auto)
                spec = AutoTiles.ResolveFor(level, auto.Group, col, row);

            if (spec.IsAir)
            {
                Raylib.DrawRectangleRec(new TileRect(col, row, 1, 1).ToWorld(), Theme.EraseFill);
                return;
            }

            editor.Preview.TileFor(spec).Draw(col, row, GhostOpacity);
            DrawItemOverlay(spec, col, row, GhostOpacity);
        }

        #endregion
    }
}
