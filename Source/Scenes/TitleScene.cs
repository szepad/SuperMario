namespace Mario2
{
    public class TitleScene : Scene
    {
        protected override void OnLoad()
        {

        }

        protected override void OnUnload()
        {

        }

        public override void Update()
        {
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                Game.Instance.Exit();
            }

            if (Input.IsKeyPressed(KeyboardKey.Enter))
            {
                Game.Instance.LoadScene(new GameScene());
            }

            if (Input.IsKeyPressed(KeyboardKey.E))
            {
                Game.Instance.LoadScene(new EditorScene());
            }
        }

        public override void Draw()
        {
            Raylib.ClearBackground(Color.Black);
            Text.Draw("Title", Game.Width / 2, 5, 10, Color.White, Pivot.Top);
        }
    }
}
