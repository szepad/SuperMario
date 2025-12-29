namespace Mario2
{
    public class GameScene : Scene
    {
        private readonly World _world = new();

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

            _world.Update();
        }

        public override void Draw()
        {
            Raylib.ClearBackground(_world.BackgroundColor);
            _world.Draw();
        }
    }
}
