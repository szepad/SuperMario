namespace Mario2
{
    public enum AxeCutsceneState
    {
        Wait,
        BowserFalls,
        WalkToRight,
    }

    public class AxeCutscene(Player player, Axe axe)
        : Cutscene<AxeCutsceneState>(player)
    {
        private readonly Axe _axe = axe;
        private Bowser? _bowser;

        private const int StopTileCount = 10;
        private const float WalkVel = 69f * Game.TextureScale;

        private int _bridgeStartX;
        private int _bridgeStartY;

        private const float BridgeFallTime = 0.05f;
        private float _bridgeFallTimer = 0.5f;
        
        private const int BridgeTileCount = 13;
        private int _currentBridgeTileIndex = 0;

        private const float BowserFallTime = 1.75f;

        private bool _lavaSplashFxSpawned;
        private float _lavaSplashSpawnTimer;
        private const float LavaSplashSpawnTime = 0.075f;
        private int _lavaSplashCount = 3;

        public override Action GetFunction(AxeCutsceneState state) => state switch
        {
            AxeCutsceneState.WalkToRight => WalkState,
            AxeCutsceneState.BowserFalls => BowserFallState,
            _ => WaitState,
        };

        public override void Start()
        {
            _bowser = (Bowser?)Player.World.Entities.FirstOrDefault(e => e is Bowser);
            if (_bowser is null)
            {
                EnterWalkState();
                return;
            }
         
            Player.World.CameraStyle = CameraStyle.Stopped;
            Player.SetRunAnimFrameTimes(float.PositiveInfinity);
            Player.Gravity = 0f;
            Player.Velocity = Vector2.Zero;

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
                Player.World.SetTile(_bridgeStartX - _currentBridgeTileIndex, _bridgeStartY, SimpleTile.Air);
                Player.World.SpawnEffect(new Vector2(_bridgeStartX - _currentBridgeTileIndex, _bridgeStartY) * Tile.Size,
                    EffectData.BridgeDebris);

                if (_currentBridgeTileIndex == 0)
                {
                    // Chain
                    Player.World.SetTile(_bridgeStartX, _bridgeStartY - 1, SimpleTile.Air);
                }

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
                }
            }

            StateTimer += Time.DeltaTime;
            if (StateTimer >= BowserFallTime)
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
                }
            }
        }
    }
}
