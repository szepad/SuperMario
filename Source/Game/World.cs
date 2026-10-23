using System.Diagnostics.CodeAnalysis;

namespace Mario2
{
    public enum CameraStyle
    {
        Follow,
        AutoScroll,
        Stopped
    }

    public class World
    {
        public const int Rows = 14;
        public const float FallOffHeight = (Rows + 2) * Tile.Size;

        public CameraStyle CameraStyle { get; set; } = CameraStyle.Follow;
        public float CameraAutoScrollSpeed { get; set; } = Tile.Size * 4f;

        public event Action<MusicName, bool>? RequestMusicEvent;

        public event Action? OnPlayerDeath;
        private bool _playerDeathInvoked;

        public event Action? OnLevelEnd;
        private bool _levelEndInvoked;

        public event Action? OnDisplayCastleMessage;

        public event Action<WarpPipe>? OnPipeLoadSublevel;

        public string LoadedPath { get; init; } = string.Empty;
        public string ParentWorldPath { get; set; } = string.Empty;

        /// <summary>Set on the editor's preview world, which is drawn but never updated.</summary>
        public bool IsEditorPreview { get; init; }

        public int TimeLeft { get; set; } = 400;
        public int Cols { get; private set; } = 100;
        public Vector2 PlayerSpawnPosition { get; set; }
        public WorldBackground Background { get; private set; }
        public ParallaxBackground Parallax { get; private set; }
        public MusicName Music { get; private set; } = MusicName.None;
        public bool IsPipeIntro { get; set; }

        public Player Player { get; private set; }
        public Tile[,] Tiles { get; private set; }

        public IReadOnlyList<Entity> Entities => _entities;
        private readonly List<Entity> _entities = [];

        public bool DrawStarFlag { get; set; }
        public Vector2 StarFlagPosition;
        private readonly Sprite _starFlag = new("star_flag");

        public int FireballCount { get; private set; }

        public bool IsFrozen => _freezeTimer > 0f;
        private float _freezeTimer;

        private readonly Sprite _globalQuestionTileAnim = new("question");

        private readonly EffectPool _effectPool = new();

        private const float TimerTickRate = 0.4f;
        private float _timerTickTimer = TimerTickRate;

        private bool _hurryStarted;
        private const int HurryTime = 100;

        private const float WarningSoundTime = 2f;
        private float _warningSoundTimer;

        private bool _spawnedPiranhaPlants;

        private MusicName _musicForHurry = MusicName.None;

        private Axe? _axe = null;

        public World()
        {
            ResetPlayer();

            Tiles = new Tile[Rows, Cols];
            Clear(Tiles);

            SetBackground(WorldBackground.Sky);

            _globalQuestionTileAnim.Add("?", new Animation(3, 16, 16, 0, 0.15f));

            _starFlag.Add("flag", new Animation(4, 16, 16, 0, 0.15f));
            _starFlag.Origin = new Vector2(Tile.Size / 2f, 0f);
        }

        [MemberNotNull(nameof(Player))]
        public void ResetPlayer()
        {
            if (Player is not null && _entities.Contains(Player))
            {
                _entities.Remove(Player);
            }

            Player = new Player(this);
            _entities.Add(Player);
            Player.Activate();
            Player.OnDeath += TryInvokePlayerDeath;
            Player.SetPosition(PlayerSpawnPosition);
        }

        public void Start()
        {
            PlayerLevelMusic();
            TryTeleportToMidway();
            TrySpawnPiranhaPlants();
        }

        public void PlayerLevelMusic()
        {
            if (TimeLeft < HurryTime)
            {
                _hurryStarted = true;
                PlayHurryMusic();
            }
            else
            {
                RequestMusic(Music);
            }
        }

        public void RequestMusic(MusicName music, bool isLooped = true)
        {
            RequestMusicEvent?.Invoke(music, isLooped);
        }

