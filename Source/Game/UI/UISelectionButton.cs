namespace Mario2
{
    public sealed record UISelectionButton<T>(string Label, T[] Selection,
        Action<UISelectionButton<T>, T> OnValueChanged, int CurrIndex = 0)
        : UIWidget(Label, Vector2.Zero)
    {
        public int CurrIndex { get; private set; } = CurrIndex;
        public T Curr => Selection[CurrIndex];

        private T Next()
        {
            CurrIndex = CurrIndex == Selection.Length - 1 ? 0 : CurrIndex + 1;
            return Selection[CurrIndex];
        }

        private T Prev()
        {
            CurrIndex = CurrIndex == 0 ? Selection.Length - 1 : CurrIndex - 1;
            return Selection[CurrIndex];
        }

        public override void Update()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressedRepeat(KeyboardKey.Left))
                OnValueChanged?.Invoke(this, Prev());

            if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressedRepeat(KeyboardKey.Right))
                OnValueChanged?.Invoke(this, Next());
        }

        public override void DrawCursor()
        {
            Raylib.DrawCircleV(Position - MarkerOffset, 2f.Scale(), Color.Red);
            Raylib.DrawCircleV(Position + MarkerOffset, 2f.Scale(), Color.Red);
        }
    }
}
