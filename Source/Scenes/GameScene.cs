namespace Mario2
{
    public class GameScene(string path = "") : Scene
    {
        private World _world = null!;
        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);

        private readonly bool _insideEditor = path != string.Empty;
        private readonly string _editorFilePath = path;

        protected override void OnLoad()
        {
            _camera.Offset.X = Game.Width / 2f;
            _camera.Offset.Y = Game.Height - World.Rows * Tile.Size + Tile.Size;

            if (_insideEditor)
            {
                World? loaded = WorldLoader.Deserialize(_editorFilePath, out _);
                if (loaded is null)
                {
                    if (ParentScene is null)
                    {
                        Game.Instance.LoadScene(new TitleScene());
                    }
                    else
                    {
                        UnloadSubScene();
                    }
                }
                else
                {
                    _world = loaded;
                }
            }
            else
            {
                SessionManager.Reset();
                if (SessionManager.LoadNext(out _world) != SessionLoadResult.Success)
                {
                    Game.Instance.LoadScene(new TitleScene());
                }
            }
        }

        protected override void OnUnload()
        {

        }

        protected override void OnUpdate()
        {
            _world.Update();

            const float HalfX = Game.Width / 2f;
            _camera.Target.X = Helper.Clamp(_world.Player.Position.X, HalfX, _world.Cols * Tile.Size - HalfX);
            _world.UpdateParallax(_camera);

            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                if (ParentScene is null) Game.Instance.LoadScene(new TitleScene());
                else ParentScene.UnloadSubScene();
            }
        }

        protected override void OnDraw()
        {
            Raylib.ClearBackground(_world.Parallax.ClearColor);
            Raylib.BeginMode2D(_camera);
            _world.Draw();
            Raylib.EndMode2D();
        }
    }
}
