namespace Mario2.Editor
{
    public class WorldWindow : EditorWindow
    {
        public Vector2 RenderTexDrawPosition { get; private set; }
        public RenderTexture2D WorldContentTex { get; set; }

        public Entity? SelectedEntity { get; private set; }

        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 0.4f);

        private bool _isPanning;
        private Vector2 _panStartPos;

        private (int X, int Y) _tilePos;

        private bool _firstRender = true;

        // After deleting an entity, disable right click, so we don't also delete tiles behind it
        private bool _disableRightClickTemporarily;
        private bool _disableLeftClickTemporarily;

        public WorldWindow(EditorScene editor)
            : base(editor, "Editor")
        {
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
            AssignRuleTileRefs();
        }

        public Vector2 GetMousePositionInsideEditor()
            => Raylib.GetMousePosition() - RenderTexDrawPosition;

        public Vector2 ScreenToWorld(Vector2 screenPos)
            => (screenPos + _camera.Target * _camera.Zoom) / _camera.Zoom;

        public Vector2 WorldToScreen(Vector2 worldPos)
            => (worldPos - _camera.Target) * _camera.Zoom;

        public void DeselectEntity()
        {
            SelectedEntity = null;
        }

        public void FocusCameraOn(Entity entity)
        {
            _camera.Target = entity.Center - new Vector2(Game.Width, Game.Height) / 2f;
        }

        public override void Update()
        {
            Vector2 screenPos = ScreenToWorld(GetMousePositionInsideEditor());
            _tilePos.X = (int)(screenPos.X / Tile.Size);
            _tilePos.Y = (int)(screenPos.Y / Tile.Size);

            HandleZooming();
            HandlePanning();
            HandleEditing();

            if (Editor.IsFileDirty) Flags |= ImGuiWindowFlags.UnsavedDocument;
            else Flags &= ~ImGuiWindowFlags.UnsavedDocument;
        }

        private void HandleZooming()
        {
            if (!IsBeingHovered)
                return;

            float scroll = Raylib.GetMouseWheelMove();
            Vector2 mousePos = GetMousePositionInsideEditor();

            if (scroll != 0)
            {
                float prevZoom = _camera.Zoom;

                _camera.Zoom += (_camera.Zoom / 5f) * scroll;
                _camera.Zoom = Helper.Clamp(_camera.Zoom, 0.1f, 25f);

                Vector2 prevNormalPos = (mousePos + _camera.Target * prevZoom) / prevZoom;
                Vector2 currNormalPos = (mousePos + _camera.Target * _camera.Zoom) / _camera.Zoom;
                Vector2 diff = currNormalPos - prevNormalPos;
                _camera.Target -= diff;
            }
        }

