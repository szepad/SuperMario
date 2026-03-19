namespace Mario2
{
    public class PiranhaPlant : Enemy
    {
        public Palette Palette { get; private set; } = Palette.Overworld;

        public PiranhaPlant(World world)
            : base(world)
        {
            Sprite.SetSheet("piranha_plant");
            Sprite.Add("eat", new Animation(2, 16, 24, 0, 0.1f));

            Size = new Vector2(16f, 24f).Scale();
            HitboxSize = new Vector2(12f, 22f).Scale();
            HitboxOffset = new Vector2(2f, 2f).Scale();

            IsEditorSaveable = false;
            IsStompable = false;
            IsCollidable = false;
            Gravity = 0f;
            LayerDepth = LayerDepth.Background;
        }

        public void SetPalette(Palette palette)
        {
            Palette = palette;
            switch (palette)
            {
                case Palette.Overworld: Sprite.SetSheet("piranha_plant"); break;
                case Palette.Underground: Sprite.SetSheet("piranha_plant_ug"); break;
                case Palette.Castle: Sprite.SetSheet("piranha_plant_ug"); break;
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();

            switch (World.Background)
            {
                case WorldBackground.Underground: SetPalette(Palette.Underground); break;
                case WorldBackground.Castle: SetPalette(Palette.Castle); break;
                default: SetPalette(Palette.Overworld); break;
            }

            SetAi(new PiranhaPlantAi(this));
        }
    }
}
