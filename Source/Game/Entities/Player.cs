namespace Mario2
{
    public class Player : Entity
    {
        private const float MaxWalkSpeed = 93.6f * Game.TextureScale;
        private const float MaxRunSpeed = 153.6f * Game.TextureScale;
        private const float WalkAccel = 160f * Game.TextureScale;
        private const float RunAccel = 170f * Game.TextureScale;
        private const float StopFriction = 300f * Game.TextureScale;
        private const float SkidFriction = 100f * Game.TextureScale;

        private float _inputDir;

        private const float JumpForce = 180f * Game.TextureScale;
        private const float MaxJumpTime = 0.3f;
        private float _jumpTimer;
        private bool _isJumping;

        private const float JumpBufferTime = 0.06f;
        private const float CoyoteTime = 0.05f;

        private float _jumpBuffer;
        private float _coyoteTimer;

        public Player(World world) : base(world)
        {
            Size = new Vector2(8f, 14f).Scale();
            SpriteOffset = new Vector2(4f, 2f).Scale();
            LayerDepth = LayerDepth.Player;

            Sprite.SetSheet("mario");
            Sprite.Add("idle", new Animation(1, 16, 16, 0, 1f));
            Sprite.Add("run", new Animation(3, 16, 16, 16, 0.1f));
            Sprite.Add("skid", new Animation(1, 16, 16, 32, 1f));
            Sprite.Add("jump", new Animation(1, 16, 16, 48, 1f));
        }

        protected override void OnSpawn()
        {
            
        }

        protected override void OnDestroy()
        {
            
        }

        public override void Update()
        {
            HandleMovement();
            HandleJumping();
            HandleAnimations();

            base.Update();
        }

        private void HandleMovement()
        {
            bool running = Input.IsKeyDown(KeyboardKey.D);
            float accel = running ? RunAccel : WalkAccel;

            _inputDir = 0f;
            if (Input.IsKeyDown(KeyboardKey.Left)) _inputDir--;
            if (Input.IsKeyDown(KeyboardKey.Right)) _inputDir++;

            float stopFriction = IsGrounded ? StopFriction : StopFriction * 0.5f;

            if (Velocity.X >= 0f && _inputDir > 0f)
            {
                Velocity.X += accel * Time.DeltaTime;
            }
            else if (Velocity.X <= 0f && _inputDir < 0f)
            {
                Velocity.X -= accel * Time.DeltaTime;
            }
            else if (Velocity.X > 0f && _inputDir < 0f)
            {
                Velocity.X -= (stopFriction + SkidFriction) * Time.DeltaTime;
            }
            else if (Velocity.X < 0f && _inputDir > 0f)
            {
                Velocity.X += (stopFriction + SkidFriction) * Time.DeltaTime;
            }
            else if (Velocity.X != 0f && _inputDir == 0f)
            {
                if (IsGrounded)
                {
                    Velocity.X -= MathF.Sign(Velocity.X) * stopFriction * Time.DeltaTime;
                    if (Velocity.X.CloseToZero(threshold: stopFriction * Time.DeltaTime + 0.01f))
                    {
                        Velocity.X = 0f;
                    }
                }
            }

            Velocity.X = Helper.Clamp(Velocity.X, -MaxRunSpeed, MaxRunSpeed);
            if (!running && MathF.Abs(Velocity.X) > MaxWalkSpeed)
            {
                Velocity.X -= MathF.Sign(Velocity.X) * StopFriction * Time.DeltaTime;
            }
        }

        private void HandleJumping()
        {
            if (Input.IsKeyPressed(KeyboardKey.A))
            {
                _jumpBuffer = JumpBufferTime;
            }


            if (IsGrounded)
            {
                _coyoteTimer = CoyoteTime;
            }

            _jumpBuffer -= Time.DeltaTime;
            _coyoteTimer -= Time.DeltaTime;

            if (_coyoteTimer > 0f && _jumpBuffer > 0f)
            {
                Velocity.Y = -JumpForce;
                _isJumping = true;
                _jumpTimer = 0f;
            }

            _jumpTimer += Time.DeltaTime;
            if (_jumpTimer >= MaxJumpTime || !Input.IsKeyDown(KeyboardKey.A))
            {
                StopJump();
            }

            Gravity = _isJumping ? 0f : DefaultGravity;
            if (_jumpTimer >= MaxJumpTime && _jumpTimer < MaxJumpTime + 0.02f)
            {
                Gravity *= 4f;
            }
        }

        public void StopJump()
        {
            if (!_isJumping)
                return;

            _jumpTimer = MaxJumpTime;
            _isJumping = false;
        }

        private void HandleAnimations()
        {
            if (!IsGrounded)
            {
                Sprite.Play("jump");
                return;
            }

            if (_inputDir < 0f) Sprite.FlipX = true;
            else if (_inputDir > 0f) Sprite.FlipX = false;

            if (Velocity.X == 0f)
            {
                Sprite.Play("idle");
                return;
            }
            
            if (MathF.Sign(Velocity.X) != MathF.Sign(_inputDir) && _inputDir != 0f)
            {
                Sprite.Play("skid");
                return;
            }

            Sprite.Play("run");
            if (Sprite.CurrentAnimationName == "run")
            {
                for (int i = 0; i < Sprite.CurrentAnimation?.FrameTimes.Length; i++)
                {
                    float t = Helper.Clamp01(MathF.Abs(Velocity.X) / MaxRunSpeed);
                    Sprite.CurrentAnimation.FrameTimes[i] = Raymath.Lerp(0.2f, 0.067f, t);
                }
            }
        }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
        {
            if (side == CollisionSide.Top)
            {
                StopJump();

                if (tile is ContainerTile ct)
                {
                    ct.OnHit(this);
                }
            }
        }

        protected override void OnTileOverlap(Tile tile)
        {
            if (tile is CoinTile)
            {
                tile.Destroy();
            }
        }
    }
}
