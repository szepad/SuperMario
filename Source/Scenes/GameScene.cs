namespace Mario2
{
    public class GameScene : Scene
    {
        private World _world = null!;
        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);

        protected override void OnLoad()
        {
            _camera.Offset.X = Game.Width / 2f;
            _camera.Offset.Y = Game.Height - World.Rows * Tile.Size + Tile.Size;

            SessionManager.Reset();
            if (SessionManager.LoadNext(out _world) != SessionLoadResult.Success)
            {
                Game.Instance.LoadScene(new TitleScene());
            }
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

            const float HalfX = Game.Width / 2f;
            _camera.Target.X = Helper.Clamp(_world.Player.Position.X, HalfX, _world.Cols * Tile.Size - HalfX);
        }

        public override void Draw()
        {
            Raylib.ClearBackground(_world.BackgroundColor);
            Raylib.BeginMode2D(_camera);
            _world.Draw();
            Raylib.EndMode2D();
        }
    }
}
