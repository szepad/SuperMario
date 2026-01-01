using Mario2.Editor;
using NativeFileDialogSharp;

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
        public PropertyWindow PropertyWindow { get; private set; }

        public string FilePath { get; set; } = string.Empty;
        public bool IsFileDirty { get; private set; }

        private readonly List<EditorWindow> _windowRenderQueue = [];

        private bool _exitToTitle;

        public const string DefaultLayout = "default.ini";
        private bool _loadLayout;
        private string _nextLayoutPath = string.Empty;

        private bool _askSave;
        private bool _executeSaveAction;
        private Action? _askSaveAction;

        private bool _showError;
        private string _errorMessage = string.Empty;

        public EditorScene()
        {
            FileManager = new(this);
            MenuBar = new EditorMenuBar(this);

            WorldWindow = new WorldWindow(this);
            TileWindow = new TileWindow(this);
            PropertyWindow = new PropertyWindow(this);

            _windowRenderQueue.Add(WorldWindow);
            _windowRenderQueue.Add(TileWindow);
            _windowRenderQueue.Add(PropertyWindow);
        }

        protected override void OnLoad()
        {
            WorldWindow.WorldContentTex = Raylib.LoadRenderTexture(
                Raylib.GetMonitorWidth(Raylib.GetCurrentMonitor()),
                Raylib.GetMonitorHeight(Raylib.GetCurrentMonitor()));

            RlImGui.Setup(enableDocking: true);
            EditorThemeManager.ApplyDefault();

            if (!File.Exists("imgui.ini"))
            {
                QueueLoadLayout(DefaultLayout);
            }
        }

        protected override void OnUnload()
        {
            Raylib.UnloadRenderTexture(WorldWindow.WorldContentTex);
            RlImGui.Shutdown();
        }

        public void Dirty()
        {
            IsFileDirty = true;
        }

        public void CleanDirty()
        {
            IsFileDirty = false;
        }

        public void AskSave(Action? afterSaving)
        {
            if (!IsFileDirty)
            {
                afterSaving?.Invoke();
                return;
            }

            _askSave = true;
            _executeSaveAction = false;
            _askSaveAction = afterSaving;
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

        public override void Update()
        {
            if (_loadLayout)
            {
                LoadNextLayout();
                _loadLayout = false;
            }
            
            HandleGeneralControls();
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
                    FileManager.Open();
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
        }

        public override void Draw()
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
            Gui.ShowDemoWindow();

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

                Gui.SameLine();
                if (Gui.Button("Discard"))
                {
                    Gui.CloseCurrentPopup();
                    closed = true;
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
