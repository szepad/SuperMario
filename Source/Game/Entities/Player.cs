using System.Diagnostics.CodeAnalysis;

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

    public enum PlayerAnim
    {
        Idle,
        Run,
        Skid,
        Jump,
        Climb,
        ClimbStop,
        Death,
        Grow,
        Crouch,
        Fireball,
        Emote
    }

    public class Player : Entity
    {
        public event Action? OnDeath;

        public bool IsDead { get; private set; }
        public PlayerState State { get; private set; } = PlayerState.Small;

        public bool IsInvincible => _iFrameTimer > 0f || _invAfterStomp > 0f;
        public bool IsStarman => _starmanTimer > 0f;

        [MemberNotNullWhen(true, nameof(_cutscene))]
        public bool IsCutscenePlaying => _cutscene is not null;

        private const float MaxWalkSpeed = 80f * Game.TextureScale;
        private const float MaxRunSpeed = 150f * Game.TextureScale;
        private const float WalkAccel = 160f * Game.TextureScale;
        private const float RunAccel = 170f * Game.TextureScale;
        private const float StopFriction = 300f * Game.TextureScale;
        private const float SkidFriction = 100f * Game.TextureScale;

        private float _inputDir;

        private const float MinJumpForce = 180f * Game.TextureScale;
        private const float MaxJumpForce = 205f * Game.TextureScale;
        private const float MaxJumpTime = 0.3f;
        private float _jumpTimer;
        private bool _isJumping;

        private const float JumpBufferTime = 0.06f;
        private const float CoyoteTime = 0.05f;

        private float _jumpBuffer;
        private float _coyoteTimer;

        private const float StompBounce = 180f * Game.TextureScale;

        private const float IFrames = 3.5f;
        private float _iFrameTimer;

        private bool _hasJumped;
        private bool _isCrouching;

        private bool _shrinkOnNextUpdate;

        private bool _isSkidding;
        private float _skidEffectTimer;
        private const float SkidEffectTime = 0.075f;

        private readonly PaletteShader _palette = new(Assets.GetTex("palette_mario"), 5);

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

        private const float DeathJumpTime = 0.35f;
        private const float DeathJumpForce = 270f * Game.TextureScale;
        private float _deathJumpTimer;

        private ICutscene? _cutscene;

        // Player should be invincible for a short time after stomping an enemy
        // Because his velocity is set upwards, but if he collided with 2 enemies
        // At once, the second one will kill him.
        private const float InvincibilityAfterStomp = 0.1f;
        private float _invAfterStomp;

        public Player(World world) : base(world)
        {
            IsEditorSaveable = false;

            SetSmallStateVars();
            LayerDepth = LayerDepth.Player;

            Sprite.SetSheet("mario");
            Sprite.Add($"{PlayerAnim.Idle}_small", new Animation(1, 16, 16, 0, 1f));
            Sprite.Add($"{PlayerAnim.Run}_small", new Animation(3, 16, 16, 16, 0.1f));
            Sprite.Add($"{PlayerAnim.Skid}_small", new Animation(1, 16, 16, 32, 1f));
            Sprite.Add($"{PlayerAnim.Jump}_small", new Animation(1, 16, 16, 48, 1f));
            Sprite.Add($"{PlayerAnim.Climb}_small", new Animation(2, 16, 16, 64, 0.15f));
            Sprite.Add($"{PlayerAnim.ClimbStop}_small", new Animation(1, 16, 16, 64, 0.15f));
            Sprite.Add($"{PlayerAnim.Death}_small", new Animation(1, 16, 16, 80, 1f));
            Sprite.Add($"{PlayerAnim.Emote}_small", new Animation(2, 16, 16, 96, 0.5f, isLooped: false));

            // Big
            Sprite.Add($"{PlayerAnim.Grow}_big", new Animation(2, 24, 32, 128, 0.1f));
            Sprite.Add($"{PlayerAnim.Idle}_big", new Animation(1, 24, 32, 128 + 32, 1f));
            Sprite.Add($"{PlayerAnim.Run}_big", new Animation(3, 24, 32, 128 + 32 * 2, 0.1f));
            Sprite.Add($"{PlayerAnim.Skid}_big", new Animation(1, 24, 32, 128 + 32 * 3, 1f));
            Sprite.Add($"{PlayerAnim.Jump}_big", new Animation(1, 24, 32, 128 + 32 * 4, 1f));
            Sprite.Add($"{PlayerAnim.Crouch}_big", new Animation(1, 24, 32, 128 + 32 * 5, 1f));
            Sprite.Add($"{PlayerAnim.Fireball}_big", new Animation(1, 24, 32, 128 + 32 * 6, 1f));
            Sprite.Add($"{PlayerAnim.Climb}_big", new Animation(2, 24, 32, 128 + 32 * 7, 0.15f));
            Sprite.Add($"{PlayerAnim.ClimbStop}_big", new Animation(1, 24, 32, 128 + 32 * 7, 0.15f));
            Sprite.Add($"{PlayerAnim.Emote}_big", new Animation(1, 24, 32, 128 + 32 * 8, 0.5f, isLooped: false));
        }

        protected override void OnSpawn()
        {
            
        }

        protected override void OnDestroy()
        {
            
        }

        public void StartCutscene(ICutscene cutscene)
        {
            EndStar(true, music: false);
            _cutscene = cutscene;
            cutscene.Start();
        }

        public void EndCutscene()
        {
            _cutscene = null;
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

        // This method isn't called if player has fallen off of the screen
        public void Die()
        {
            if (IsDead) return;
            IsDead = true;

            _deathJumpTimer = DeathJumpTime;
            IsHittable = false;
            IsCollidable = false;
            Velocity = Vector2.Zero;
            Gravity = 0f;
            SetStateInstantNoSideFx(PlayerState.Small);
            PlayAnim(PlayerAnim.Death);

            OnDeath?.Invoke();
        }

        public void SetStateInstantNoSideFx(PlayerState state)
        {
            switch (state)
            {
                case PlayerState.Small:
                    ForceStandUp();
                    SetSmallStateVars();
                    ChangePalette(PlayerPalette.Normal);
                    break;
                case PlayerState.Big:
                    SetBigStateVarsX();
                    SetBigStateVarsY();
                    ChangePalette(PlayerPalette.Normal);
                    break;
                case PlayerState.Fire:
                    SetBigStateVarsX();
                    SetBigStateVarsY();
                    ChangePalette(PlayerPalette.Fire);
                    break;
            }

            State = state;
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
            PlayAnim(PlayerAnim.Grow);

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
            SoundManager.PlayMusic(MusicName.Invincible);
        }

        private void EndStar(bool checkIfStarman, bool music = true)
        {
            if (checkIfStarman && !IsStarman)
                return;

            _useStarVisuals = false;
            ChangePalette(State == PlayerState.Fire ? PlayerPalette.Fire : PlayerPalette.Normal);

            if (music) World.PlayMusic();
            else SoundManager.StopMusic();
        }

        private void ChangePalette(PlayerPalette palette)
        {
            _palette.CurrentRow = palette switch
            {
                PlayerPalette.Fire => 1,
                PlayerPalette.Star1 => 2,
                PlayerPalette.Star2 => 3,
                PlayerPalette.Star3 => 4,
                _ => 0
            };
        }

        private void Shrink()
        {
            if (_shrinkOnNextUpdate) return;
            _shrinkOnNextUpdate = true;

            _iFrameTimer = IFrames;
            World.Freeze(1f);
            PlayAnim(PlayerAnim.Grow);
            ChangePalette(PlayerPalette.Normal);

            SoundManager.PlaySound(Sounds.PlayerWarp);
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

        public void TryStandUp()
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

        public void ForceStandUp()
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
            _invAfterStomp -= Time.DeltaTime;

            HandleStateChangeCallbacks();

            if (!IsDead)
            {
                if (IsCutscenePlaying)
                {
                    UpdateVisualLogic();
                    _cutscene.Update();
                    if (_cutscene.IsFinished)
                    {
                        _cutscene = null;
                    }
                }
                else
                {
                    HandleStarman();
                    HandleMovement();
                    HandleJumping();
                    HandleFireballThrowing();
                    UpdateVisualLogic();
                    HandleAnimations();
                }
            }
            else
            {
                if (_deathJumpTimer > 0f)
                {
                    _deathJumpTimer -= Time.DeltaTime;
                    if (_deathJumpTimer <= 0f)
                    {
                        Gravity = DefaultGravity;
                        Velocity.Y = -DeathJumpForce;
                    }
                }
            }

            base.Update();

            if (IsGrounded)
            {
                ScoreRewards.EnemyStompCount = 0;
            }
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
                EndStar(false);
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
                SoundManager.PlaySound(Sounds.PlayerSkid);
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
                _hasJumped = false;
            }

            _jumpBuffer -= Time.DeltaTime;
            _coyoteTimer -= Time.DeltaTime;

            if (_coyoteTimer > 0f && _jumpBuffer > 0f)
            {
                _coyoteTimer = 0f;
                _jumpBuffer = 0f;

                float t = Helper.Clamp01(MathF.Abs(Velocity.X) / MaxRunSpeed);
                Velocity.Y = -Raymath.Lerp(MinJumpForce, MaxJumpForce, t);

                _isJumping = true;
                _hasJumped = true;
                _jumpTimer = 0f;

                SoundManager.PlaySound(Sounds.PlayerJump);
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

            SoundManager.PlaySound(Sounds.PlayerFireball);
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
                PlayAnim(PlayerAnim.Crouch);
                return;
            }

            if (_hasJumped)
            {
                PlayAnim(PlayerAnim.Jump);
                return;
            }

            if (IsGrounded)
            {
                if (_inputDir < 0f) Sprite.FlipX = true;
                else if (_inputDir > 0f) Sprite.FlipX = false;

                bool runAnimPlaying = Sprite.CurrentAnimationName.Contains("run", StringComparison.CurrentCultureIgnoreCase);
                if (_throwFireballCooldown > 0f && 
                    (!runAnimPlaying || MathF.Abs(Velocity.X) <= MaxWalkSpeed * 1.1f))
                {
                    PlayAnim(PlayerAnim.Fireball);
                    return;
                }

                if (Velocity.X == 0f)
                {
                    PlayAnim(PlayerAnim.Idle);
                    return;
                }
            
                if (MathF.Sign(Velocity.X) != MathF.Sign(_inputDir) && _inputDir != 0f)
                {
                    PlayAnim(PlayerAnim.Skid);
                    return;
                }

                PlayAnim(PlayerAnim.Run);

                float t = Helper.Clamp01(MathF.Abs(Velocity.X) / MaxRunSpeed);
                SetRunAnimFrameTimes(Raymath.Lerp(0.135f, 0.067f, t));
            }
            else
            {
                PlayAnim(PlayerAnim.Run);
                SetRunAnimFrameTimes(float.PositiveInfinity);
            }
        }

        public void SetRunAnimFrameTimes(float time = 0.1f)
        {
            if (Sprite.Animations.TryGetValue("Run_small", out Animation? anim))
            {
                for (int i = 0; i < anim.FrameTimes.Length; i++)
                {
                    anim.FrameTimes[i] = time;
                }
            }

            if (Sprite.Animations.TryGetValue("Run_big", out Animation? anim2))
            {
                for (int i = 0; i < anim2.FrameTimes.Length; i++)
                {
                    anim2.FrameTimes[i] = time;
                }
            }
        }

        public void PlayAnim(PlayerAnim anim)
        {
            Sprite.Play($"{anim}_{(State == PlayerState.Small ? "small" : "big")}");
        }

        public override void Draw()
        {
            _palette.Begin(Sprite.Opacity);

            if (IsInvincible)
            {
                if (_iFrameTimer % 0.2f < 0.1f)
                    DoDraw();
            }
            else
            {
                DoDraw();
            }

            _palette.End();
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
                SoundManager.PlaySound(Sounds.PlayerHitBlock);

                if (tile is ContainerTile ct)
                {
                    ct.OnHit(this);
                }
            }
            else if (side == CollisionSide.Right)
            {
                if (IsGrounded && CollidingWithPipeEntity(out WarpPipe? pipe) 
                    && pipe.Dir == PipeDirection.Right
                    && _inputDir > 0f)
                {
                    StartCutscene(new PipeEnterRightCutscene(this, pipe));
                }
            }

            _cutscene?.OnTileCollision(tile, side);
        }

        protected override void OnSolidEntityCollision(Entity other, CollisionSide side)
        {
            if (side == CollisionSide.Top)
            {
                StopJump();
                SoundManager.PlaySound(Sounds.PlayerHitBlock);
            }
            
            _cutscene?.OnSolidEntityCollision(other, side);
        }

        public bool CollidingWithPipeEntity([NotNullWhen(true)] out WarpPipe? pipe)
        {
            foreach (Entity entity in World.Entities)
            {
                if (entity is WarpPipe pipeCast &&
                    Raylib.CheckCollisionRecs(Rect, entity.Rect))
                {
                    pipe = pipeCast;
                    return true;
                }
            }

            pipe = null;
            return false;
        }

        protected override void OnTileOverlap(Tile tile)
        {
            if (tile is CoinTile)
            {
                tile.Destroy();
                SessionManager.GetCoin();
                SessionManager.GetScoreWithoutText(ScoreRewards.Coin);
            }
        }

        protected override void OnEntityOverlap(Entity other)
        {
            if (IsCutscenePlaying)
                return;

            if (other is Item item) HandleItemCollision(item);
            else if (other is Enemy enemy) HandleEnemyCollision(enemy);
            else
            {
                if (other is Flagpole flag)
                {
                    flag.OnHit();
                    StartCutscene(new FlagpoleCutscene(this, flag));
                }
                else if (other is WarpPipe pipe)
                {
                    if (pipe.Dir == PipeDirection.Down)
                    {
                        const float EnterThreshold = 4f * Game.TextureScale;
                        if (IsGrounded && Input.IsKeyDown(KeyboardKey.Down)
                            && MathF.Abs(pipe.Feet.X - Feet.X) < EnterThreshold)
                        {
                            StartCutscene(new PipeEnterDownCutscene(this, pipe));
                        }
                    }
                }
            }
        }

        private void HandleItemCollision(Item item)
        {
            int score = ScoreRewards.PowerUp;
            bool oneUp = item is OneUp;

            if (oneUp) score = ScoreRewards.OneUp;
            else SoundManager.PlaySound(Sounds.PlayerPowerup);

            item.Pickup();
            if (item is Mushroom && !oneUp)
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

            SessionManager.GetScore(World, score, item.Head, speed: ScoreRewards.FasterSpeed);
        }

        private void HandleEnemyCollision(Enemy enemy)
        {
            if (IsStarman)
            {
                enemy.Die(this);
                World.SpawnEffect(enemy.Center, EffectData.Hit);
                SessionManager.GetScore(World, ScoreRewards.StarKill, enemy.Head,
                    ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);
                
                enemy.Velocity.X = MathF.Abs(enemy.Velocity.X);
                if (enemy.Center.X < Center.X)
                {
                    enemy.Velocity.X = -enemy.Velocity.X;
                }
                
                return;
            }

            if (enemy is ShellEnemy shell && shell.State == ShellState.Shell)
            {
                shell.Kick(this);
                DisableOverlapUntilCollisionLeave(enemy);
            }
            else if (enemy.IsStompable
                && !enemy.IsStomped
                && Velocity.Y > 0f
                && Feet.Y <= enemy.Feet.Y)
            {
                enemy.Stomp(this);
                Velocity.Y = -StompBounce;
                DisableOverlapUntilCollisionLeave(enemy);
                _invAfterStomp = InvincibilityAfterStomp;
            }
            else
            {
                TakeDamage();
            }
        }
    }
}
