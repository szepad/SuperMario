namespace Mario2
{
    public class BowserFire : Entity
    {
        private readonly float _targetY;

        private const float Speed = 100f * Game.TextureScale;
        private const float ParticleTime = 0.5f;
        private float _particleTimer = ParticleTime * 0.66f;

        public BowserFire(World world, float targetY)
            : base(world)
        {
            _targetY = targetY;

            Size = new Vector2(24f, 16f).Scale();
            SizeOffset = Size / 2f;
            SpriteOffset = SizeOffset;
            HitboxSize = new Vector2(14f, 8f).Scale();
            HitboxOffset = -SizeOffset + new Vector2(2f, 4f).Scale();

            LayerDepth = LayerDepth.Foreground;
            Gravity = 0f;
            IsCollidable = false;

            Sprite.SetSheet("bowser_fire");
            Sprite.Add("fire", new Animation(3, 24, 16, 0, 0.1f));

            Velocity.X = -Speed;
        }

        protected override void OnSpawn()
        {
            Activate();
        }

        public override void Update()
        {
            if (_targetY < Position.Y)
            {
                Position.Y -= Speed * Time.DeltaTime * 0.5f;
            }
            else if (_targetY > Position.Y)
            {
                Position.Y += Speed * Time.DeltaTime * 0.5f;
            }

            if (MathF.Abs(_targetY - Position.Y).CloseToZero())
            {
                Position.Y = _targetY;
            }

            _particleTimer += Time.DeltaTime;
            if (_particleTimer >= ParticleTime)
            {
                _particleTimer = 0f;
                World.SpawnEffect(Position + new Vector2(6f, 0f).Scale(), EffectData.BowserFireDrop);
            }

            base.Update();
        }
    }
}
