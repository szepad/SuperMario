namespace Mario2
{
    public class Fireball : Entity
    {
        public bool HasImpacted { get; private set; }
        public event Action<Fireball>? OnImpact;

        private float _impactTimer;
        private const float ImpactTime = 0.3f;

        private static readonly Vector2 LaunchVelocity = new Vector2(225f, 167f).Scale();
        private const float BounceForce = 160f * Game.TextureScale;

        public Fireball(World world, float dir) : base(world)
        {
            Size = HitboxSize = new Vector2(8f).Scale();
            SpriteOffset = new Vector2(4f).Scale();

            Sprite.SetSheet("fireball");
            Sprite.Add("fireball", new Animation(4, 16, 16, 0, 0.075f));
            Sprite.Add("impact", new Animation(4, 16, 16, 16, 0.05f, isLooped: false));

            Velocity = LaunchVelocity;
            Velocity.X *= dir;

            Gravity *= 0.8f;
        }

        protected override void OnSpawn()
        {
            
        }

        public void Impact()
        {
            if (HasImpacted) return;
            HasImpacted = true;

            Sprite.Play("impact");
            _impactTimer = ImpactTime;
            Gravity = 0f;
            Velocity = Vector2.Zero;
            IsHittable = false;
            IsCollidable = false;

            OnImpact?.Invoke(this);

            SoundManager.PlaySound(Sounds.PlayerHitBlock);
        }

        public override void Update()
        {
            if (HasImpacted)
            {
                _impactTimer -= Time.DeltaTime;
                if (_impactTimer <= 0f)
                {
                    Destroy();
                }
            }

            base.Update();
        }

        protected override void OnTileCollision(Tile tile, CollisionSide side)
            => OnSolidHit(side);

        protected override void OnSolidEntityCollision(Entity other, CollisionSide side)
            => OnSolidHit(side);

        private void OnSolidHit(CollisionSide side)
        {
            if (side == CollisionSide.Bottom)
            {
                Velocity.Y = -BounceForce;
                Gravity = DefaultGravity * 1.35f;
            }
            else
            {
                Impact();
            }
        }

        protected override void OnEntityOverlap(Entity other)
        {
            if (other is Enemy enemy)
            {
                Impact();

                if (enemy.KillableWithFireball)
                {
                    enemy.Velocity.X = MathF.Abs(enemy.Velocity.X);
                    if (enemy.Center.X < Center.X)
                    {
                        enemy.Velocity.X = -enemy.Velocity.X;
                    }

                    enemy.Die(this);
                    SessionManager.GetScore(World, ScoreRewards.Fireball, enemy.Head,
                        ScoreRewards.ShorterLifeTime, ScoreRewards.FasterSpeed);
                }
            }
            else if (other is Flagpole)
            {
                Impact();
            }
        }

        public override void OnCameraLeaveX()
        {
            Destroy();
        }
    }
}
