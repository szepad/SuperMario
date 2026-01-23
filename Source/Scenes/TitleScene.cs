namespace Mario2
{
    public class TitleScene : Scene
    {
        protected override void OnLoad()
        {
            SoundManager.PlayMusic(MusicName.Title);
            Raylib.HideCursor();
            SessionManager.Reset();
        }

        protected override void OnUnload()
        {

        }

        protected override void OnUpdate()
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

        protected override void OnDraw()
        {
            Raylib.ClearBackground(Color.Black);
            Text.Draw("Title", Game.Width / 2, 5, Pivot.Top);
        }
    }
}
