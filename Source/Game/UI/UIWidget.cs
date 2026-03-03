namespace Mario2
{
    public abstract record UIWidget(string Label, Vector2 Position)
    {
        public const float DefaultGap = 6f * Game.TextureScale;

        public string Label { get; set; } = Label;
        public Vector2 Position = Position;

        protected const float MarkerSpacing = 7f * Game.TextureScale;
        protected Vector2 MarkerOffset => new(Text.GetWidth(Label) / 2f + MarkerSpacing, 0f);

        public abstract void Update();
        public abstract void DrawCursor();

        public virtual void Draw()
        {
            Text.Draw(Label, Position, Pivot.Center);
        }
    }
}
