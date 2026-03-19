namespace Mario2
{
    public enum ShellState
    {
        Normal,
        Shell,
        Spin,
    }

    public abstract class ShellEnemy : Enemy
    {
        public ShellState State { get; private set; } = ShellState.Normal;

        public int KillCombo { get; private set; } = 0;

        protected const string WalkAnim = "walk";
        protected const string ShellAnim = "shell";
        protected const string ShellSpinAnim = "shell_spin";
        protected const string WakeUpAnim = "wake_up";

        protected EnemyAi NormalAi { get; set; }
        private readonly EnemyAi _spinAi;

        private const float StandUpTime = 10f;
        private const float ShakeAnimTime = 1.5f;
        private float _standUpTimer;

        public ShellEnemy(World world) : base(world)
        {
            KillableWithTileBounce = false;
            _spinAi = new GoombaAi(this, Goomba.WalkSpeed * 5f)
            {
                ChangeDirOnEnemyOverlap = false
            };

            NormalAi = new DummyAi(this);
        }

        protected override void OnActivate()
        {
            SetAi(NormalAi);
            base.OnActivate();
        }

        public override void Update()
        {
            if (State == ShellState.Shell && !IsDead)
            {
                _standUpTimer += Time.DeltaTime;
                if (_standUpTimer >= StandUpTime)
                {
                    StandUp();
                }
                else if (_standUpTimer > StandUpTime - ShakeAnimTime)
                {
                    Sprite.Play(WakeUpAnim);
                }
            }
            else
            {
                _standUpTimer = 0f;
            }

             base.Update();
        }

        private void StandUp()
        {
            State = ShellState.Normal;
            Sprite.Play(WalkAnim);
            SetAi(NormalAi);
            Sprite.FlipY = false;
        }

        public void Kick(Player player)
        {
            State = ShellState.Spin;
            Sprite.Play(ShellSpinAnim);
            SetAi(_spinAi);
            Velocity.X = player.Feet.X < Feet.X
                ? MathF.Abs(Velocity.X)
                : -MathF.Abs(Velocity.X);

            Vector2 effectPos = Raymath.Vector2Lerp(player.Feet, Position + Size / 2f, 0.5f);
            player.World.SpawnEffect(effectPos, EffectData.Hit);

            KillCombo = 0;
            SoundManager.PlaySound(Sounds.ShellHit);

            SessionManager.GetScore(World, ScoreRewards.Kick, Head,
                ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);
        }

        public override void TileBounceHit(Entity bouncer, Tile tile)
        {
            base.TileBounceHit(bouncer, tile);

            float preserveVelX = Velocity.X;
            EnterShellState();
            Sprite.FlipY = true;
            Velocity.X = preserveVelX;
            FlipVelocityBasedOnTileBounce(tile);
            Velocity.Y = -DieBounceForce;
            _standUpTimer = 0f;

            SessionManager.GetScore(World, ScoreRewards.TileBounceKill, Head,
                ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);
        }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
        {
            OnSolidHit(side);
            base.OnTileCollision(tile, side);
        }

        protected override void OnSolidEntityCollision(Entity other, CollisionSide side)
        {
            OnSolidHit(side);
            base.OnSolidEntityCollision(other, side);
        }

        private void OnSolidHit(CollisionSide side)
        {
            if (side == CollisionSide.Bottom && State == ShellState.Shell)
            {
                // Landed from tile bounce
                Velocity.X = 0f;
            }

            if (State == ShellState.Spin && side != CollisionSide.Top && side != CollisionSide.Bottom)
            {
                float dir = 1f;
                if (side == CollisionSide.Left) dir = -1f;

                World.SpawnEffect(Center + new Vector2(dir * Tile.Size / 2f, 0f), EffectData.Hit);
                SoundManager.PlaySound(Sounds.PlayerHitBlock);
            }
        }

        protected override void OnStomp(Entity stomper)
        {
            if (State == ShellState.Shell && stomper is Player player)
            {
                Kick(player);
            }
            else
            {
                EnterShellState();
            }

            ResetStomped();
            base.OnStomp(stomper);
        }

        protected override void OnEntityOverlap(Entity other)
        {
            if (State == ShellState.Spin && other is Enemy enemy)
            {
                SessionManager.GetScore(World, ScoreRewards.GetShellKillScore(KillCombo), enemy.Head,
                    ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);

                KillCombo++;
                enemy.Die(this);
                if (other is ShellEnemy shell && shell.State == ShellState.Spin)
                {
                    Die(shell);
                    World.SpawnEffect(Center, EffectData.Hit);
                }

                World.SpawnEffect(enemy.Center, EffectData.Hit);
            }

            base.OnEntityOverlap(other);
        }

        protected void EnterShellState()
        {
            State = ShellState.Shell;
            Sprite.Play(ShellAnim);
            SetAi(new DummyAi(this));
            Velocity = Vector2.Zero;
        }
    }
}
