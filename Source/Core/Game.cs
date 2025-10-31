global using System.Numerics;
global using Raylib_cs;

namespace Mario2
{
    public class Game
    {
        public const int Width = 256;
        public const int Height = 224;
        public const string Title = "Mario 2";

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
            Raylib.InitWindow(Width * 4, Height * 4, Title);
            Raylib.InitAudioDevice();
            Raylib.SetExitKey(KeyboardKey.Null);

            Raylib.SetWindowState(ConfigFlags.VSyncHint);
            Raylib.SetTargetFPS(0);
        }

        private void Load()
        {
            Assets.Load("../../../res");
            _virtualScreen = Raylib.LoadRenderTexture(Width, Height);
            LoadScene(new GameScene());
        }

        private void Unload()
        {
            Assets.Unload();
            Raylib.UnloadRenderTexture(_virtualScreen);
            LoadScene(null);
        }

        private void Update()
        {
            Time.Progress();
            _activeScene?.Update();
        }

        private void Draw()
        {
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
