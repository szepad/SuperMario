namespace Mario2
{
    public sealed record UIPressButton(string Label, Action<UIPressButton>? OnSelect)
        : UIWidget(Label, Vector2.Zero)
    {
        public override void Update()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                OnSelect?.Invoke(this);
            }
        }

        public override void DrawCursor()
        {
            Raylib.DrawCircleV(Position - MarkerOffset, 2f.Scale(), Color.Red);

            //Raylib.DrawTextureEx(Assets.GetTex("cursor"),
            //    button.Position - offset - new Vector2(4f).Scale(),
            //    0f, Game.TextureScale, Color.White);
        }
    }
}
