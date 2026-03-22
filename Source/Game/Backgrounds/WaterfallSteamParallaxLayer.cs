namespace Mario2
{
    public sealed class WaterfallSteamParallaxLayer : ParallaxLayer
    {
        private int _animFrame;
        private readonly int _frameCount;
        private const int FrameSizePx = 16;

        private const float FrameTime = 0.1f;
        private float _frameTimer;

        public WaterfallSteamParallaxLayer(Vector2 offset, float speed, int repeatCount)
            : base(Assets.GetTex("bg_steam"), offset, speed, repeatCount)
        {
            _frameCount = Tex.Height / FrameSizePx;
            Palette = new PaletteShader(Assets.GetTex("palette_waterfall"), 8);
        }

        public override void Update()
        {
            base.Update();

            _frameTimer += Time.DeltaTime;
            if (_frameTimer >= FrameTime)
            {
                _frameTimer = 0f;
                _animFrame++;
                if (_animFrame >= _frameCount)
                {
                    _animFrame = 0;
                }
            }
        }

        protected override void DrawBackgroundTexture(Vector2 pos, Color color)
        {
            Raylib.DrawTexturePro(Tex,
                new Rectangle(0, _animFrame * FrameSizePx, Tex.Width, FrameSizePx),
                new Rectangle(pos, new Vector2(Tex.Width, FrameSizePx).Scale()),
                Vector2.Zero, 0f, color);
        }
    }
}
