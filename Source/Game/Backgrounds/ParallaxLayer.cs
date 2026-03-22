using System.Reflection.Emit;

namespace Mario2
{
    public record ParallaxAnimatedObject(Sprite Sprite, List<Vector2> RelativePositions);

    public class ParallaxLayer(Texture2D tex, Vector2 offset, float speed = 1f,
        int repeatCount = 1, float opacity = 1f)
    {
        public Texture2D Tex { get; } = tex;
        public float Speed { get; } = speed;
        public int RepeatCount { get; } = repeatCount;
        public float Opacity { get; } = opacity;

        public PaletteShader? Palette { get; init; }
        public bool CyclePaletteRows { get; set; } = true;
        public float PaletteCycleSpeed { get; set; } = 0.1f;

        public List<ParallaxAnimatedObject> AnimatedObjects { get; init; } = [];

        public Vector2 Offset { get; set; } = offset;
        public Vector2 Position { get; set; }

        private float _paletteCycleTimer;

        public virtual void Update()
        {
            foreach (ParallaxAnimatedObject obj in AnimatedObjects)
            {
                obj.Sprite.Update();
            }

            if (Palette is not null && CyclePaletteRows)
            {
                _paletteCycleTimer += Time.DeltaTime;
                if (_paletteCycleTimer >= PaletteCycleSpeed)
                {
                    _paletteCycleTimer = 0f;
                    Palette.CurrentRow++;
                    if (Palette.CurrentRow >= Palette.RowCount)
                    {
                        Palette.CurrentRow = 0;
                    }
                }
            }
        }

        public virtual void Draw()
        {
            Palette?.Begin();

            for (int i = 0; i < RepeatCount; i++)
            {
                Vector2 pos = Position - Offset;
                float repeatOffset = Tex.Width * Game.TextureScale * i;
                pos.X += repeatOffset;

                DrawBackgroundTexture(pos, Color.White.Alpha(Opacity));
                DrawObjects(pos);
            }

            Palette?.End();
        }

        protected virtual void DrawBackgroundTexture(Vector2 pos, Color color)
        {
            Raylib.DrawTextureEx(Tex, pos, 0f, Game.TextureScale, color);
        }

        protected virtual void DrawObjects(Vector2 pos)
        {
            foreach (ParallaxAnimatedObject obj in AnimatedObjects)
            {
                foreach (Vector2 relPos in obj.RelativePositions)
                {
                    obj.Sprite.Draw(pos + relPos.Scale(), Opacity);
                }
            }
        }
    }
}
