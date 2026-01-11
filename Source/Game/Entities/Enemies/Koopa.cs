namespace Mario2
{
    public class Koopa : ShellEnemy
    {
        protected override EnemyAi NormalAi { get; }

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
    }
}
