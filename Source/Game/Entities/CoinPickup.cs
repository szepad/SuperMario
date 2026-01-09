namespace Mario2
{
    public class CoinPickup : Item
    {
        private const float ExplodeTime = 0.7f;
        private const float LayerChangeTime = 0.92f;
        private float _lifeTimer = 1f;

        private bool _isExploded;

        public CoinPickup(World world) : base(world)
        {
            IsCollidable = false;
            Size = new Vector2(10f, 14f).Scale();
            SpriteOffset = new Vector2(5f, 0f).Scale();

            Sprite.SetSheet("coin_spin");
            Sprite.Add("coin", new Animation(4, 20, 16, 0, 0.075f));
            Sprite.Add("explode", new Animation(6, 20, 16, 16, 0.1f, isLooped: false));

            Gravity *= 1.5f;
            LayerDepth = LayerDepth.Background;
        }

        protected override void OnSpawn()
        {
            Velocity.Y = -350f * Game.TextureScale;
            Position.Y -= 2f * Game.TextureScale;
        }

        protected override void OnDestroy()
        {

        }

        public override void Update()
        {
            _lifeTimer -= Time.DeltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy();
            }
            else if (_lifeTimer <= ExplodeTime && !_isExploded)
            {
                Gravity = DefaultGravity * 0.1f;
                Velocity = Vector2.Zero;
                Sprite.Play("explode");
                _isExploded = true;
            }
            else if (_lifeTimer <= LayerChangeTime)
            {
                LayerDepth = LayerDepth.Normal;
            }

                base.Update();
        }
    }
}
