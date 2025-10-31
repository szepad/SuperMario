namespace Mario2
{
    public class GameScene : Scene
    {
        private readonly Player _player = new();

        protected override void OnLoad()
        {
            _player.Position.X = 100;
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

            _player.Update();
        }

        public override void Draw()
        {
            Raylib.ClearBackground(Color.SkyBlue);
            _player.Draw();
        }
    }
}
