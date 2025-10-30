namespace Mario2
{
    public class GameScene : Scene
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
                Game.Instance.LoadScene(new TitleScene());
            }
        }

        public override void Draw()
        {
            Raylib.ClearBackground(Color.SkyBlue);
            Text.Draw("Game", Game.Width / 2, 5, 10, Color.White, Pivot.Top);
        }
    }
}
