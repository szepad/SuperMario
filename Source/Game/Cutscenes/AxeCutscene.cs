namespace Mario2
{
    public enum AxeCutsceneState
    {
        Wait,
        BowserFalls,
        WalkToRight,
        Message,
        TimerCountdown,
        EndWait,
    }

    public class AxeCutscene : Cutscene<AxeCutsceneState>
    {
        private readonly Axe _axe;
        private Bowser? _bowser;

        private const float StopTileCount = 10.75f;
        private const float WalkVel = 69f * Game.TextureScale;

        private int _bridgeStartX;
        private int _bridgeStartY;

        private const float BridgeFallTime = 0.0625f;
        private float _bridgeFallTimer = 0.5f;
        
        private const int BridgeTileCount = 13;
        private int _currentBridgeTileIndex = 0;

        private bool _lavaSplashFxSpawned;
        private float _lavaSplashSpawnTimer;
        private const float LavaSplashSpawnTime = 0.075f;
        private int _lavaSplashCount = 3;

        private bool _splashSoundPlayed;

        private const float TimeAfterMessage = 6f;
        private const float TimeAfterEnd = 3f;

        private TimerCountdownObject _timerCountdown;

        public AxeCutscene(Player player, Axe axe) : base(player)
        {
            _axe = axe;
            _timerCountdown = new(this, () =>
            {
                State++;
                StateTimer = 0f;
            });
        }

        public override Action GetFunction(AxeCutsceneState state) => state switch
        {
            AxeCutsceneState.BowserFalls => BowserFallState,
            AxeCutsceneState.WalkToRight => WalkState,
            AxeCutsceneState.Message => MessageState,
            AxeCutsceneState.TimerCountdown => _timerCountdown.Update,
            AxeCutsceneState.EndWait => EndWaitState,
            _ => WaitState,
        };

        public override void Start()
        {
            Player.World.RequestMusic(MusicName.None);
            Player.World.CameraStyle = CameraStyle.Stopped;
            Player.Gravity = 0f;
            Player.Velocity = Vector2.Zero;

            _bowser = (Bowser?)Player.World.Entities.FirstOrDefault(e => e is Bowser);
            if (_bowser is null)
            {
                EnterWalkState();
                return;
            }

            Player.SetRunAnimFrameTimes(float.PositiveInfinity);

            _bowser.SetAi(new DummyAi(_bowser));
            _bowser.Gravity = 0f;
            _bowser.Velocity = Vector2.Zero;
            _bowser.Sprite.Play(Bowser.DieAnim);

            _bridgeStartX = (int)(_axe.Center.X / Tile.Size) - 1;
            _bridgeStartY = (int)(_axe.Center.Y / Tile.Size) + 2;
        }

        private void WaitState()
        {
            _bridgeFallTimer -= Time.DeltaTime;
            if (_bridgeFallTimer <= 0f && _currentBridgeTileIndex < BridgeTileCount)
            {
                // Chain
                if (_currentBridgeTileIndex == 0)
                {
                    Player.World.SetTile(_bridgeStartX, _bridgeStartY - 1, SimpleTile.Air);
                    SoundManager.PlaySound(Sounds.Bridge);
                }

                Player.World.SetTile(_bridgeStartX - _currentBridgeTileIndex, _bridgeStartY, SimpleTile.Air);
                Player.World.SpawnEffect(new Vector2(_bridgeStartX - _currentBridgeTileIndex, _bridgeStartY) * Tile.Size,
                    EffectData.BridgeDebris);

                _bridgeFallTimer = BridgeFallTime;
                _currentBridgeTileIndex++;
            }

            if (_currentBridgeTileIndex >= BridgeTileCount)
            {
                State++;
                if (_bowser is not null) 
                {
                    _bowser.Gravity = Entity.DefaultGravity * 0.35f;
                    _bowser.LayerDepth = LayerDepth.Background;
                }
            }
        }

        private void BowserFallState()
        {
            if (_bowser is null)
            {
                EnterWalkState();
                return;
            }

            int tx = (int)(_bowser.Feet.X / Tile.Size);
            int ty = (int)(_bowser.Feet.Y / Tile.Size + 0.45f);
            if (!_lavaSplashFxSpawned && _bowser.World.GetTile(tx, ty, out Tile tile)
                && tile is LavaTile)
            {
                _lavaSplashFxSpawned = true;
            }

            if (_lavaSplashFxSpawned && _lavaSplashCount > 0)
            {
                _lavaSplashSpawnTimer += Time.DeltaTime;
                if (_lavaSplashSpawnTimer >= LavaSplashSpawnTime)
                {
                    _lavaSplashSpawnTimer = 0f;
                    _lavaSplashCount--;
                    _bowser.World.SpawnEffect(new Vector2(_bowser.Feet.X, World.Rows * Tile.Size - Tile.Size * 1.5f),
                        EffectData.BowserLavaSplash);

                    if (!_splashSoundPlayed)
                    {
                        _splashSoundPlayed = true;
                        SoundManager.PlaySound(Sounds.BowserLava);
                    }
                }
            }

            if (_bowser.Feet.Y >= World.Rows * Tile.Size)
            {
                EnterWalkState();
            }
        }

        private void EnterWalkState()
        {
            State = AxeCutsceneState.WalkToRight;
            StateTimer = 0f;
            Player.Sprite.FlipX = false;
            Player.Gravity = Entity.DefaultGravity;
            Player.Velocity.X = WalkVel;
            Player.World.CameraAutoScrollSpeed = Player.Velocity.X * 1.5f;
            Player.World.CameraStyle = CameraStyle.AutoScroll;
            Player.PlayAnim(PlayerAnim.Run);
            Player.SetRunAnimFrameTimes(0.15f);
            Player.World.RequestMusic(MusicName.WorldClear, false);
        }

        private void WalkState()
        {
            if (Player.Center.X > Player.World.Cols * Tile.Size - Tile.Size * StopTileCount)
            {
                Player.Velocity.X -= 69f.Scale() * Time.DeltaTime;

                float t = Helper.Clamp01(Player.Velocity.X / WalkVel);
                float frameTime = Raymath.Lerp(0.15f, 0.25f, 1f - t);
                Player.SetRunAnimFrameTimes(frameTime);

                if (Player.Velocity.X <= 0f)
                {
                    Player.Velocity.X = 0f;
                    Player.PlayAnim(PlayerAnim.Idle);
                    State++;
                    Player.World.DisplayCastleMessage();
                    StateTimer = 0f;
                }
            }
        }

        private void MessageState()
        {
            StateTimer += Time.DeltaTime;
            if (StateTimer >= TimeAfterMessage)
            {
                State++;
            }
        }

        private void EndWaitState()
        {
            StateTimer += Time.DeltaTime;
            if (StateTimer >= TimeAfterEnd)
            {
                Player.World.ReachLevelEnd();
            }
        }
    }
}
