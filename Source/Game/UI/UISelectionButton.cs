namespace Mario2
{
    public sealed class UISelectionButton<T> : UIWidget
    {
        public event Action<UISelectionButton<T>, T>? OnValueChanged;

        public T[] Selection { get; private set; }
        public int CurrIndex { get; private set; }
        public T Curr => Selection[CurrIndex];

        public UISelectionButton(string label, T[] selection,
            Action<UISelectionButton<T>, T> onValueChanged, int currIndex = 0) 
            : base(label, Vector2.Zero)
        {
            Selection = selection;
            CurrIndex = currIndex;
            OnValueChanged += onValueChanged;
        }

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
            Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                Position - MarkerOffset - new Vector2(4f).Scale(),
                0f, Game.TextureScale, Color.White);

            Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                Position + MarkerOffset - new Vector2(4f).Scale(),
                0f, Game.TextureScale, Color.White);
        }
    }
}
