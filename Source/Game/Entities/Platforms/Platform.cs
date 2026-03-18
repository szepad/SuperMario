namespace Mario2
{
    public abstract class Platform : Entity
    {
        public PlatformType Type { get; private set; }

        protected const string Sprite_Long = "long";
        protected const string Sprite_Medium = "medium";
        protected const string Sprite_Short = "short";

        public Platform(World world) 
            : base(world)
        {
            Sprite.SetSheet("platform");
            Sprite.Add(Sprite_Long, new Animation(1, 48, 16, 0, 0.1f, false));
            Sprite.Add(Sprite_Medium, new Animation(1, 48, 16, 16, 0.1f, false));
            Sprite.Add(Sprite_Short, new Animation(1, 48, 16, 32, 0.1f, false));

            Resize(PlatformType.Long);
            Gravity = 0f;
            IsCollidable = false;
            IsHittable = false;
            IsSolid = true;

            SpriteOffset = new Vector2(24f, 4f).Scale();
        }

        public void Resize(PlatformType type)
        {
            Type = type;
            switch (type)
            {
                case PlatformType.Medium:
                    Size = new Vector2(32f, 8f).Scale();
                    Sprite.Play(Sprite_Medium);
                    break;

                case PlatformType.Short:
                    Size = new Vector2(24f, 8f).Scale();
                    Sprite.Play(Sprite_Short);
                    break;

                default:
                    Size = new Vector2(48f, 8f).Scale();
                    Sprite.Play(Sprite_Long);
                    break;
            }

            SizeOffset = Size / 2f;
        }

        protected override void OnSpawn()
        {

        }
    }
}
