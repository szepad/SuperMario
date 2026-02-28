namespace Mario2
{
    public class TitleScene : Scene
    {
        private World? _world;

        private readonly UIButtonHandler _buttons = new();

        protected override void OnLoad()
        {
            SoundManager.PlayMusic(MusicName.Title);
            Raylib.HideCursor();
            SessionManager.Reset();

            _world = WorldLoader.Deserialize("levels/01.lvl", out _);

            _buttons.Add(new UIButton("Play", Button_Play));
            _buttons.Add(new UIButton("Editor", Button_Editor));
            _buttons.Add(new UIButton("Settings", _ => { }));
            _buttons.Add(new UIButton("Exit", Button_Exit));
            _buttons.PlaceVertically(new Vector2(Game.Width / 2f, Game.Height * 0.575f),
                6f.Scale());
        }

        private void Button_Play(UIButton self)
        {
            Game.Instance.LoadScene(new GameScene());
        }

        private void Button_Editor(UIButton self)
        {
            Game.Instance.LoadScene(new EditorScene());
        }

        private void Button_Exit(UIButton self)
        {
            Game.Instance.Exit();
        }

        protected override void OnUnload()
        {

        }

        protected override void OnUpdate()
        {
#if DEBUG
            if (Input.IsKeyPressed(KeyboardKey.Escape))
            {
                Game.Instance.Exit();
            }
#endif

            _buttons.Update();
        }

        protected override void OnDraw()
        {
            Raylib.ClearBackground(Color.Black);

            if (_world is not null)
            {
                Raylib.ClearBackground(_world.Parallax.ClearColor);
                Raylib.BeginMode2D(new Camera2D(new Vector2(0f, Tile.Size), Vector2.Zero, 0f, 1f));
                _world.DrawBackground();
                _world.Draw(default, tileCulling: false);
                Raylib.EndMode2D();
            }

            Raylib.DrawTextureEx(Assets.GetTex("title"), Vector2.Zero, 0f,
                Game.TextureScale, Color.White);

            _buttons.Draw();
        }
    }
}
