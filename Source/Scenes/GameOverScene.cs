namespace Mario2
{
    public class GameOverScene : Scene
    {
        protected override void OnLoad()
        {
            SoundManager.PlayMusic(MusicName.GameOver, isLooped: false);
            Raylib.HideCursor();
        }

        protected override void OnUnload()
        {

        }

        protected override void OnUpdate()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            {
                Game.Instance.LoadScene(new TitleScene());
            }
        }

        protected override void OnDraw()
        {
            Raylib.ClearBackground(Color.Black);
            Text.Draw("Game Over", Game.Width / 2, Game.Height * 0.45f, Pivot.Center);
        }
    }
}
