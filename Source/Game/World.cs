using System;
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

        public event Action? OnPlayerDeath;
        private bool _playerDeathInvoked;

        public event Action? OnLevelEnd;
        private bool _levelEndInvoked;

        public event Action<WarpPipe>? OnPipeLoadSublevel;

        public string LoadedPath { get; init; } = string.Empty;
        public int TimeLeft { get; set; } = 400;
        public int Cols { get; private set; } = 100;
        public Vector2 PlayerSpawnPosition { get; set; }
        public WorldBackground Background { get; private set; }
        public ParallaxBackground Parallax { get; private set; }
        public MusicName Music { get; private set; } = MusicName.None;
        public bool IsPipeIntro { get; set; }

        public Player Player { get; private set; }
        public Tile[,] Tiles { get; private set; }
        public List<Entity> Entities { get; } = [];

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
            if (Player is not null && Entities.Contains(Player))
            {
                Entities.Remove(Player);
            }

            Player = new Player(this);
            Entities.Add(Player);
            Player.Activate();
            Player.OnDeath += TryInvokePlayerDeath;
            Player.SetPosition(PlayerSpawnPosition);
        }

        public void Start()
        {
            SoundManager.PlayMusic(Music);
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
            Entities.Add(entity);
            entity.Spawn();

            if (entity is Fireball fireball)
            {
                FireballCount++;
                fireball.OnImpact += _ => FireballCount--;
            }

            return entity;
        }

        public void Freeze(float time)
        {
            _freezeTimer = time;
        }

        public void HandlePipeTransition(WarpPipe pipe)
        {
            if (pipe.Destination == string.Empty || Path.GetFullPath(pipe.Destination) == Path.GetFullPath(LoadedPath))
            {
                WarpExit? exit = (WarpExit?)Entities.FirstOrDefault(
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

                foreach (Entity entity in Entities)
                    if (entity is ScoreText)
                        entity.Update();

                RemoveDestroyedEntities();
            }

            _effectPool.Update();
            UpdateTiles(camera);
        }

        private void UpdateEntities(Camera2D camera)
        {
            HandleEntityCameraLogic(camera);

            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                Entity entity = Entities[i];
                if (entity.IsActive)
                {
                    entity.Update();
                }
            }

            RemoveDestroyedEntities();

            // Handle entity collisions after every entity has moved for the frame
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                Entity entity = Entities[i];
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

            foreach (Entity entity in Entities)
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
                        entity.OnCameraLeaveX();
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
            for (int i = Entities.Count - 1; i >= 0; i--)
            {
                Entity entity = Entities[i];
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

                    Entities.RemoveAt(i);
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
            }
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
        }

        public void UpdateParallax(Camera2D camera)
        {
            Parallax.Update(camera);
        }

        public void DrawBackground()
        {
            Parallax.Draw();
        }

        public void Draw(Camera2D camera, bool editor = false)
        {
            if (DrawStarFlag)
            {
                _starFlag.Draw(StarFlagPosition);
            }

            List<Entity> drawList = [..Entities.OrderBy(x => x.LayerDepth)];

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

            DrawTiles(camera, editor);

            for (int i = lastBg + 1; i < drawList.Count; i++)
            {
                drawList[i].Draw();
            }

            _effectPool.Draw();
        }

        private (int, int) GetTileXBoundsOnCamera(Camera2D camera)
        {
            int minX = (int)((camera.Target.X - camera.Offset.X) / Tile.Size);
            int maxX = (int)((camera.Target.X + camera.Offset.X) / Tile.Size) + 1;

            minX = Helper.Clamp(minX, 0, Cols);
            maxX = Helper.Clamp(maxX, 0, Cols);

            return (minX, maxX);
        }

        private void DrawTiles(Camera2D camera, bool editor)
        {
            (int minX, int maxX) = editor ? (0, Cols) : GetTileXBoundsOnCamera(camera);

            for (int y = 0; y < Rows; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    Tile tile = Tiles[y, x];
                    if (tile == SimpleTile.Air)
                        continue;

                    DrawTile(tile, x, y, editor);
                }
            }
        }

        private void DrawTile(Tile tile, int x, int y, bool editor)
        {
            if (!editor)
            {
                tile.Draw(x, y, (tile is ContainerTile ct && ct.IsHidden) ? 0f : 1f);
                return;
            }

            if (tile is ContainerTile ct2)
            {
                if (ct2.IsHidden)
                {
                    ct2.Draw(x, y, 0.5f);
                }
                else
                {
                    ct2.Draw(x, y, tile.IsCollidable ? 1f : 0.5f);
                }

                if (ct2.ItemInside != ItemType.None)
                {
                    Raylib.DrawTexturePro(Assets.GetTex("items"),
                        new Rectangle(0, ((int)ct2.ItemInside - 1) * 16, 16, 16),
                        new Rectangle(x * Tile.Size, (y - 1) * Tile.Size, new Vector2(Tile.Size)),
                        Vector2.Zero, 0f, Color.White.Alpha(0.5f));
                }
            }
            else
            {
                tile.Draw(x, y, tile.IsCollidable ? 1f : 0.5f);
            }
        }
    }
}
