namespace Mario2
{
    public abstract class UIWidget(string label, Vector2 position, bool greyedOut = false)
    {
        public const float DefaultGap = 6f * Game.TextureScale;

        public string Label { get; set; } = label;
        public bool IsGreyedOut { get; set; } = greyedOut;
        public Vector2 Position = position;

        protected const float MarkerSpacing = 7f * Game.TextureScale;
        protected Vector2 MarkerOffset => new(Text.GetWidth(Label) / 2f + MarkerSpacing, 0f);

        public abstract void Update();
        public abstract void DrawCursor();

        public virtual void Draw()
        {
            Text.Draw(Label, Position, Pivot.Center, IsGreyedOut);
        }
    }
}
