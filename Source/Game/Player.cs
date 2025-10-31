namespace Mario2
{
    public class Player : Entity
    {
        public Player()
        {
            Sprite.SetSheet("mario");
            Sprite.Add("idle", new Animation(1, 16, 16, 0, 1f));
            Sprite.Add("run", new Animation(3, 16, 16, 16, 0.1f));
        }

        public override void Update()
        {
            if (Input.IsKeyDown(KeyboardKey.Left))
            {
                Sprite.Play("run");
            }
            else
            {
                Sprite.Play("idle");
            }

            base.Update();
        }
    }
}
