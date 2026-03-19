namespace Mario2
{
    public class Koopa : ShellEnemy
    {
        public Palette Palette { get; private set; } = Palette.Overworld; 
        
        private const float SpriteOffsetYNormal = 8f * Game.TextureScale;
        private const float SpriteOffsetYFlip = -2f * Game.TextureScale;

        public Koopa(World world) : base(world)
        {
            Size = new Vector2(Tile.Size);
            SpriteOffset.Y = 8f * Game.TextureScale;
            HitboxSize = new Vector2(14f).Scale();
            HitboxOffset = new Vector2(1f, 2f).Scale();

            Sprite.SetSheet("koopa");
            Sprite.Add(WalkAnim, new Animation(2, 16, 24, 0, 0.1f));
            Sprite.Add(ShellAnim, new Animation(1, 16, 24, 48, 1f));
            Sprite.Add(WakeUpAnim, new Animation(2, 16, 24, 72, 0.1f));
            Sprite.Add(ShellSpinAnim, new Animation(4, 16, 24, 96, 0.1f));

            NormalAi = new GoombaAi(this, Goomba.WalkSpeed);
        }

        public virtual void SetPalette(Palette palette)
        {
            Palette = palette;
            switch (palette)
            {
                case Palette.Overworld: Sprite.SetSheet("koopa"); break;
                case Palette.Underground: Sprite.SetSheet("koopa_ug"); break;
                case Palette.Castle: Sprite.SetSheet("koopa_castle"); break;
            }
        }

        public override void Update()
        {
            FlipTowardsVelocity();
            base.Update();
        }

        public override void Draw()
        {
            SpriteOffset.Y = Sprite.FlipY ? SpriteOffsetYFlip : SpriteOffsetYNormal;
            base.Draw();
        }

        public override string SaveCustomData()
        {
            return Palette.ToString();
        }

        public override void LoadCustomData(string data)
        {
            if (Enum.TryParse(data, out Palette palette))
            {
                SetPalette(palette);
            }
        }
    }
}
