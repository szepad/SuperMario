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

        public List<ParallaxAnimatedObject> AnimatedObjects { get; init; } = [];

        public Vector2 Offset { get; set; } = offset;
        public Vector2 Position { get; set; }

        public void UpdateObjects()
        {
            foreach (ParallaxAnimatedObject obj in AnimatedObjects)
            {
                obj.Sprite.Update();
            }
        }
    }
}
