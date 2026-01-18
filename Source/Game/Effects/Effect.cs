namespace Mario2
{
    public class Effect
    {
        public const string AnimSkid = "skid";
        public const string AnimHit = "hit";
        public const string AnimBrick = "brick";

        public bool IsInUse => _lifeTimer > 0f;
        
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
            _sprite.Add(AnimBrick, new Animation(4, 8, 8, 38, 0.075f, isLooped: true));
        }

        public void Init(Vector2 position, Vector2 velocity, EffectData data)
        {
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
