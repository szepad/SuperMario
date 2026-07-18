using Mario2.Editor;

namespace Mario2
{
    public class EditorScene : Scene
    {
        public World World { get; set; } = new();
        public EditorOptions Options { get; } = new();
        public EditorFileManager FileManager { get; }

        public EditorMenuBar MenuBar { get; private set; }
        
        public WorldWindow WorldWindow { get; private set; }
        public TileWindow TileWindow { get; private set; }
        public TilesetRawWindow TilesetRawWindow { get; private set; }
        public EditorWindow ActiveSelector { get; private set; }

        public EntityWindow EntityWindow { get; private set; }
        public PropertyWindow PropertyWindow { get; private set; }

        public static string RecentlyOpened { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsFileDirty { get; private set; }

        private readonly List<EditorWindow> _windowRenderQueue = [];

        private bool _exitToTitle;
        
        private bool _loadLayout;
        private string _nextLayoutPath = string.Empty;

        private bool _askSave;
        private bool _executeSaveAction;
        private Action? _askSaveAction;
        private bool _askSaveCanDiscard;

        private bool _showError;
        private string _errorMessage = string.Empty;

        private const MusicName EditorMusic = MusicName.None;
        
        private static IntPtr _imguiIniPointer = IntPtr.Zero;

        public EditorScene()
        {
            FileManager = new(this);
            MenuBar = new EditorMenuBar(this);

            WorldWindow = new WorldWindow(this);
            TileWindow = new TileWindow(this);
            TilesetRawWindow = new TilesetRawWindow(this);
            EntityWindow = new EntityWindow(this);
            PropertyWindow = new PropertyWindow(this);

            _windowRenderQueue.Add(WorldWindow);
            _windowRenderQueue.Add(TileWindow);
            _windowRenderQueue.Add(TilesetRawWindow);
            _windowRenderQueue.Add(EntityWindow);
            _windowRenderQueue.Add(PropertyWindow);

            ActiveSelector = TileWindow;
        }

        protected override void OnLoad()
        {
            Raylib.ShowCursor();
            SoundManager.PlayMusic(EditorMusic);

            WorldWindow.WorldContentTex = Raylib.LoadRenderTexture(
                Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()),
                Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()));

            RlImGui.Setup(enableDocking: true);
            SetupIniFileSaveLocation();
            EditorThemeManager.ApplyDefault();

            if (!File.Exists(AppPaths.ImGuiIniFilePath))
            {
                QueueLoadLayout(AppPaths.DefaultLayout);
            }

            if (RecentlyOpened != string.Empty)
            {
                FileManager.Open(RecentlyOpened);
            }
        }
        
        protected override void OnUnload()
        {
            Raylib.UnloadRenderTexture(WorldWindow.WorldContentTex);
            FreeIniFileSaveLocation();
            RlImGui.Shutdown();
        }

        private static void SetupIniFileSaveLocation()
        {
            _imguiIniPointer = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(AppPaths.ImGuiIniFilePath);
            
            unsafe
            {
                Gui.GetIO().NativePtr->IniFilename = (byte*)_imguiIniPointer;
            }
        }