        private void HandlePanning()
        {
            if (!IsBeingHovered)
                _isPanning = false;

            if (Raylib.IsMouseButtonPressed(MouseButton.Middle))
            {
                _panStartPos = GetMousePositionInsideEditor();
                _isPanning = true;
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Middle) && _isPanning)
            {
                Vector2 mousePos = GetMousePositionInsideEditor();
                _camera.Target += (_panStartPos - mousePos) / _camera.Zoom;
                _panStartPos = mousePos;
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Middle))
            {
                _isPanning = false;
            }
        }

        private void HandleEditing()
        {
            if (!IsBeingHovered)
                return;

            (int tx, int ty) = _tilePos;
            Entity? entityUnderMouse = GetEntityUnderMouse();

            HandlePlacing(entityUnderMouse, tx, ty);
            HandleDeleting(entityUnderMouse, tx, ty);

            if (Raylib.IsMouseButtonUp(MouseButton.Left))
            {
                _disableLeftClickTemporarily = false;
            }
            
            if (Raylib.IsMouseButtonUp(MouseButton.Right))
            {
                _disableRightClickTemporarily = false;
            }

            if (SelectedEntity is not null)
            {
                const float MoveOffset = 8f * Game.TextureScale;
                if (Raylib.IsKeyPressed(KeyboardKey.Left)) SelectedEntity.Position.X -= MoveOffset;
                if (Raylib.IsKeyPressed(KeyboardKey.Right)) SelectedEntity.Position.X += MoveOffset;
                if (Raylib.IsKeyPressed(KeyboardKey.Up)) SelectedEntity.Position.Y -= MoveOffset;
                if (Raylib.IsKeyPressed(KeyboardKey.Down)) SelectedEntity.Position.Y += MoveOffset;
            }
        }

        private void HandlePlacing(Entity? entityUnderMouse, int tx, int ty)
        {
            if (!Raylib.IsMouseButtonDown(MouseButton.Left) || _disableLeftClickTemporarily)
                return;
            
            if (entityUnderMouse is not null)
            {
                SelectedEntity = entityUnderMouse;
                return;
            }

            if (Editor.ActiveSelector is TileWindow tileWindow)
            {
                PlaceTile(tileWindow, tx, ty);
            }
            else if (Editor.ActiveSelector is TilesetRawWindow tileRawWindow)
            {
                PlaceTileRaw(tileRawWindow, tx, ty);
            }
            else if (Editor.ActiveSelector is EntityWindow entityWindow)
            {
                PlaceEntity(entityWindow, tx, ty);
            }

            Editor.Dirty();
        }

        private void PlaceTile(TileWindow tileWindow, int tx, int ty)
        {
            Tile? tile = Editor.World.SetTile(tx, ty, tileWindow.PlaceSelected(Editor.World, tx, ty));
            if (tile is ContainerTile ct)
            {
                ct.SetItem(Editor.PropertyWindow.ContainerTileItem);

                if (Editor.PropertyWindow.ContainerTileHidden)
                    ct.SetHidden();

                if (tile is BrickTile brick)
                {
                    brick.SetPalette(Editor.PropertyWindow.BrickTilePalette);
                }
            }

            UpdateRule(Editor.World, tx, ty);
        }

        private void PlaceTileRaw(TilesetRawWindow tileRawWindow, int tx, int ty)
        {
            Editor.World.SetTile(tx, ty, tileRawWindow.PlaceSelected(Editor.World, tx, ty));
        }

        private void PlaceEntity(EntityWindow entityWindow, int tx, int ty)
        {
            Entity entity = entityWindow.PlaceSelected(Editor.World, tx, ty);
            Vector2 worldPos = ScreenToWorld(GetMousePositionInsideEditor());
            Vector2 tilePos = worldPos / Tile.Size;
            float xFrac = tilePos.X - (int)tilePos.X;
            float xOff = 0.5f;
            if (xFrac < 1f / 4f)
            {
                xOff = 0f;
            }
            else if (xFrac > 3f / 4f)
            {
                xOff = 1f;
            }

            Vector2 pos = new(
                ((int)tilePos.X + xOff) * Tile.Size,
                ((int)tilePos.Y + 1f) * Tile.Size);

            if (entity is Player)
            {
                Editor.World.Player.SetFeetPos(pos);
                SelectedEntity = Editor.World.Player;
            }
            else
            {
                SelectedEntity = Editor.World.SpawnEntity(entity, pos);
            }

            _disableLeftClickTemporarily = true;
        }

        private void HandleDeleting(Entity? entityUnderMouse, int tx, int ty)
        {
            if (Raylib.IsMouseButtonDown(MouseButton.Right) && !_disableRightClickTemporarily)
            {
                if (entityUnderMouse is null)
                {
                    Editor.World.SetTile(tx, ty, SimpleTile.Air);
                    UpdateRule(Editor.World, tx, ty);
                    Editor.Dirty();
                }
                else if (entityUnderMouse is not Player)
                {
                    Editor.World.Entities.Remove(entityUnderMouse);
                    _disableRightClickTemporarily = true;
                }

                DeselectEntity();
            }
        }

        public void AssignRuleTileRefs()
        {
            World world = Editor.World;
            for (int row = 0; row < World.Rows; row++)
            {
                for (int col = 0; col < world.Cols; col++)
                {
                    if (!world.GetTile(col, row, out Tile tile) || tile == SimpleTile.Air)
                        continue;

                    bool assigned = false;
                    foreach (ISelectableObject<Tile> selectable in Editor.TileWindow.Selectables)
                    {
                        if (selectable is RuleTile ruleTile)
                        {
                            foreach (TileObject obj in ruleTile.Rules.Values)
                            {
                                Tile objTile = obj.OnPlace(world, 0, 0);
                                if (tile is SimpleTile st)
                                {
                                    if (objTile is SimpleTile ot && st.Id == ot.Id)
                                    {
                                        tile.Rule = ruleTile;
                                        assigned = true;
                                        break;
                                    }
                                }
                                else if (tile.GetType().FullName == objTile.GetType().FullName)
                                {
                                    tile.Rule = ruleTile;
                                    assigned = true;
                                    break;
                                }
                            }

                            if (assigned)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void UpdateRule(World world, int col, int row)
        {
            void Update(int x, int y)
            {
                if (world.GetTile(x, y, out Tile tile) && tile.Rule is not null)
                {
                    world.SetTile(x, y, tile.Rule.OnPlace(world, x, y));
                }
            };

            Update(col, row - 1);
            Update(col + 1, row);
            Update(col, row + 1);
            Update(col - 1, row);
        }

        private Entity? GetEntityUnderMouse()
        {
            Vector2 mousePos = ScreenToWorld(GetMousePositionInsideEditor());

            foreach (Entity entity in Editor.World.Entities)
            {
                if (Raylib.CheckCollisionPointRec(mousePos, entity.Rect))
                {
                    return entity;
                }
            }

            return null;
        }

        public void RenderWorld()
        {
            Raylib.BeginTextureMode(WorldContentTex);

            //Raylib.ClearBackground(Editor.World.Parallax.ClearColor);
            Raylib.ClearBackground(Color.Black);

            if (!_firstRender)
            {
                Raylib.BeginMode2D(_camera);
                
                Editor.World.DrawBackground();
                if (Editor.Options.ShowGrid)
                    DrawGrid();
                
                Editor.World.Draw(default, editor: true);
                if (SelectedEntity is not null)
                {
                    const float Scale = 1.5f;
                    Rectangle rect = SelectedEntity.Rect;
                    rect.X -= (rect.Width * Scale - rect.Width) / 2f;
                    rect.Y -= (rect.Height * Scale - rect.Height) / 2f;
                    rect.Width *= Scale;
                    rect.Height *= Scale;

                    Raylib.DrawRectanglePro(rect, Vector2.Zero, 0f, Color.Blue.Alpha(0.5f));
                }
                
                Raylib.EndMode2D();
            }

            Raylib.EndTextureMode();
        }

        private void DrawGrid()
        {
            for (int row = 0; row <= World.Rows; row++)
            {
                int y = row * Tile.Size;
                Raylib.DrawLine(0, y, Editor.World.Cols * Tile.Size, y, Color.Gray);
            }

            for (int col = 0; col <= Editor.World.Cols; col++)
            {
                int x = col * Tile.Size;
                Raylib.DrawLine(x, 0, x, World.Rows * Tile.Size, Color.Gray);
            }
        }

        protected override void OnRender()
        {
            if (_firstRender)
            {
                //_camera.Target.Y = -Tile.Size * 2f;
                //_camera.Target.X = -Tile.Size * 4f;
                _firstRender = false;
            }
            
            Gui.Text($"({_tilePos.X},{_tilePos.Y})");

            RenderTexDrawPosition = Gui.GetWindowPos() + Gui.GetCursorPos();
            RlImGui.ImageRenderTexture(WorldContentTex);
        }
    }
}
