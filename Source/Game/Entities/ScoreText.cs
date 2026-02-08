namespace Mario2
{
    public class ScoreText : Entity
    {
        public float Speed { get; set; }
        public float LifeTimer { get; set; }

        public int Points { get; }

        public ScoreText(World world, int points, Vector2 pos,
            float lifeTime = ScoreRewards.NormalLifeTime, 
            float speed = ScoreRewards.NormalSpeed)
            : base(world)
        {
            Speed = speed;
            LifeTimer = lifeTime;
            Points = points;

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
        }

        protected override void OnSpawn()
        {
            Activate();
        }

        public override void Update()
        {
            Position.Y -= Speed * Time.DeltaTime;
            LifeTimer -= Time.DeltaTime;
            if (LifeTimer <= 0f)
            {
                Destroy();
            }
        }
    }
}
