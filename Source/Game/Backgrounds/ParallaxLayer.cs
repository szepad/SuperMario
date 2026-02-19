namespace Mario2
{
    public class ParallaxLayer(Texture2D tex, Vector2 offset, float speed = 1f, int repeatCount = 1, float opacity = 1f)
    {
        public Texture2D Tex { get; } = tex;
        public float Speed { get; } = speed;
        public int RepeatCount { get; } = repeatCount;
        public float Opacity { get; } = opacity;

        public Vector2 Offset { get; set; } = offset;
        public Vector2 Position { get; set; }
    }
}
