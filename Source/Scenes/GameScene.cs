namespace Mario2
{
    public class GameScene(string path = "") : Scene
    {
        public const int CameraOffsetY = Tile.Size;

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

        private const float LoadTime = 2.2f;
        private float _loadTimer;

        private const float PlayerDeathReloadTime = 5f;
        private float _playerDeathTimer;

        private readonly Dictionary<string, World> _loadedSubWorlds = new();

        protected override void OnLoad()
        {
            SoundManager.StopMusic();
            Raylib.HideCursor();

            if (_insideEditor)
            {
                HandleEditorPlaytest();
                _state = State.Gameplay;
            }
            else
            {
                LoadNext();
            }
        }

        private void ResetCamera()
        {
            _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);
            _camera.Offset.X = Game.Width / 2f;
            _camera.Offset.Y = CameraOffsetY;
            _world.HandleCameraFollow(ref _camera);
            _world.UpdateParallax(_camera);
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
                _loadedSubWorlds[Path.GetFullPath(_world.LoadedPath)] = _world;
                AssignWorldEvents();
                StartWorld();
            }
            else if (ParentScene is null)
            {
                Game.Instance.LoadScene(new TitleScene());
            }
            else
            {
                ParentScene.UnloadSubScene();
            }
        }

        private void Load(Func<SessionLoadResult> loader)
        {
            SoundManager.StopMusic();
            _state = State.Loading;
            _loadTimer = LoadTime;
            _loadedSubWorlds.Clear();

            if (loader.Invoke() != SessionLoadResult.Success)
            {
                Game.Instance.LoadScene(new TitleScene());
            }
            else
            {
                _loadedSubWorlds[Path.GetFullPath(_world.LoadedPath)] = _world;
                AssignWorldEvents();
            }
        }

        private void LoadNext() => Load(() => SessionManager.LoadNext(out _world));
        private void Reload() => Load(() => SessionManager.Reload(out _world));

        private void AssignWorldEvents() 
        {
            _world.OnPlayerDeath += () => _playerDeathTimer = PlayerDeathReloadTime;
            _world.OnLevelEnd += World_OnLevelEnd;
            _world.OnPipeLoadSublevel += World_OnPipeLoadSublevel;
        }

        private void StartWorld()
        {
            if (_world.IsPipeIntro && !SessionManager.Reloaded)
            {
                _world.IsPipeIntro = false;
                string dest = _world.LoadedPath;
                if (!LoadSublevel(Path.Combine("levels", "pipe_intro.lvl")))
                {
                    StartWorld();
                    return;
                }

                foreach (Entity entity in _world.Entities)
                {
                    if (entity is WarpPipe pipe)
                    {
                        pipe.Destination = dest;
                        break;
                    }
                }

                _world.Player.StartCutscene(new PipeIntroCutscene(_world.Player));
            }
            else
            {
                _world.Start();
                ResetCamera();
            }
        }

        private void World_OnLevelEnd()
        {
            PlayerState playerState = _world.Player.State;
            LoadNext();
            _world.Player.SetStateInstantNoSideFx(playerState);
        }

        private void World_OnPipeLoadSublevel(WarpPipe pipe)
        {
            if (!LoadSublevel(pipe.Destination))
            {
                pipe.Destination = string.Empty;
            }

            _world.HandlePipeTransition(pipe);
            _world.SetCameraPosition(ref _camera, 0f);
        }

        private bool LoadSublevel(string path)
        {
            path = Path.GetFullPath(path);

            World prev = _world;
            if (_loadedSubWorlds.TryGetValue(path, out World? loaded))
            {
                _world = loaded;
            }
            else
            {
                World? world = WorldLoader.Deserialize(path, out _);
                if (world is null)
                {
                    return false;
                }

                _loadedSubWorlds[Path.GetFullPath(world.LoadedPath)] = world;
                world.ParentWorldPath = _world.LoadedPath;
                _world = world;
                AssignWorldEvents();
            }

            _world.ResetPlayer();
            _world.Player.SetStateInstantNoSideFx(prev.Player.State);
            _world.TimeLeft = prev.TimeLeft;

            StartWorld();

            return true;
        }

        private void OnPlayerDeath()
        {
            if (_insideEditor)
            {
                ParentScene?.UnloadSubScene();
                return;
            }

            SessionManager.LoseLife();
            if (SessionManager.Lives <= 0)
            {
                Game.Instance.LoadScene(new GameOverScene());
            }
            else
            {
                Midway? mid = (Midway?)_world.Entities.FirstOrDefault(e => e is Midway);
                if (mid is not null && _world.Player.Center.X > mid.Center.X)
                {
                    SessionManager.ReachMid();
                }
                else
                {
                    SessionManager.ClearMid();
                }

                Reload();
            }
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
                StartWorld();
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
                    OnPlayerDeath();
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

            Text.Draw($"*{SessionManager.Lives.ToString().PadLeft(2, ' ')}",
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
