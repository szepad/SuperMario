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
            SoundManager.StopMusic();
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
            _world.HandleCameraFollow(ref _camera);
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
                return;
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
                _world.Start();
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
            DrawHud();

            const int OffsetYFromOriginal = 16;
            Text.Draw($"WORLD {SessionManager.CurrentWorld}-{SessionManager.CurrentLevelDisplay}",
                new Vector2(88f, 63f + OffsetYFromOriginal).Scale());

            Raylib.DrawTexturePro(Assets.GetTex("entity_selector"), new Rectangle(0, 0, 16, 16),
                new Rectangle(new Vector2(97f + 4f, 80f + OffsetYFromOriginal).Scale(), new Vector2(16f).Scale()), Vector2.Zero, 0f, Color.White);

            Text.Draw($"*{"1".PadLeft(2, ' ')}",
                new Vector2(128f, 87f + OffsetYFromOriginal).Scale());
        }

        private void DrawGameplayState()
        {
            Raylib.ClearBackground(_world.Parallax.ClearColor);
            _world.DrawBackground();
            DrawHud();
            Raylib.BeginMode2D(_camera);
            _world.Draw(_camera);
            Raylib.EndMode2D();
        }

        private void DrawHud()
        {
            const int Row1 = 14 * Game.TextureScale;
            const int Row2 = 23 * Game.TextureScale;

            Text.Draw("MARIO", 24f.Scale(), Row1);
            Text.Draw(SessionManager.Score.ToString().PadLeft(6, '0'), 24f.Scale(), Row2);

            AnimatedTile.HudCoinSprite.Draw(new Vector2(88f.Scale(), Row2));
            Text.Draw(SessionManager.Coins.ToString().PadLeft(2, '0'), 104f.Scale(), Row2);

            Text.Draw("WORLD", 144f.Scale(), Row1);
            Text.Draw(SessionManager.CurrentWorld.ToString(), 160f.Scale(), Row2, Pivot.TopRight);
            Text.Draw($"-{SessionManager.CurrentLevelDisplay}", 160f.Scale(), Row2);

            Text.Draw("TIME", 200f.Scale(), Row1);
            Text.Draw(_world.TimeLeft.ToString().PadLeft(3, '0'), 208f.Scale(), Row2);
        }
    }
}
