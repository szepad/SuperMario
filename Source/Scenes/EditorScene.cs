using RlImGui = rlImGui_cs.rlImGui;
using Gui = ImGuiNET.ImGui;
using ImGuiNET;

namespace Mario2
{
    public class EditorScene : Scene
    {
        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);
        private World _world = new();
        private bool _exitToTitle;

        private static bool _showGrid = true;

        private bool _isPanning;
        private Vector2 _panStartPos;

        private Vector2 _editorWindowPos;
        private RenderTexture2D _editorWindowContent;
        private bool _editorWindowHovered;

        private int _selectedTile = 0;
        private float _tileSizeScale = 1f;

        private int _colsInputVal = 0;

        private const string DefaultLayout = "default.ini";
        private bool _loadLayout;
        private string _nextLayoutPath = string.Empty;

        protected override void OnLoad()
        {
            _world = new World();
            _editorWindowContent = Raylib.LoadRenderTexture(
                Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()), 
                Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()));

            RlImGui.Setup(enableDocking: true);

            if (!File.Exists("imgui.ini"))
            {
                QueueLoadLayout(DefaultLayout);
            }
        }

        protected override void OnUnload()
        {
            Raylib.UnloadRenderTexture(_editorWindowContent);
            RlImGui.Shutdown();
        }

        private Vector2 GetMousePositionInsideEditor() 
            => Raylib.GetMousePosition() - _editorWindowPos;

        private Vector2 ScreenToWorld(Vector2 screenPos)
            => (screenPos + _camera.Target * _camera.Zoom) / _camera.Zoom;

        public override void Update()
        {
            HandleGeneralControls();
            HandleZooming();
            HandlePanning();
            HandleEditing();

            if (_loadLayout)
            {
                LoadNextLayout();
                _loadLayout = false;
            }

            if (_exitToTitle)
            {
                Game.Instance.LoadScene(new TitleScene());
            }
        }

        private void HandleGeneralControls()
        {
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                QueueLoadTitleScene();
            }
        }

        private void QueueLoadTitleScene()
        {
            _exitToTitle = true;
        }

        private void HandleZooming()
        {
            if (!_editorWindowHovered)
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
            if (!_editorWindowHovered)
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
            if (!_editorWindowHovered)
                return;

            Vector2 screenPos = ScreenToWorld(GetMousePositionInsideEditor());
            int tx = (int)(screenPos.X / Tile.Size);
            int ty = (int)(screenPos.Y / Tile.Size);
            
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                _world.SetTile(tx, ty, new SimpleTile(_selectedTile, false));
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Right))
            {
                _world.SetTile(tx, ty, SimpleTile.Air);
            }
        }

        public override void Draw()
        {
            Raylib.BeginDrawing();

            Raylib.BeginTextureMode(_editorWindowContent);
            DrawEditorWindowContent();
            Raylib.EndTextureMode();

            RlImGui.Begin();

            Gui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TabDimmed, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TabDimmedSelected, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TabDimmedSelectedOverline, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.Tab, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TabSelected, new Vector4(0, 0, 0, 1));
            Gui.PushStyleColor(ImGuiCol.TabSelectedOverline, new Vector4(0, 0, 0, 1));

            Gui.DockSpaceOverViewport();
            Gui.ShowDemoWindow();
            
            DrawMenuBar();
            DrawEditorWindow();
            DrawTilesetWindow();
            DrawPropertiesWindow();
            DrawEntitiesWindow();

            Gui.PopStyleColor(9);

            RlImGui.End();
            Raylib.EndDrawing();
        }

        private void DrawEditorWindowContent()
        {
            Raylib.ClearBackground(Color.Black);

            Raylib.BeginMode2D(_camera);
            _world.Draw();
            if (_showGrid) DrawGrid();
            Raylib.EndMode2D();
        }

        private void DrawGrid()
        {
            for (int row = 0; row <= _world.Rows; row++)
            {
                int y = row * Tile.Size;
                Raylib.DrawLine(0, y, _world.Cols * Tile.Size, y, Color.Gray);
            }

            for (int col = 0; col <= _world.Cols; col++)
            {
                int x = col * Tile.Size;
                Raylib.DrawLine(x, 0, x, _world.Rows * Tile.Size, Color.Gray);
            }
        }

        private void DrawMenuBar()
        {
            Gui.BeginMainMenuBar();

            if (Gui.BeginMenu("File"))
            {
                if (Gui.MenuItem("New", "Ctrl+N"))
                {

                }
                if (Gui.MenuItem("Open", "Ctrl+O"))
                {

                }
                if (Gui.MenuItem("Save", "Ctrl+S"))
                {

                }
                if (Gui.MenuItem("Save As", "Ctrl+Shift+S"))
                {

                }

                Gui.Separator();
                if (Gui.MenuItem("Back", "Escape"))
                {
                    QueueLoadTitleScene();
                }
                if (Gui.MenuItem("Exit", "Alt+F4"))
                {
                    Game.Instance.Exit();
                }

                Gui.EndMenu();
            }
            if (Gui.BeginMenu("View"))
            {
                Gui.MenuItem("Show Grid", string.Empty, ref _showGrid);
                Gui.SeparatorText("Layout Presets");

                if (Gui.MenuItem("Default"))
                {
                    QueueLoadLayout(DefaultLayout);
                }

                Gui.EndMenu();
            }

            Gui.EndMainMenuBar();
        }

        private void DrawEditorWindow()
        {
            Gui.Begin("Editor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            _editorWindowHovered = Gui.IsWindowHovered();
            _editorWindowPos = Gui.GetWindowPos() + Gui.GetCursorPos();
            RlImGui.ImageRenderTexture(_editorWindowContent);
            Gui.End();
        }
        
        private void DrawTilesetWindow()
        {
            Texture2D tileset = Assets.GetTex("tileset");
            const int TileCount = 120;
            const int TileSizePx = 16;

            Gui.Begin("Tileset");

            Gui.SetNextItemWidth(64f);
            Gui.SliderFloat("Zoom", ref _tileSizeScale, 1f, 3f, "");

            int itemSize = (int)(32f * _tileSizeScale);
            int cols = (int)(Gui.GetWindowWidth() / (itemSize + 9));
            if (cols <= 0) cols = 1;
            int rows = (int)MathF.Ceiling(TileCount / (float)cols);
            int itemIndex = 0;

            if (Gui.BeginTable("tiles", cols, ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.NoPadInnerX))
            {
                for (int row = 0; row < rows; row++)
                {
                    Gui.TableNextRow();
                    for (int col = 0; col < cols; col++)
                    {
                        Gui.TableSetColumnIndex(col);

                        if (itemIndex >= TileCount)
                            break;

                        if (_selectedTile == itemIndex)
                        {
                            Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
                            Gui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                            Gui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0, 0, 1));
                            DrawTileButton(itemIndex, itemSize, TileSizePx, tileset);
                            Gui.PopStyleColor(3);
                        }
                        else
                        {
                            Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 0));
                            DrawTileButton(itemIndex, itemSize, TileSizePx, tileset);
                            Gui.PopStyleColor(1);
                        }

                        itemIndex++;
                    }
                }

                Gui.EndTable();
            }

            Gui.End();
        }

        private void DrawTileButton(int index, int displaySize, int tileSizePx, Texture2D tileset)
        {
            int cols = (tileset.Width / tileSizePx);
            int col = index % cols;
            int row = index / cols;

            Rectangle src = new(col * tileSizePx, row * tileSizePx, tileSizePx, tileSizePx);
            Vector2 uv0 = new(src.X / tileset.Width, src.Y / tileset.Height);
            Vector2 uv1 = uv0 + new Vector2(1f / (tileset.Width / src.Width), 1f / (tileset.Height / src.Height));

            if (Gui.ImageButton($"tile,{col},{row}", (nint)tileset.Id, new Vector2(displaySize), uv0, uv1))
            {
                _selectedTile = index;
            }
        }

        private void DrawPropertiesWindow()
        {
            Gui.Begin("Properties");

            Gui.SeparatorText("World");
            Gui.SetNextItemWidth(100f);

            Gui.Text($"Cols: {_world.Cols}");
            Gui.SameLine();
            if (Gui.Button("Change"))
            {
                _colsInputVal = _world.Cols;
                Gui.OpenPopup("Change Columns");
            }

            Vector2 center = Gui.GetMainViewport().GetCenter();
            Gui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f));
            if (Gui.BeginPopupModal("Change Columns"))
            {
                Gui.InputInt("##1", ref _colsInputVal);
                if (Gui.Button("Apply"))
                {
                    _world.SetCols(_colsInputVal);
                    Gui.CloseCurrentPopup();
                }

                Gui.SameLine();
                if (Gui.Button("Cancel")) Gui.CloseCurrentPopup();

                Gui.EndPopup();
            }

            Gui.Text($"Rows: {_world.Rows}");
            Gui.SameLine();
            Gui.BeginDisabled();
            Gui.Button("Change##2");
            Gui.EndDisabled();

            Gui.Dummy(new Vector2(0f, 20f));
            Gui.SeparatorText("Tile");
            bool a = false;
            Gui.Checkbox("Collidable", ref a);

            Gui.End();
        }

        private void DrawEntitiesWindow()
        {
            Gui.Begin("Entities");
            Gui.End();
        }

        private void QueueLoadLayout(string path)
        {
            if (!File.Exists(path))
                return;

            _loadLayout = true;
            _nextLayoutPath = path;
        }
        
        private void LoadNextLayout()
        {
            if (!File.Exists(_nextLayoutPath))
                return;

            Gui.LoadIniSettingsFromDisk(_nextLayoutPath);
        }
    }
}
