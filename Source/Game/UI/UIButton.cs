namespace Mario2
{
    public record UIButton(Vector2 Position, string Label, Action<UIButton>? OnSelect,
        Pivot Pivot = Pivot.Center)
    {
        public Vector2 Position = Position;

        public UIButton(string label, Action<UIButton> onSelect, Pivot pivot = Pivot.Center)
            : this(Vector2.Zero, label, onSelect, pivot)
        { }

        public UIButton(string label, Pivot pivot = Pivot.Center)
            : this(Vector2.Zero, label, null, pivot)
        { }
    }
}