        private static void FreeIniFileSaveLocation()
        {
            if (_imguiIniPointer != IntPtr.Zero)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(_imguiIniPointer);
                _imguiIniPointer = IntPtr.Zero;
            }
        }

        protected override void OnSubSceneUnload()
        {
            Raylib.ShowCursor();
            SoundManager.PlayMusic(EditorMusic);
        }

        public void SetActiveSelector(EditorWindow selector)
        {
            ActiveSelector = selector;
        }

        public void Dirty()
        {
            IsFileDirty = true;
        }

        public void CleanDirty()
        {
            IsFileDirty = false;
        }

        public void AskSave(Action? afterSaving, bool canDiscard = true)
        {
            if (!IsFileDirty)
            {
                afterSaving?.Invoke();
                return;
            }

            _askSave = true;
            _executeSaveAction = false;
            _askSaveAction = afterSaving;
            _askSaveCanDiscard = canDiscard;
        }

        public void ShowError(string message)
        {
            _showError = true;
            _errorMessage = message;
        }

        public void QueueTitleScene()
        {
            _exitToTitle = true;
        }

        public void QueueLoadLayout(string path)
        {
            _loadLayout = true;
            _nextLayoutPath = path;
        }

        protected override void OnUpdate()
        {
            if (_loadLayout)
            {
                LoadNextLayout();
                _loadLayout = false;
            }

            foreach (Entity entity in World.Entities)
            {
                if (!entity.IsActive)
                {
                    entity.Activate();
                }
            }

            World.UpdateParallax(new Camera2D(Vector2.Zero, Vector2.Zero, 0f, 1f));
            foreach (EditorWindow window in _windowRenderQueue)
            {
                window.Update();
            }

            if (_exitToTitle)
            {
                AskSave(() => Game.Instance.LoadScene(new TitleScene()));
                _exitToTitle = false;
            }

            if (_executeSaveAction)
            {
                _askSaveAction?.Invoke();
                _askSaveAction = null;
                _executeSaveAction = false;
            }

            HandleGeneralControls();
        }

        private void LoadNextLayout()
        {
            if (!File.Exists(_nextLayoutPath))
                return;

            Gui.LoadIniSettingsFromDisk(_nextLayoutPath);
        }

        private void HandleGeneralControls()
        {
            // TODO: Disable when modal popup is active (ask save / error)
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                QueueTitleScene();
            }

            if (Input.IsKeyDown(KeyboardKey.LeftControl))
            {
                if (Input.IsKeyPressed(KeyboardKey.N))
                {
                    FileManager.New();
                }

                if (Input.IsKeyPressed(KeyboardKey.O))
                {
                    FileManager.AskOpen();
                }

                if (Input.IsKeyPressed(KeyboardKey.S))
                {
                    if (Input.IsKeyDown(KeyboardKey.LeftShift))
                    {
                        FileManager.SaveAs();
                    }
                    else
                    {
                        FileManager.Save();
                    }
                }
            }

            if (Input.IsKeyPressed(KeyboardKey.Enter))
            {
                if (FilePath == string.Empty && !IsFileDirty)
                {
                    Dirty();
                }
                
                AskSave(() => LoadSubScene(new GameScene(FilePath)), canDiscard: false);
            }
        }

        protected override void OnDraw()
        {
            Raylib.BeginDrawing();

            WorldWindow.RenderWorld();

            RlImGui.Begin();

            Vector4 tabCol = new(35 / 255f, 67 / 255f, 108 / 255f, 1f);
            Vector4 tabColDarker = new(tabCol.X * 0.67f, tabCol.Y * 0.67f, tabCol.Z * 0.67f, tabCol.W);
            Vector4 titleCol = new(tabCol.X * 0.33f, tabCol.Y * 0.33f, tabCol.Z * 0.33f, tabCol.W);
            
            Gui.PushStyleColor(ImGuiCol.TitleBg, titleCol);
            Gui.PushStyleColor(ImGuiCol.TitleBgActive, titleCol);
            Gui.PushStyleColor(ImGuiCol.TitleBgCollapsed, titleCol);

            Gui.PushStyleColor(ImGuiCol.Tab, tabColDarker);
            Gui.PushStyleColor(ImGuiCol.TabDimmed, tabColDarker);
            Gui.PushStyleColor(ImGuiCol.TabSelected, tabCol);
            Gui.PushStyleColor(ImGuiCol.TabDimmedSelected, tabCol);

            Gui.PushStyleColor(ImGuiCol.TabDimmedSelectedOverline, Vector4.Zero);
            Gui.PushStyleColor(ImGuiCol.TabSelectedOverline, Vector4.Zero);

            Gui.DockSpaceOverViewport();
            //Gui.ShowDemoWindow();

            MenuBar.Render();
            foreach (EditorWindow window in _windowRenderQueue)
            {
                window.Render();
            }

            Gui.PopStyleColor(9);

            HandleAskSave();
            HandleError();

            RlImGui.End();
            Raylib.EndDrawing();
        }

        private void HandleAskSave()
        {
            if (_askSave)
            {
                Gui.OpenPopup("Unsaved Level");
                _askSave = false;
            }

            bool cancel = false;
            bool closed = false;
            Vector2 center = Gui.GetMainViewport().GetCenter();
            Gui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f));
            if (Gui.BeginPopupModal("Unsaved Level", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                Gui.Text("Would you like to save before moving on?");

                if (Gui.Button("Save"))
                {
                    Gui.CloseCurrentPopup();
                    closed = true;
                    if (!FileManager.Save())
                    {
                        cancel = true;
                    }
                }

                if (_askSaveCanDiscard)
                {
                    Gui.SameLine();
                    if (Gui.Button("Discard"))
                    {
                        Gui.CloseCurrentPopup();
                        closed = true;
                    }
                }

                Gui.SameLine();
                if (Gui.Button("Cancel"))
                {
                    Gui.CloseCurrentPopup();
                    closed = true;
                    cancel = true;
                }

                Gui.EndPopup();
            }

            if (closed && !cancel)
            {
                _executeSaveAction = true;
            }
        }

        private void HandleError()
        {
            if (_showError)
            {
                Gui.OpenPopup("Error!");
                _showError = false;
            }

            Vector2 center = Gui.GetMainViewport().GetCenter();
            Gui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f));
            if (Gui.BeginPopupModal("Error!", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings))
            {
                Gui.Text(_errorMessage);

                if (Gui.Button("Ok"))
                {
                    Gui.CloseCurrentPopup();
                }

                Gui.EndPopup();
            }

            //Gui.PopStyleVar(1);
        }
    }
}
