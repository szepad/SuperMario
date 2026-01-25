namespace Mario2
{
    public class Flagpole : Entity
    {
        public float FlagRelY { get; set; } = 1f.Scale();

        public Flagpole(World world) : base(world)
        {
            Size = HitboxSize = new Vector2(4f.Scale(), Tile.Size * 9f);
            SpriteOffset = new Vector2(-2f, 0f).Scale();

            IsCollidable = false;
            Gravity = 0f;

            Sprite.SetSheet("flag");
            Sprite.Add("flag", new Animation(3, 16, 16, 0, 0.15f));
        }

        protected override void OnSpawn()
        {

        }

        public override void Update()
        {
            SpriteOffset.Y = -FlagRelY;

            base.Update();
        }
    }
}
