namespace Mario2
{
    public class World
    {
        public int TileCountX { get; private set; } = 16;
        public int TileCountY { get; private set; } = 14;
        private Camera2D _camera = new(Vector2.Zero, Vector2.Zero, 0f, 1f);

        private readonly Player _player;

        private const string Map =
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            ".........1......" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "................" +
            "..011111111112..";

        public Tile[,] Tiles => _tiles;
        private readonly Tile[,] _tiles;

        public World()
        {
            _player = new Player(this);
            _player.Position.X = 50;

            _tiles = new Tile[TileCountY, TileCountX];
            for (int y = 0; y < TileCountY; y++)
            {
                for (int x = 0; x < TileCountX; x++)
                {
                    if (int.TryParse(Map[y * TileCountX + x].ToString(), out int id))
                    {
                        _tiles[y, x] = new SimpleTile(id, true);
                    }
                    else
                    {
                        _tiles[y, x] = SimpleTile.Air;
                    }
                }
            }
        }

        public void Update()
        {
            _player.Update();

            _camera.Offset = new Vector2(Game.Width, Game.Height) / 2f;
            _camera.Target = _player.Rect.Center;
        }

        public void Draw()
        {
            Raylib.BeginMode2D(_camera);

            for (int y = 0; y < TileCountY; y++)
            {
                for (int x = 0; x < TileCountX; x++)
                {
                    Tile tile = _tiles[y, x];
                    if (tile == SimpleTile.Air) 
                        continue;

                    tile.Draw(x, y);
                }
            }

            _player.Draw();

            Raylib.EndMode2D();
        }
    }
}
