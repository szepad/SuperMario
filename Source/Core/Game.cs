global using Raylib_cs;
global using System.Numerics;

namespace Mario2
{
    public class Game
    {
        public const string Title = "Mario 2";
        public const int Width = 256 * TextureScale;
        public const int Height = 224 * TextureScale;
        
        public const int TextureScale = 4;

        public bool AllowDevMode { get; private set; }
        public bool IsDevModeOn { get; private set; }

        public Color LetterBoxColor { get; set; } = Color.Black;
        public StretchMode StretchMode { get; set; }

        public GameSettings Settings { get; } = new();
        public Scene? ActiveScene { get; private set; }
        
        private bool _hasExited;

        private RenderTexture2D _virtualScreen;

        private static Game? _instance;
        public static Game Instance
        {
            get
            {
                return _instance ??= new Game();
            }
        }

        private Game() { }

        public void Run(string[] args)
        {
            bool isCommand = CommandLine.HasCommand(args);

            Init(hidden: isCommand);
            Load();

            if (isCommand)
            {
                CommandLine.TryRun(args);
                Unload();
                return;
            }

            LoadScene(new TitleScene());

            while (!Raylib.WindowShouldClose() && !_hasExited)
            {
                Update();
                Draw();
            }

            Unload();
        }

        public void Exit()
        {
            _hasExited = true;
        }

        public void LoadScene(Scene? scene)
        {
            ActiveScene?.Unload();
            ActiveScene = scene;
            ActiveScene?.Load();
        }

        private void Init(bool hidden)
        {
            Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
            Raylib.SetConfigFlags(hidden ? ConfigFlags.HiddenWindow : ConfigFlags.ResizableWindow);
            Raylib.InitWindow(Width, Height, Title);
            Raylib.InitAudioDevice();
            Raylib.SetExitKey(KeyboardKey.Null);
            
            Raylib.SetWindowState(ConfigFlags.VSyncHint);
            Raylib.SetTargetFPS(0);

            if (!OperatingSystem.IsMacOS())
            {
                Image icon = Raylib.LoadImage(Path.Combine(AppPaths.ResFolder, "icon.png"));
                Raylib.SetWindowIcon(icon);
                Raylib.UnloadImage(icon);
            }

            StretchMode = StretchMode.LetterBox;

#if DEBUG
            AllowDevMode = true;
#endif
        }

        private void Load()
        {
            Assets.Load();
            _virtualScreen = Raylib.LoadRenderTexture(Width, Height);

            GameSettingsLoader.Load(Settings);
        }

        private void Unload()
        {
            GameSettingsLoader.Save(Settings);

            Assets.Unload();
            Raylib.UnloadRenderTexture(_virtualScreen);
            
            LoadScene(null);

            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }

        private void OnResize()
        {
            
        }
        
        private void Update()
        {
            if (AllowDevMode)
            {
                if (Input.IsKeyPressed(KeyboardKey.F10))
                {
                    IsDevModeOn = !IsDevModeOn;
                }

                if (Input.IsKeyPressed(KeyboardKey.Grave)) Time.DebugTimeScale = 0f;
                else if (Input.IsKeyPressed(KeyboardKey.One)) Time.DebugTimeScale = 1f;
                else if (Input.IsKeyPressed(KeyboardKey.Two)) Time.DebugTimeScale = 0.5f;
                else if (Input.IsKeyPressed(KeyboardKey.Three)) Time.DebugTimeScale = 1f / 3f;
                else if (Input.IsKeyPressed(KeyboardKey.Four)) Time.DebugTimeScale = 1f / 10f;
                else if (Input.IsKeyPressed(KeyboardKey.Five)) Time.DebugTimeScale = 1f / 50f;
                else if (Input.IsKeyPressed(KeyboardKey.Six)) Time.DebugTimeScale = 2f;
                else if (Input.IsKeyPressed(KeyboardKey.Seven)) Time.DebugTimeScale = 5f;
                else if (Input.IsKeyPressed(KeyboardKey.Eight)) Time.DebugTimeScale = 50f;
            }

            Time.Progress();
            ActiveScene?.Update();
            AnimatedTile.UpdateSpriteInstances();
            SoundManager.UpdateCurrentMusic();

            if (Raylib.IsKeyPressed(KeyboardKey.F11))
            {
                Settings.SetFullscreen(!Settings.IsFullscreen);
            }

            if (Raylib.IsWindowResized())
            {
                OnResize();
            }
        }

        private void Draw()
        {
            // Scenes like the editor render at window resolution instead of the virtual screen
            if (ActiveScene?.ActiveLeaf.DrawsToWindow == true)
            {
                ActiveScene.PreDraw();
                ActiveScene.Draw();
                return;
            }

            ActiveScene?.PreDraw();

            Raylib.BeginTextureMode(_virtualScreen);
            ActiveScene?.Draw();
            Raylib.EndTextureMode();

            Raylib.ClearBackground(LetterBoxColor);

            // Flip height for opengl reasons
            Rectangle src = new(0, 0, Width, -Height);
            Rectangle dst = StretchMode switch
            {
                StretchMode.Stretch => GameViewHandler.GetStretchView(),
                StretchMode.LetterBox => GameViewHandler.GetLetterBoxView(),
                _ => GameViewHandler.GetNormalView(),
            };

            Raylib.DrawTexturePro(_virtualScreen.Texture, src, dst, Vector2.Zero, 0f, Color.White);

            if (IsDevModeOn)
            {
                Raylib.DrawText($"Fps: {Raylib.GetFPS()}", 0, 0, 30, Color.White);
            }

            Raylib.EndDrawing();
        }
    }
}
