namespace Mario2
{
    public class Player : Entity
    {
        private const float MoveSpeed = 120f;
        private const float JumpForce = 225f;

        public Player(World world) : base(world)
        {
            Size = new Vector2(8f, 14f);
            SpriteOffset = new Vector2(4f, 2f);

            Sprite.SetSheet("mario");
            Sprite.Add("idle", new Animation(1, 16, 16, 0, 1f));
            Sprite.Add("run", new Animation(3, 16, 16, 16, 0.1f));
        }

        public override void Update()
        {
            if (Input.IsKeyPressed(KeyboardKey.A))
            {
                Velocity.Y = -JumpForce;
            }

            float dir = 0f;
            if (Input.IsKeyDown(KeyboardKey.Left)) dir--;
            if (Input.IsKeyDown(KeyboardKey.Right)) dir++;
            Velocity.X = dir * MoveSpeed;
            
            if (Velocity.X == 0f) Sprite.Play("idle");
            else
            {
                Sprite.Play("run");
                Sprite.FlipX = Velocity.X < 0f;
            } 

            base.Update();
        }
    }
}
