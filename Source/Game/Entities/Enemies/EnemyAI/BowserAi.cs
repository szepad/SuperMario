using System.Reflection.Emit;

namespace Mario2
{
    public class BowserAi(Bowser bowser) 
        : EnemyAi(bowser)
    {
        public Bowser Bowser { get; } = bowser;

        public bool CanAct { get; set; } = true;
        public bool CanThrowHammers { get; set; } = false;

        private const float MoveSpeed = 15f * Game.TextureScale;
        private const float JumpForce = 130f * Game.TextureScale;

        private const float MinJumpWaitTime = 1.5f;
        private const float MaxJumpWaitTime = 2.75f;
        private float _jumpTimer = Rng.GetFloat(0f, 2f);

        private const float TryChangeDirTime = 1f;
        private float _dirChangeTimer;

        private const float LeftMoveBounds = 8 * Tile.Size;
        private const float RightMoveBounds = 3 * Tile.Size;
        private float _originX;

        public const float MinBreathWaitTime = 2f;
        public const float MaxBreathWaitTime = 4f;
        public const float BreathAfterTime = 0.5f;
        private float _breathPrepareTimer = Rng.GetFloat(0.2f, 2f);
        private float _doBreathTimer;
        private bool _doBreath;

        private const float MinHammerThrowWaitTime = 0.75f;
        private const float MaxHammerThrowWaitTime = 2f;
        private const float TimeBetweenThrow = 0.125f;
        private const int MinHammerAmount = 1;
        private const int MaxHammerAmount = 7;

        private int _currentHammerAmount;
        private float _hammerThrowTimer;
        private bool _isThrowingHammers;

        private bool _hammerThrowTelegraph;

        protected override void OnSet()
        {
            _originX = Bowser.Feet.X;
            base.OnSet();
        }

        public override void Update()
        {
            if (!CanAct)
                return;

            HandleFlipping();

            if (!Bowser.Sprite.FlipX)
            {
                // Facing right
                Bowser.Velocity.X = MoveSpeed * 1.75f;
                _hammerThrowTelegraph = false;
                _doBreath = false;
                Bowser.Sprite.Play(Bowser.WalkAnim);
            }
            else
            {
                HandleDirectionChanging();
                HandleJumping();
                HandleFireBreathing();
            }
        }

        private void HandleFlipping()
        {
            if (Bowser.World.Player.IsDead)
            {
                Bowser.Sprite.FlipX = true;
            }
            else if (Bowser.IsGrounded)
            {
                Bowser.Sprite.FlipX = Bowser.World.Player.Center.X < Bowser.Center.X;
            }
        }

        private void HandleDirectionChanging()
        {
            if (!Bowser.IsGrounded)
                return;

            _dirChangeTimer -= Time.DeltaTime;
            if (_dirChangeTimer <= 0f)
            {
                _dirChangeTimer = TryChangeDirTime;
                Bowser.Velocity.X = Rng.GetInt(0, 2) == 0
                    ? -MoveSpeed : MoveSpeed;
            }

            if (Bowser.Center.X < _originX - LeftMoveBounds)
            {
                Bowser.Velocity.X = MoveSpeed;
                _dirChangeTimer = TryChangeDirTime;
            }
            else if (Bowser.Center.X > _originX + RightMoveBounds)
            {
                Bowser.Velocity.X = -MoveSpeed;
                _dirChangeTimer = TryChangeDirTime;
            }
        }

        private void HandleJumping()
        {
            if (!Bowser.IsGrounded)
                return;

            _jumpTimer -= Time.DeltaTime;
            if (_jumpTimer <= 0f)
            {
                Bowser.Velocity.Y = -JumpForce;
                _jumpTimer = Rng.GetFloat(MinJumpWaitTime, MaxJumpWaitTime);
            }
        }

        private void HandleFireBreathing()
        {
            _breathPrepareTimer -= Time.DeltaTime;
            if (_breathPrepareTimer <= 0f)
            {
                Bowser.Sprite.Play(Bowser.SpitAnim_Prep);
                _breathPrepareTimer = Rng.GetFloat(MinBreathWaitTime, MaxBreathWaitTime);
                _doBreathTimer = BreathAfterTime;
                _doBreath = true;
            }

            if (_doBreath)
            {
                _doBreathTimer -= Time.DeltaTime;
                if (_doBreathTimer <= 0f && Bowser.IsGrounded)
                {
                    _doBreath = false;
                    Bowser.Sprite.Play(Bowser.SpitAnim_Open);

                    float targetY = Bowser.Head.Y + 5f.Scale();

                    switch (Rng.GetInt(0, 3))
                    {
                        case 1: // High
                            targetY -= Tile.Size * 1.25f;
                            break;
                        case 2: // Low
                            targetY += Tile.Size;
                            break;
                    }

                    BowserFire fire = Bowser.World.SpawnEntity(
                        new BowserFire(Bowser.World, targetY));

                    fire.SetPosition(Bowser.Head + new Vector2(-14f, 11f).Scale());
                }
            }
        }
    }
}
