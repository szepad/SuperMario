namespace Mario2
{
    public class GameScene(string path = "") : Scene
    {
        private enum State
        {
            Loading,
            Gameplay
        }

        private World _world = null!;
        private Camera2D _camera;

        private readonly bool _insideEditor = path != string.Empty;
        private readonly string _editorFilePath = path;

        private State _state = State.Loading;

        private const float LoadTime = 1f;
        private float _loadTimer;

        private const float PlayerDeathReloadTime = 5f;
        private float _playerDeathTimer;

        protected override void OnLoad()
        {
            Raylib.HideCursor();

            if (_insideEditor)
            {
                HandleEditorPlaytest();
                _state = State.Gameplay;
                return;
            }

            Load(() => SessionManager.LoadNext(out _world));
        }

        private void ResetCamera()
        {
            _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);
            _camera.Offset.X = Game.Width / 2f;
            _camera.Offset.Y = Game.Height - World.Rows * Tile.Size + Tile.Size;
        }

        protected override void OnUnload()
        {

        }

        private void HandleEditorPlaytest()
        {
            World? loaded = WorldLoader.Deserialize(_editorFilePath, out _);
            if (loaded is not null)
            {
                _world = loaded;
            }
            else if (ParentScene is null)
            {
                Game.Instance.LoadScene(new TitleScene());
            }
            else
            {
                ParentScene.UnloadSubScene();
            }

            ResetCamera();
        }

        private void Load(Func<SessionLoadResult> loader)
        {
            _state = State.Loading;
            _loadTimer = LoadTime;

            if (loader.Invoke() != SessionLoadResult.Success)
            {
                Game.Instance.LoadScene(new TitleScene());
            }

            _world.OnPlayerDeath += () => _playerDeathTimer = PlayerDeathReloadTime;
            ResetCamera();
        }

        protected override void OnUpdate()
        {
            switch (_state)
            {
                case State.Loading: UpdateLoadState(); break;
                case State.Gameplay: UpdateGameplayState(); break;
            }
        }

        private void UpdateLoadState()
        {
            _loadTimer -= Time.DeltaTime;
            if (_loadTimer <= 0f)
            {
                _state = State.Gameplay;
            }
        }

        private void UpdateGameplayState()
        {
            _world.HandleCameraFollow(ref _camera);
            _world.Update(_camera);
            _world.UpdateParallax(_camera);

            if (_playerDeathTimer > 0f)
            {
                _playerDeathTimer -= Time.DeltaTime;
                if (_playerDeathTimer <= 0f)
                {
                    Load(() => SessionManager.LoadCurrent(out _world));
                }
            }
            else if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                if (ParentScene is null) Game.Instance.LoadScene(new TitleScene());
                else ParentScene.UnloadSubScene();
            }
        }

        protected override void OnDraw()
        {
            switch (_state)
            {
                case State.Loading: DrawLoadState(); break;
                case State.Gameplay: DrawGameplayState(); break;
            }
        }

        private void DrawLoadState()
        {
            Raylib.ClearBackground(Color.Black);
            Text.Draw($"Level {SessionManager.CurrentLevel}", new Vector2(Game.Width / 2f, Game.Height * 0.4f),
                40, Color.White);
        }

        private void DrawGameplayState()
        {
            Raylib.ClearBackground(_world.Parallax.ClearColor);
            _world.DrawBackground();
            Raylib.BeginMode2D(_camera);
            _world.Draw(_camera);
            Raylib.EndMode2D();
        }
    }
}
