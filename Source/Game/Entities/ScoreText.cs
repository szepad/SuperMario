namespace Mario2
{
    public class ScoreText : Entity
    {
        private float _speed;
        private float _lifeTimer;

        public ScoreText(World world, int points, Vector2 pos,
            float lifeTime = ScoreRewards.NormalLifeTime, 
            float speed = ScoreRewards.NormalSpeed)
            : base(world)
        {
            SetPosition(pos);
            SpriteOffset = new Vector2(10f, 5f).Scale();

            Sprite.SetSheet("scores");
            Sprite.Add("100", new Animation(1, 20, 10, 0, 1f));
            Sprite.Add("200", new Animation(1, 20, 10, 10, 1f));
            Sprite.Add("400", new Animation(1, 20, 10, 20, 1f));
            Sprite.Add("500", new Animation(1, 20, 10, 30, 1f));
            Sprite.Add("800", new Animation(1, 20, 10, 40, 1f));
            Sprite.Add("1000", new Animation(1, 20, 10, 50, 1f));
            Sprite.Add("2000", new Animation(1, 20, 10, 60, 1f));
            Sprite.Add("4000", new Animation(1, 20, 10, 70, 1f));
            Sprite.Add("5000", new Animation(1, 20, 10, 80, 1f));
            Sprite.Add("8000", new Animation(1, 20, 10, 90, 1f));
            Sprite.Add("10000", new Animation(1, 20, 10, 100, 1f));

            Sprite.Play(points.ToString());

            LayerDepth = LayerDepth.Foreground;
            Gravity = 0f;
            IsHittable = false;
            IsCollidable = false;
            _speed = speed;
            _lifeTimer = lifeTime;
        }

        protected override void OnSpawn()
        {
            Activate();
        }

        public override void Update()
        {
            Position.Y -= _speed * Time.DeltaTime;
            _lifeTimer -= Time.DeltaTime;
            if (_lifeTimer <= 0f)
            {
                Destroy();
            }
        }
    }
}
