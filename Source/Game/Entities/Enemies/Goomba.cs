namespace Mario2
{
    public class Goomba : Enemy
    {
        public const float WalkSpeed = Tile.Size * 2f;
        private float _deathTimer = 1f;

        public Goomba(World world) : base(world)
        {
            Size = new Vector2(Tile.Size);

            Sprite.SetSheet("goomba");
            Sprite.Add("walk", new Animation(2, 16, 16, 0, 0.15f));
            Sprite.Add("stomp", new Animation(1, 16, 16, 16, 0.1f));

            Ai = new GoombaAi(this, WalkSpeed);
        }

        protected override void OnStomp(Entity stomper)
        {
            Sprite.Play("stomp");
            IsHittable = false;
            Velocity = Vector2.Zero;
            Ai = new DummyAi(this);
        }

        public override void Update()
        {
            if (IsStomped)
            {
                _deathTimer -= Time.DeltaTime;
                if (_deathTimer <= 0f)
                {
                    Destroy();
                }
            }

            base.Update();
        }
    }
}
