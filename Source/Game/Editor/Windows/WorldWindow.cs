namespace Mario2.Editor
{
    public class WorldWindow : EditorWindow
    {
        public Vector2 RenderTexDrawPosition { get; private set; }
        public RenderTexture2D WorldContentTex { get; set; }

        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 0.4f);

        private bool _isPanning;
        private Vector2 _panStartPos;

        private bool _firstRender = true;

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

        public override void Update()
        {
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

            Vector2 screenPos = ScreenToWorld(GetMousePositionInsideEditor());
            int tx = (int)(screenPos.X / Tile.Size);
            int ty = (int)(screenPos.Y / Tile.Size);

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Editor.World.SetTile(tx, ty, Editor.TileWindow.GetSelected(Editor.World, tx, ty));
                UpdateRule(Editor.World, tx, ty);
                Editor.Dirty();
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                Editor.World.SetTile(tx, ty, SimpleTile.Air);
                UpdateRule(Editor.World, tx, ty);
                Editor.Dirty();
            }
        }

        public void AssignRuleTileRefs()
        {
            World world = Editor.World;
            for (int row = 0; row < World.Rows; row++)
            {
                for (int col = 0; col < world.Cols; col++)
                {
                    if (!world.GetTile(col, row, out Tile tile) || tile is not SimpleTile st)
                        continue;

                    foreach (TileObject selectable in TileWindow.Selectables)
                    {
                        if (selectable is RuleTile ruleTile
                            && ruleTile.Rules.ContainsValue(st.Id))
                        {
                            tile.Rule = ruleTile;
                            break;
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

        public void RenderWorld()
        {
            Raylib.BeginTextureMode(WorldContentTex);

            //Raylib.ClearBackground(Editor.World.Parallax.ClearColor);
            Raylib.ClearBackground(Color.Black);

            if (!_firstRender)
            {
                Raylib.BeginMode2D(_camera);
                if (Editor.Options.ShowGrid) DrawGrid();
                Editor.World.Draw(editor: true);
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
                _camera.Target.Y = -Tile.Size * 2f;
                _camera.Target.X = -Tile.Size * 4f;
                _firstRender = false;
            }

            RenderTexDrawPosition = Gui.GetWindowPos() + Gui.GetCursorPos();
            RlImGui.ImageRenderTexture(WorldContentTex);
        }
    }
}
