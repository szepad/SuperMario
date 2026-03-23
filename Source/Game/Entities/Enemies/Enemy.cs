namespace Mario2
{
    public abstract class Enemy : Entity
    {
        public bool IsStompable { get; set; } = true;
        public bool KillableWithTileBounce { get; set; } = true;
        public bool KillableWithFireball { get; set; } = true;
        public EnemyAi Ai { get; private set; }

        public bool IsDead { get; private set; }
        public bool IsStomped { get; private set; }

        protected const float DieBounceForce = 140f * Game.TextureScale;

        public Enemy(World world) : base(world)
        {
            Ai = new DummyAi(this);
        }

        protected override void OnSpawn()
        {
            Ai.Set();
        }

        public void SetAi(EnemyAi ai)
        {
            Ai = ai;
            Ai.Set();
        }

        public void Stomp(Entity stomper)
        {
            if (IsStomped || !IsStompable) return;
            IsStomped = true;

            SoundManager.PlaySound(Sounds.EnemyStomp);
            SessionManager.GetScore(World, 
                (this is RedFlyingKoopa rfk && rfk.IsWinged) ? ScoreRewards.FlyingKoopaStomp : ScoreRewards.GetEnemyStompScore(),
                Head, ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);

            ScoreRewards.EnemyStompCount++;

            OnStomp(stomper);
            Ai.OnStomp(stomper);
        }

        public void ResetStomped()
        {
            IsStomped = false;
        }

        public void Die(Entity killer)
        {
            if (IsDead) return;
            IsDead = true;

            IsHittable = false;
            IsCollidable = false;
            Velocity.Y = -DieBounceForce;
            Sprite.FlipY = true;
            LayerDepth = LayerDepth.Normal;
            Gravity = DefaultGravity;

            OnDie();
            Ai.OnDie();

            if (killer is ShellEnemy shell && shell.State == ShellState.Spin)
            {
                if (shell.KillCombo >= 8)
                {
                    SessionManager.GetLife();
                }

                // Incremental sound
                if (Enum.TryParse($"ShellCombo0{shell.KillCombo}", out Sounds sound))
                {
                    SoundManager.PlaySound(sound);
                }
                else
                {
                    SoundManager.PlaySound(Sounds.ShellCombo08);
                }
            }
            else
            {
                SoundManager.PlaySound(Sounds.ShellHit);
            }
        }

        public override void TileBounceHit(Entity bouncer, Tile tile)
        {
            if (KillableWithTileBounce)
            {
                FlipVelocityBasedOnTileBounce(tile);
                Die(this);
                World.SpawnEffect(Center, EffectData.Hit);
                SessionManager.GetScore(World, ScoreRewards.TileBounceKill, Head,
                    ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);
            }
        }

        protected void FlipVelocityBasedOnTileBounce(Tile tile)
        {
            if (Position.X + Size.X / 2f < (tile.Col + 0.5f) * Tile.Size)
            {
                Velocity.X = -MathF.Abs(Velocity.X);
            }
            else
            {
                Velocity.X = MathF.Abs(Velocity.X);
            }
        }

        public override void Update()
        {
            if (!IsDead)
            {
                Ai.Update();
            }

            base.Update();
        }

        protected void FlipTowardsVelocity()
        {
            if ((Velocity.X > 0f && !Sprite.FlipX) || (Velocity.X < 0f && Sprite.FlipX))
                Sprite.FlipX = !Sprite.FlipX;
        }

        protected virtual void OnStomp(Entity stomper) { }
        protected virtual void OnDie() { }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
        {
            Ai.OnTileCollision(tile, side);
        }

        protected override void OnSolidEntityCollision(Entity other, CollisionSide side)
        {
            Ai.OnSolidEntityCollision(other, side);
        }

        protected override void OnTileOverlap(Tile tile)
        {
            Ai.OnTileOverlap(tile);
        }

        protected override void OnEntityOverlap(Entity other)
        {
            Ai.OnEntityOverlap(other);
        }
    }
}
