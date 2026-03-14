namespace Mario2
{
    public sealed class UIPressButton : UIWidget
    {
        public event Action<UIPressButton>? OnSelect;

        public UIPressButton(string label, Action<UIPressButton>? onSelect) 
            : base(label, Vector2.Zero)
        {
            OnSelect += onSelect;
        }

        public override void Update()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                OnSelect?.Invoke(this);
            }
        }

        public override void DrawCursor()
        {
            Raylib.DrawTextureEx(Assets.GetTex("cursor"),
                Position - MarkerOffset - new Vector2(4f).Scale(),
                0f, Game.TextureScale, Color.White);
        }
    }
}
