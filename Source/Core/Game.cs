global using System.Numerics;
global using Raylib_cs;
global using RlImGui = rlImGui_cs.rlImGui;
global using Gui = ImGuiNET.ImGui;
global using ImGuiNET;

namespace Mario2
{
    public class Game
    {
        public const int Width = 1280;//256;
        public const int Height = 720;//224;
        public const int TexScale = 4;
        public const string Title = "Mario 2";

        public bool IsDevMode { get; private set; }

        private bool _hasExited;

        private Scene? _activeScene;

        private RenderTexture2D _virtualScreen;

        #region Singleton
        private static Game? _instance;
        public static Game Instance
        {
            get
            {
                return _instance ??= new Game();
            }
        }

        private Game() { }
        #endregion

        public void Run()
        {
            Init();
            Load();
            while (!Raylib.WindowShouldClose() && !_hasExited)
            {
                Update();
                Draw();
            }

            Unload();
            Raylib.CloseWindow();
        }

        public void Exit()
        {
            _hasExited = true;
        }

        public void LoadScene(Scene? scene)
        {
            _activeScene?.Unload();
            _activeScene = scene;
            _activeScene?.Load();
        }

        private void Init()
        {
            Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(Width, Height, Title);
            Raylib.InitAudioDevice();
            Raylib.SetExitKey(KeyboardKey.Null);

            Raylib.SetWindowState(ConfigFlags.VSyncHint);
            Raylib.SetTargetFPS(0);
        }

        private void Load()
        {
            Assets.Load("../../../res");
            _virtualScreen = Raylib.LoadRenderTexture(Width, Height);
            LoadScene(new EditorScene());
        }

        private void Unload()
        {
            Assets.Unload();
            Raylib.UnloadRenderTexture(_virtualScreen);
            LoadScene(null);
        }

        private void Update()
        {
#if DEBUG
            if (Input.IsKeyPressed(KeyboardKey.F10))
            {
                IsDevMode = !IsDevMode;
            }

            if (Input.IsKeyPressed(KeyboardKey.Grave)) Time.TimeScale = 0f;
            else if (Input.IsKeyPressed(KeyboardKey.One)) Time.TimeScale = 1f;
            else if (Input.IsKeyPressed(KeyboardKey.Two)) Time.TimeScale = 0.5f;
            else if (Input.IsKeyPressed(KeyboardKey.Three)) Time.TimeScale = 1f / 3f;
            else if (Input.IsKeyPressed(KeyboardKey.Four)) Time.TimeScale = 1f / 10f;
            else if (Input.IsKeyPressed(KeyboardKey.Five)) Time.TimeScale = 1f / 50f;
#endif

            Time.Progress();
            _activeScene?.Update();
        }

        private void Draw()
        {
            if (_activeScene is EditorScene)
            {
                _activeScene.Draw();
                return;
            }

            _activeScene?.PreDraw();

            Raylib.BeginTextureMode(_virtualScreen);
            _activeScene?.Draw();
            Raylib.EndTextureMode();

            int sw = Raylib.GetScreenWidth();
            int sh = Raylib.GetScreenHeight();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexturePro(_virtualScreen.Texture,
                new Rectangle(0, 0, Width, -Height),
                new Rectangle(0, 0, sw, sh),
                Vector2.Zero, 0f, Color.White);
            Raylib.EndDrawing();
        }
    }
}
