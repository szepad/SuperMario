namespace Mario2
{
    public class Effect
    {
        public const string AnimSkid = "skid";
        public const string AnimHit = "hit";
        public const string AnimBrickOw = "brick_ow";
        public const string AnimBrickUg = "brick_ug";
        public const string AnimBrickCastle = "brick_castle";
        public const string BowserFireDrop = "bowser_fire_drop";
        public const string BowserLavaSplash = "bowser_lava_splash";
        public const string BridgeDebris = "bridge_debris";

        public bool IsInUse => _lifeTimer > 0f;
        public bool IsBackground { get; private set; }

        private readonly Sprite _sprite = new("effects");
        
        private float _lifeTimer;
        private Vector2 _spriteOffset;
        private Vector2 _position;
        private Vector2 _velocity;
        private float _gravity;

        public Effect()
        {
            _sprite.Add(AnimSkid, new Animation(4, 10, 10, 0, 0.075f, isLooped: false));
            _sprite.Add(AnimHit, new Animation(7, 29, 28, 10, 0.025f, isLooped: false));
            _sprite.Add(AnimBrickOw, new Animation(4, 8, 8, 38, 0.075f, isLooped: true));
            _sprite.Add(AnimBrickUg, new Animation(4, 8, 8, 46, 0.075f, isLooped: true));
            _sprite.Add(AnimBrickCastle, new Animation(4, 8, 8, 54, 0.075f, isLooped: true));
            _sprite.Add(BowserFireDrop, new Animation(4, 8, 8, 62, 0.15f, isLooped: false));
            _sprite.Add(BowserLavaSplash, new Animation(6, 44, 16, 70, 0.075f, isLooped: false));
            _sprite.Add(BridgeDebris, new Animation(9, 16, 40, 86, 0.075f, isLooped: false));
        }

        public void Init(Vector2 position, Vector2 velocity, EffectData data)
        {
            IsBackground = data.IsBackground;
            _position = position;
            _velocity = velocity;
            _sprite.ForcePlay(data.AnimName);
            _lifeTimer = data.LifeTime;
            _spriteOffset = data.SpriteOffset;
            _gravity = data.Gravity;
        }

        public void Update()
        {
            _sprite.Update();
            _lifeTimer -= Time.DeltaTime;

            _velocity.Y += _gravity * Time.DeltaTime;
            _position += _velocity * Time.DeltaTime;
        }

        public void Draw()
        {
            _sprite.Draw(_position - _spriteOffset);
        }
    }
}