        private void TrySpawnPiranhaPlants()
        {
            if (_spawnedPiranhaPlants || SessionManager.CurrentLevel == 1) return;
            _spawnedPiranhaPlants = true;

            const int PipeId = 24;
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    if (GetTile(col, row, out Tile tile) 
                        && tile is SimpleTile st
                        && st.Id == PipeId)
                    {
                        PiranhaPlant plant = new(this);
                        plant.SetPosition(new Vector2(col, row) * Tile.Size + new Vector2(8f, 2f).Scale());

                        if (!_entities.Where(e => e is NoPlant)
                            .Any(e => Vector2.Distance(e.Feet, plant.Head) <= Tile.Size))
                        {
                            SpawnEntity(plant);
                        }
                    }
                }
            }    
        }

        private void TryTeleportToMidway()
        {
            if (!SessionManager.ReachedMidway)
                return;

            Midway? mid = (Midway?)_entities.FirstOrDefault(e => e is Midway);
            if (mid is not null)
            {
                Player.SetFeetPos(mid.Feet);

                // Enemies near shouldn't spawn
                for (int i = _entities.Count - 1; i >= 0; i--)
                {
                    if (_entities[i] is not Enemy enemy)
                        continue;

                    const float SpawnThreshold = Tile.Size * 6f;
                    if (MathF.Abs(mid.Feet.X - enemy.Feet.X) < SpawnThreshold)
                    {
                        RemoveEntity(enemy);
                    }
                }
            }
        }

        private void PlayHurryMusic()
        {
            MusicName baseMusic = _musicForHurry != MusicName.None
                ? _musicForHurry : Music;

            if (Enum.TryParse($"{baseMusic}Hurry", 
                out MusicName hurry))
            {
                RequestMusic(hurry);
            }
            else
            {
                RequestMusic(baseMusic);
            }
        }

        [MemberNotNull(nameof(Parallax))]
        public void SetBackground(WorldBackground background)
        {
            Background = background;
            Parallax = WorldBackgroundExtension.GetParallax(background);
        }

        public void SetMusic(MusicName music)
        {
            Music = music;
        }

        public void DisplayCastleMessage() => OnDisplayCastleMessage?.Invoke();

        public void Clear() => Clear(Tiles);
        public static void Clear(Tile[,] tiles)
        {
            for (int row = 0; row < tiles.GetLength(0); row++)
            {
                for (int col = 0; col < tiles.GetLength(1); col++)
                {
                    tiles[row, col] = SimpleTile.Air;
                }
            }
        }

        public bool GetTile(int col, int row, out Tile tile)
        {
            tile = SimpleTile.Air;
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return false;

            tile = Tiles[row, col];
            return true;
        }

        public Tile? SetTile(int col, int row, Tile tile)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return null;

            tile.World = this;
            tile.Col = col;
            tile.Row = row;
            Tiles[row, col] = tile;
            return tile;
        }

        public void SetCols(int newCols)
        {
            if (newCols < 0)
                return;

            Tile[,] newMtx = new Tile[Rows, newCols];
            Clear(newMtx);

            for (int col = 0; col < newCols; col++)
            {
                if (col >= Cols)
                    break;

                for (int row = 0; row < Rows; row++)
                {
                    newMtx[row, col] = Tiles[row, col];
                }
            }

            Cols = newCols;
            Tiles = newMtx;
        }

        public void ReachLevelEnd()
        {
            if (_levelEndInvoked) return;
            _levelEndInvoked = true;

            OnLevelEnd?.Invoke();
        }

        public T SpawnEntity<T>(T entity, Vector2 feetPos) where T : Entity
        {
            entity.SetFeetPos(feetPos);
            T spawned = SpawnEntity(entity);
            return spawned;
        }

        public T SpawnEntity<T>(T entity) where T : Entity
        {
            _entities.Add(entity);
            entity.Spawn();

            if (entity is Fireball fireball)
            {
                FireballCount++;
                fireball.OnImpact += _ => FireballCount--;
            }
            else if (entity is Axe axe)
            {
                _axe = axe;
            }

            return entity;
        }

        public void RemoveEntity(Entity entity)
        {
            _entities.Remove(entity);

            if (entity is Axe)
                _axe = null;
        }

        public void Freeze(float time)
        {
            _freezeTimer = time;
        }

        public void HandlePipeTransition(WarpPipe pipe)
        {
            if (pipe.ReturnToSubworld)
                pipe.Destination = ParentWorldPath;

            if (pipe.Destination == string.Empty || LevelPaths.Same(pipe.Destination, LoadedPath))
            {
                WarpExit? exit = (WarpExit?)_entities.FirstOrDefault(
                ent => ent is WarpExit exit && ent != pipe
                && exit.Tag == pipe.Tag);

                if (exit is null)
                {
                    //exit = new WarpExit(this) { Tag = pipe.Tag };
                    //exit.SetFeetPos(pipe.Feet);
                    PlayerState state = Player.State;
                    ResetPlayer();
                    Player.SetStateInstantNoSideFx(state);
                    Player.SetPosition(PlayerSpawnPosition);
                    
                    // Snap player onto the tile if he's standing inside it
                    Player.OldPosition.Y--;
                    Player.HandleCollisions(false);
                }
                else
                {
                    Player.StartCutscene(new PipeExitCutscene(Player, exit));
                }
            }
            else
            {
                OnPipeLoadSublevel?.Invoke(pipe);
            }
        }

        public void SpawnEffect(Vector2 position, EffectData data) => SpawnEffect(position, Vector2.Zero, data);
        public void SpawnEffect(Vector2 position, Vector2 velocity, EffectData data)
        {
            _effectPool.Acquire().Init(position, velocity, data);
        }

        public void SyncQuestionTileAnim(Sprite sprite)
        {
            sprite.SetFrame(_globalQuestionTileAnim.CurrentFrame);
            sprite.SetFrameTimer(_globalQuestionTileAnim.CurrentFrameTimer);
        }

        public void Update(Camera2D camera)
        {
            _globalQuestionTileAnim.Update();
            _starFlag.Update();

            if (!IsFrozen)
            {
                UpdateEntities(camera);
                UpdateTime();
            }
            else
            {
                _freezeTimer -= Time.DeltaTime;

                Player.UpdateVisualLogic();
                Player.Sprite.Update();

                foreach (Entity entity in _entities)
                    if (entity is ScoreText)
                        entity.Update();

                RemoveDestroyedEntities();
            }

            _effectPool.Update();
            UpdateTiles(camera);

            if (_hurryStarted && _warningSoundTimer > 0f)
            {
                _warningSoundTimer -= Time.DeltaTime;
                if (_warningSoundTimer <= 0f && SoundManager.CurrentMusic != MusicName.Invincible)
                {
                    PlayHurryMusic();
                }
            }
        }

        private void UpdateEntities(Camera2D camera)
        {
            HandleEntityCameraLogic(camera);

            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                Entity entity = _entities[i];
                if (entity.IsActive)
                {
                    entity.Update();
                }
            }

            RemoveDestroyedEntities();

            // Handle entity collisions after every entity has moved for the frame
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                Entity entity = _entities[i];
                if (entity.IsActive)
                {
                    entity.HandleEntityCollisions();
                }
            }
        }

        private void HandleEntityCameraLogic(Camera2D camera)
        {
            float cameraLeft = camera.Target.X - camera.Offset.X;
            float cameraRight = camera.Target.X + camera.Offset.X;

            foreach (Entity entity in _entities)
            {
                if (entity.IsActive)
                {
                    // Deactivate if left camera x2
                    if (entity.Rect.X + entity.Rect.Width < camera.Target.X - camera.Offset.X * 2.5f
                        || entity.Rect.X > camera.Target.X + camera.Offset.X * 2.5f)
                    {
                        entity.Destroy();
                    }
                    else if (entity.Rect.X + entity.Rect.Width < cameraLeft
                        || entity.Rect.X > cameraRight)
                    {
                        entity.CameraLeaveX();
                    }
                }
                else if (entity.Rect.X <= cameraRight)
                {
                    entity.Activate();
                    entity.ActivateSurrounding();
                }
            }

            // Keep player on screen
            if (Player.Position.X < cameraLeft)
            {
                Player.SetPosition(cameraLeft, Player.Position.Y);
                Player.Velocity.X = 0f;
            }
            else if (Player.Position.X > cameraRight - Player.Size.X)
            {
                Player.SetPosition(cameraRight - Player.Size.X, Player.Position.Y);
                Player.Velocity.X = 0f;
            }
        }

        private void RemoveDestroyedEntities()
        {
            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                Entity entity = _entities[i];
                if (entity.IsDestroyed)
                {
                    if (entity is Fireball fireball && !fireball.HasImpacted)
                    {
                        FireballCount--;
                    }
                    else if (entity is Player)
                    {
                        TryInvokePlayerDeath();
                    }

                    _entities.RemoveAt(i);
                }
            }
        }

        private void UpdateTime()
        {
            if (Player.IsCutscenePlaying || Player.IsDead || Player.IsDestroyed)
                return;

            _timerTickTimer -= Time.DeltaTime;
            if (_timerTickTimer <= 0f)
            {
                _timerTickTimer = TimerTickRate;
                TimeLeft--;

                if (TimeLeft <= 0)
                {
                    TimeLeft = 0;
                    Player.Die();
                }
                else if (TimeLeft < HurryTime)
                {
                    StartHurry();
                }
            }
        }

        private void StartHurry()
        {
            if (_hurryStarted) return;
            _hurryStarted = true;

            if (SoundManager.CurrentMusic != MusicName.Invincible)
            {
                _musicForHurry = SoundManager.CurrentMusic;
                SoundManager.StopMusic();
            }

            SoundManager.PlaySound(Sounds.Warning);
            _warningSoundTimer = WarningSoundTime;
        }

        private void TryInvokePlayerDeath()
        {
            if (_playerDeathInvoked) return;
            _playerDeathInvoked = true;

            SoundManager.StopMusic();
            SoundManager.PlaySound(Sounds.PlayerDie);

            OnPlayerDeath?.Invoke();
        }

        private void UpdateTiles(Camera2D camera)
        {
            (int minX, int maxX) = GetTileXBoundsOnCamera(camera);
            for (int y = 0; y < Rows; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    Tile tile = Tiles[y, x];
                    if (tile == SimpleTile.Air)
                        continue;

                    tile.Update();
                    
                    if (tile.IsDestroyed)
                    {
                        Tiles[y, x] = SimpleTile.Air;
                    }
                }
            }
        }

        public void SetCameraPosition(ref Camera2D camera, float x)
        {
            camera.Target.X = x;
            ClampCameraInsideLevelBounds(ref camera);
        }

        public void HandleCameraFollow(ref Camera2D camera)
        {
            if (CameraStyle == CameraStyle.Follow)
            {
                const float LookAhead = Tile.Size;
                float targetX = Player.Position.X + LookAhead;
                if (targetX > camera.Target.X)
                {
                    camera.Target.X = targetX;
                }
            }
            else if (CameraStyle == CameraStyle.AutoScroll)
            {
                camera.Target.X += CameraAutoScrollSpeed * Time.DeltaTime;
            }

            ClampCameraInsideLevelBounds(ref camera);
        }

        private void ClampCameraInsideLevelBounds(ref Camera2D camera)
        {
            const float HalfX = Game.Width / 2f;
            camera.Target.X = Helper.Clamp(camera.Target.X, HalfX, Cols * Tile.Size - HalfX);

            if (_axe is not null)
            {
                float targetX = _axe.Rect.X + _axe.Size.X + Tile.Size;
                if (camera.Target.X + HalfX > targetX)
                    camera.Target.X = targetX - HalfX;
            }
        }

        public void UpdateParallax(Camera2D camera)
        {
            Parallax.Update(camera);
        }

        public void DrawBackground()
        {
            Parallax.Draw();
        }

        public void Draw(Camera2D camera, bool tileCulling = true)
        {
            camera.Target.X = (int)camera.Target.X;
            camera.Target.Y = (int)camera.Target.Y;
            
            _effectPool.Draw(isBackground: true);

            if (DrawStarFlag)
            {
                _starFlag.Draw(StarFlagPosition);
            }

            List<Entity> drawList = [.. _entities.OrderBy(x => x.LayerDepth)];

            int lastBg = -1;
            for (int i = 0; i < drawList.Count; i++)
            {
                Entity entity = drawList[i];
                if (entity.LayerDepth == LayerDepth.Background)
                {
                    drawList[i].Draw();
                    lastBg = i;
                }
                else
                {
                    break;
                }
            }

            DrawTiles(camera, tileCulling);

            for (int i = lastBg + 1; i < drawList.Count; i++)
            {
                drawList[i].Draw();
            }

            _effectPool.Draw(isBackground: false);
        }

        private (int, int) GetTileXBoundsOnCamera(Camera2D camera)
        {
            int minX = (int)((camera.Target.X - camera.Offset.X) / Tile.Size);
            int maxX = (int)((camera.Target.X + camera.Offset.X) / Tile.Size) + 1;

            minX = Helper.Clamp(minX, 0, Cols);
            maxX = Helper.Clamp(maxX, 0, Cols);

            return (minX, maxX);
        }

        private void DrawTiles(Camera2D camera, bool tileCulling)
        {
            (int minX, int maxX) = tileCulling ? GetTileXBoundsOnCamera(camera) : (0, Cols);

            for (int y = 0; y < Rows; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    Tile tile = Tiles[y, x];
                    if (tile == SimpleTile.Air)
                        continue;

                    tile.Draw(x, y, (tile is ContainerTile ct && ct.IsHidden) ? 0f : 1f);
                }
            }
        }
    }
}
