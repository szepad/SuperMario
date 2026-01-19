namespace Mario2
{
    public enum PlayerState
    {
        Small,
        Big,
        Fire
    }

    public enum PlayerPalette
    {
        Normal,
        Fire,
        Star1,
        Star2,
        Star3
    }

    public class Player : Entity
    {
        public event Action? OnDeath;

        public bool IsDead { get; private set; }
        public PlayerState State { get; private set; } = PlayerState.Small;

        public bool IsInvincible => _iFrameTimer > 0f;
        public bool IsStarman => _starmanTimer > 0f;

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

        private const float StompBounce = 160f * Game.TextureScale;

        private const float IFrames = 3.5f;
        private float _iFrameTimer;

        private bool _isCrouching;

        private bool _shrinkOnNextUpdate;

        private bool _isSkidding;
        private float _skidEffectTimer;
        private const float SkidEffectTime = 0.075f;

        private readonly Shader _paletteShader;
        private readonly int _paletteSamplerLoc;
        private Texture2D _palette;

        private bool _useStarVisuals;
        private int _starPaletteIndex = 0;
        private float _starPaletteSwapTimer;
        private const float StarPaletteSwapTime = 0.05f;
        private const float StarPaletteSwapTimeNearEnd = 0.2f;

        private bool _endFireAcquireOnNextUpdate;

        private float _starmanTimer;
        private const float StarmanTime = 11.5f;
        private const float StarmanNearEndTime = 2f;

        private const float FireballCooldown = 0.1f;
        private float _throwFireballCooldown = 0f;

        private bool _isGapDashing;

        public Player(World world) : base(world)
        {
            SetSmallStateVars();
            LayerDepth = LayerDepth.Player;

            Sprite.SetSheet("mario");
            Sprite.Add("idle_small", new Animation(1, 16, 16, 0, 1f));
            Sprite.Add("run_small", new Animation(3, 16, 16, 16, 0.1f));
            Sprite.Add("skid_small", new Animation(1, 16, 16, 32, 1f));
            Sprite.Add("jump_small", new Animation(1, 16, 16, 48, 1f));
            Sprite.Add("death_small", new Animation(1, 16, 16, 80, 1f));

            // Big
            Sprite.Add("grow", new Animation(2, 24, 32, 128, 0.1f));
            Sprite.Add("idle_big", new Animation(1, 24, 32, 128 + 32, 1f));
            Sprite.Add("run_big", new Animation(3, 24, 32, 128 + 32 * 2, 0.1f));
            Sprite.Add("skid_big", new Animation(1, 24, 32, 128 + 32 * 3, 1f));
            Sprite.Add("jump_big", new Animation(1, 24, 32, 128 + 32 * 4, 1f));
            Sprite.Add("crouch_big", new Animation(1, 24, 32, 128 + 32 * 5, 1f));
            Sprite.Add("fireball_big", new Animation(1, 24, 32, 128 + 32 * 6, 1f));

            Assets.TryGetShader("palette", out _paletteShader);
            _paletteSamplerLoc = Raylib.GetShaderLocation(_paletteShader, "palette");
            _palette = Assets.GetTex("palette_mario");
        }

        protected override void OnSpawn()
        {
            
        }

        protected override void OnDestroy()
        {
            
        }

        public void TakeDamage()
        {
            if (IsInvincible)
                return;

            if (State == PlayerState.Small)
            {
                Die();
            }
            else
            {
                Shrink();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;

            IsHittable = false;
            IsCollidable = false;
            Velocity = Vector2.Zero;
            Gravity = 0f;
            SetSmallStateVars();
            State = PlayerState.Small;
            Sprite.Play("death_small");

            OnDeath?.Invoke();
        }

        private void SetSmallStateVars()
        {
            Vector2 prevSize = Size;
            Size = HitboxSize = new Vector2(8f, 14f).Scale();
            SpriteOffset = new Vector2(4f, 2f).Scale();
            SetPosition(Position.X + (prevSize.X - Size.X) / 2f, Position.Y + prevSize.Y - Size.Y);
        }

        private void SetBigStateVarsX()
        {
            float prevSize = Size.X;
            Size.X = HitboxSize.X = 12f * Game.TextureScale;
            SpriteOffset.X = 6f * Game.TextureScale;
            OldPosition.X = Position.X = Position.X - (Size.X - prevSize) / 2f;
        }

        private void SetBigStateVarsY()
        {
            float prevSize = Size.Y;
            Size.Y = HitboxSize.Y = 28f * Game.TextureScale;
            SpriteOffset.Y = 3f * Game.TextureScale;
            OldPosition.Y = Position.Y = Position.Y - Size.Y + prevSize;
        }

        private void Grow()
        {
            if (State != PlayerState.Small) return;
            State = PlayerState.Big;

            World.Freeze(1f);
            Sprite.Play("grow");

            SetBigStateVarsX();
            EnumerateCollisionTiles(GetTilesForCollisionCheck(), (tile, tileRect) =>
            {
                if (tile.IsCollidable)
                {
                    if (Position.X + Size.X > tileRect.X + tileRect.Width) // Inside to the left
                    {
                        OldPosition.X = Position.X = tileRect.X + tileRect.Width;
                    }
                    else if (Position.X < tileRect.X) // Inside to the right
                    {
                        OldPosition.X = Position.X = tileRect.X - Size.X;
                    }
                }
            });

            SetBigStateVarsY();
            EnumerateCollisionTiles(GetTilesForCollisionCheck(), (tile, tileRect) =>
            {
                if (tile.IsCollidable)
                {
                    Crouch(force: true);
                }
            });

            // Grow is usually called after update, but before draw, so call animation
            // management again to prevent a bugged animation showing for 1 frame
            HandleAnimations();
        }

        private void AcquireFire()
        {
            if (State == PlayerState.Fire)
                return;

            if (State == PlayerState.Small)
            {
                Grow();
                return;
            }

            State = PlayerState.Fire;
            World.Freeze(1f);
            _useStarVisuals = true;
            _endFireAcquireOnNextUpdate = true;
        }

        private void EndFireAcquire()
        {
            if (!IsStarman)
            {
                _useStarVisuals = false;
                ChangePalette(PlayerPalette.Fire);
            }
        }

        private void AcquireStar()
        {
            _useStarVisuals = true;
            _starmanTimer = StarmanTime;
        }

        private void ChangePalette(PlayerPalette palette)
        {
            _palette = palette switch
            {
                PlayerPalette.Fire => Assets.GetTex("palette_mario_fire"),
                PlayerPalette.Star1 => Assets.GetTex("palette_star_1"),
                PlayerPalette.Star2 => Assets.GetTex("palette_star_2"),
                PlayerPalette.Star3 => Assets.GetTex("palette_star_3"),
                _ => Assets.GetTex("palette_mario")
            };
        }

        private void Shrink()
        {
            if (_shrinkOnNextUpdate) return;
            _shrinkOnNextUpdate = true;

            _iFrameTimer = IFrames;
            World.Freeze(1f);
            Sprite.Play("grow");
            ChangePalette(PlayerPalette.Normal);
        }

        private void DoShrinkLogic()
        {
            if (State == PlayerState.Small) return;
            State = PlayerState.Small;
            ForceStandUp();
            SetSmallStateVars();
        }

        private void Crouch(bool force = false)
        {
            if ((_isCrouching || !IsGrounded) && !force) return;
            _isCrouching = true;

            Size.Y /= 2f;
            HitboxSize.Y /= 2f;
            SpriteOffset.Y += Size.Y;
            Position.Y += Size.Y;
        }

        private void TryStandUp()
        {
            if (!_isCrouching)
                return;

            ForceStandUp();

            bool insideTile = false;
            EnumerateCollisionTiles(GetTilesForCollisionCheck(), (tile, tileRect) =>
            {
                if (tile.IsCollidable)
                {
                    insideTile = true;
                }
            });

            if (insideTile)
            {
                Crouch(force: true);
            }
        }

        private void ForceStandUp()
        {
            if (!_isCrouching) return;
            _isCrouching = false;

            SpriteOffset.Y -= Size.Y;
            Position.Y -= Size.Y;
            Size.Y *= 2f;
            HitboxSize.Y *= 2f;
        }

        public override void Update()
        {
            _isSkidding = false;

            HandleStateChangeCallbacks();

            if (!IsDead)
            {
                HandleStarman();
                HandleMovement();
                HandleJumping();
                HandleFireballThrowing();
                UpdateVisualLogic();
                HandleAnimations();
            }

            // Apply gravity this way so "Gap-dash" is possible
            //                               |
            //                               V
            //              (Run over 1-tile gaps without jumping)

            if (_coyoteTimer <= 0f || MathF.Abs(Velocity.X) < MaxRunSpeed * 0.9f)
            {
                ApplyGravity();
                _isGapDashing = false;
            }
            else
            {
                _isGapDashing = true;
            }

            MoveAndCollide();
        }

        private void HandleStateChangeCallbacks()
        {
            if (World.IsFrozen)
                return;

            if (_shrinkOnNextUpdate)
            {
                DoShrinkLogic();
                _shrinkOnNextUpdate = false;
            }

            if (_endFireAcquireOnNextUpdate)
            {
                EndFireAcquire();
                _endFireAcquireOnNextUpdate = false;
            }
        }

        private void HandleStarman()
        {
            if (!IsStarman)
                return;

            _starmanTimer -= Time.DeltaTime;
            if (_starmanTimer <= 0f)
            {
                _useStarVisuals = false;
                ChangePalette(State == PlayerState.Fire ? PlayerPalette.Fire : PlayerPalette.Normal);
            }
        }

        private void HandleMovement()
        {
            bool running = Input.IsKeyDown(KeyboardKey.D);
            float accel = running ? RunAccel : WalkAccel;

            _inputDir = 0f;
            if (Input.IsKeyDown(KeyboardKey.Left)) _inputDir--;
            if (Input.IsKeyDown(KeyboardKey.Right)) _inputDir++;

            if (State != PlayerState.Small)
            {
                if (Input.IsKeyDown(KeyboardKey.Down)
                    && (_inputDir == 0f || !IsGrounded))
                {
                    Crouch();
                }
                else
                {
                    TryStandUp();
                } 
            }

            float stopFriction = IsGrounded ? StopFriction : StopFriction * 0.5f;

            // You should only be able to move if you are not crouching
            // or you're crouching but are in the air
            if (!_isCrouching || (_isCrouching && !IsGrounded))
            {
                if (Velocity.X >= 0f && _inputDir > 0f) // Normal walk right
                {
                    Velocity.X += accel * Time.DeltaTime;
                }
                else if (Velocity.X <= 0f && _inputDir < 0f) // Normal walk left
                {
                    Velocity.X -= accel * Time.DeltaTime;
                }
                else if (Velocity.X > 0f && _inputDir < 0f) // Skidding right
                {
                    Velocity.X -= (stopFriction + SkidFriction) * Time.DeltaTime;
                    _isSkidding = IsGrounded;
                }
                else if (Velocity.X < 0f && _inputDir > 0f) // Skidding left
                {
                    Velocity.X += (stopFriction + SkidFriction) * Time.DeltaTime;
                    _isSkidding = IsGrounded;
                }
            }

            // Stopping
            if (Velocity.X != 0f && (_inputDir == 0f || (_isCrouching && IsGrounded)))
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

            // Cap speed
            Velocity.X = Helper.Clamp(Velocity.X, -MaxRunSpeed, MaxRunSpeed);
            if ((!running || (_inputDir == 0f && IsGrounded)) && MathF.Abs(Velocity.X) > MaxWalkSpeed)
            {
                Velocity.X -= MathF.Sign(Velocity.X) * StopFriction * Time.DeltaTime;
            }

            HandleSkidEffect();
        }

        private void HandleSkidEffect()
        {
            if (!_isSkidding)
            {
                _skidEffectTimer = 0f;
                return;
            }

            _skidEffectTimer -= Time.DeltaTime;
            if (_skidEffectTimer <= 0f && MathF.Abs(Velocity.X) >= 32f * Game.TextureScale)
            {
                _skidEffectTimer = SkidEffectTime;
                World.SpawnEffect(Feet, EffectData.Skid);
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

        private void StopJump()
        {
            if (!_isJumping)
                return;

            _jumpTimer = MaxJumpTime;
            _isJumping = false;
        }

        private void HandleFireballThrowing()
        {
            _throwFireballCooldown -= Time.DeltaTime;

            if (Raylib.IsKeyPressed(KeyboardKey.D) 
                && State == PlayerState.Fire
                && _throwFireballCooldown <= 0f
                && !_isCrouching
                && World.FireballCount < 2)
            {
                ThrowFireball();
            }
        }

        private void ThrowFireball()
        {
            _throwFireballCooldown = FireballCooldown;

            if (Sprite.FlipX)
            {
                Fireball fireball = World.SpawnEntity(new Fireball(World, -1f));
                fireball.SetPosition(Position.X - 4f * Game.TextureScale, Position.Y);
            }
            else
            {
                Fireball fireball = World.SpawnEntity(new Fireball(World, 1f));
                fireball.SetPosition(Position.X + Size.X - 4f * Game.TextureScale, Position.Y);
            }
        }

        public void UpdateVisualLogic()
        {
            _iFrameTimer -= Time.DeltaTime;
            
            if (_useStarVisuals)
            {
                _starPaletteSwapTimer -= Time.DeltaTime;
                if (_starPaletteSwapTimer <= 0f)
                {
                    _starPaletteIndex++;
                    if (_starPaletteIndex >= 3)
                    {
                        _starPaletteIndex = 0;
                    }

                    ChangePalette((PlayerPalette)Enum.Parse(typeof(PlayerPalette), $"Star{_starPaletteIndex + 1}"));

                    if (IsStarman && _starmanTimer <= StarmanNearEndTime) _starPaletteSwapTimer = StarPaletteSwapTimeNearEnd;
                    else _starPaletteSwapTimer = StarPaletteSwapTime;
                }
            }
        }

        private void HandleAnimations()
        {
            if (World.IsFrozen)
                return;

            if (_isCrouching)
            {
                PlayAnim("crouch");
                return;
            }

            if (!IsGrounded && !_isGapDashing)
            {
                PlayAnim("jump");
                return;
            }

            if (_inputDir < 0f) Sprite.FlipX = true;
            else if (_inputDir > 0f) Sprite.FlipX = false;

            if (_throwFireballCooldown > 0f && 
                (!Sprite.CurrentAnimationName.Contains("run") || MathF.Abs(Velocity.X) <= MaxWalkSpeed * 1.1f))
            {
                PlayAnim("fireball");
                return;
            }

            if (Velocity.X == 0f)
            {
                PlayAnim("idle");
                return;
            }
            
            if (MathF.Sign(Velocity.X) != MathF.Sign(_inputDir) && _inputDir != 0f)
            {
                PlayAnim("skid");
                return;
            }

            PlayAnim("run");
            if (Sprite.CurrentAnimationName.Contains("run"))
            {
                for (int i = 0; i < Sprite.CurrentAnimation?.FrameTimes.Length; i++)
                {
                    float t = Helper.Clamp01(MathF.Abs(Velocity.X) / MaxRunSpeed);
                    Sprite.CurrentAnimation.FrameTimes[i] = Raymath.Lerp(0.2f, 0.067f, t);
                }
            }
        }

        private void PlayAnim(string name)
        {
            Sprite.Play($"{name}_{(State == PlayerState.Small ? "small" : "big")}");
        }

        public override void Draw()
        {
            Raylib.SetShaderValueTexture(_paletteShader, _paletteSamplerLoc, _palette);
            Raylib.BeginShaderMode(_paletteShader);

            if (IsInvincible)
            {
                if (_iFrameTimer % 0.2f < 0.1f)
                    DoDraw();
            }
            else
            {
                DoDraw();
            }

            Raylib.EndShaderMode();
            DrawDebug();
        }

        private void DoDraw()
        {
            Sprite.Draw(Position - SpriteOffset);
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

        protected override void OnEntityOverlap(Entity other)
        {
            if (other is Item item) HandleItemCollision(item);
            else if (other is Enemy enemy) HandleEnemyCollision(enemy);
        }

        private void HandleItemCollision(Item item)
        {
            item.Pickup();
            if (item is Mushroom && item is not OneUp)
            {
                Grow();
            }
            else if (item is FireFlower)
            {
                AcquireFire();
            }
            else if (item is Star)
            {
                AcquireStar();
            }
        }

        private void HandleEnemyCollision(Enemy enemy)
        {
            if (IsStarman)
            {
                enemy.Die();
                World.SpawnEffect(enemy.Center, EffectData.Hit);
                
                enemy.Velocity.X = MathF.Abs(enemy.Velocity.X);
                if (enemy.Center.X < Center.X)
                {
                    enemy.Velocity.X = -enemy.Velocity.X;
                }
                
                return;
            }

            if (enemy.IsStompable
                && !enemy.IsStomped
                && Velocity.Y > 0f
                && Feet.Y <= enemy.Feet.Y)
            {
                enemy.Stomp(this);
                Velocity.Y = -StompBounce;
                DisableOverlapUntilCollisionLeave(enemy);
            }
            else
            {
                if (enemy is ShellEnemy shell && shell.State == ShellState.Shell)
                {
                    shell.Kick(this);
                    DisableOverlapUntilCollisionLeave(enemy);
                }
                else
                {
                    TakeDamage();
                }
            }
        }
    }
}
