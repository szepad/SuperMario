using Mario2.Editor;
using NativeFileDialogSharp;

namespace Mario2
{
    public class EditorScene : Scene
    {
        public World World { get; } = new();
        public EditorOptions Options { get; } = new();
        public EditorFileManager FileManager { get; }

        public EditorMenuBar MenuBar { get; private set; }
        public WorldWindow WorldWindow { get; private set; }
        public TileWindow TileWindow { get; private set; }
        public PropertyWindow PropertyWindow { get; private set; }

        public string FileName { get; private set; } = string.Empty;
        public bool IsFileDirty { get; set; }

        private readonly List<EditorWindow> _windowRenderQueue = [];

        private bool _exitToTitle;

        public const string DefaultLayout = "default.ini";
        private bool _loadLayout;
        private string _nextLayoutPath = string.Empty;

        private bool _askSave;
        private Action? _askSaveAction;

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

        public void AskSave(Action? afterSaving)
        {
            _askSave = true;
            _askSaveAction = afterSaving;
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
                Game.Instance.LoadScene(new TitleScene());
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
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                QueueTitleScene();
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
            if (Gui.BeginPopupModal("Unsaved Level", ImGuiWindowFlags.NoResize))
            {
                Gui.TextWrapped("Would you like to save before moving on?");

                if (Gui.Button("Save"))
                {
                    Gui.CloseCurrentPopup();
                    FileManager.Save();
                    closed = true;
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
                _askSaveAction?.Invoke();
                _askSaveAction = null;
            }
        }
    }
}
