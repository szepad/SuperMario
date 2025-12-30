namespace Mario2
{
    public enum WorldBackground
    {
        Sky,
        Underground,
        Castle,
        Water,
        Night
    }

    public class World
    {
        public int Cols { get; private set; } = 100;
        public const int Rows = 14;
        public WorldBackground Background { get; set; } = WorldBackground.Sky;

        private readonly Player _player;

        public Tile[,] Tiles => _tiles;
        private Tile[,] _tiles;

        public World()
        {
            _player = new Player(this);
            _player.Position.X = 50;

            _tiles = new Tile[Rows, Cols];
            Clear(_tiles);
        }

        public void Clear() => Clear(_tiles);
        public static void Clear(Tile[,] tiles)
        {
            for (int row = 0; row < tiles.GetLength(0); row++)
            {
                for (int col = 0; col < tiles.GetLength(1); col++)
                {
                    tiles[row, col] = SimpleTile.Air;
                }
            }
        }

        public bool GetTile(int col, int row, out Tile tile)
        {
            tile = SimpleTile.Air;
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return false;

            tile = _tiles[row, col];
            return true;
        }

        public void SetTile(int col, int row, Tile tile)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return;

            _tiles[row, col] = tile;
        }

        public void SetCols(int newCols)
        {
            if (newCols < 0)
                return;

            Tile[,] newMtx = new Tile[Rows, newCols];
            Clear(newMtx);

            for (int col = 0; col < newCols; col++)
            {
                if (col >= Cols)
                    break;

                for (int row = 0; row < Rows; row++)
                {
                    newMtx[row, col] = _tiles[row, col];
                }
            }

            Cols = newCols;
            _tiles = newMtx;
        }

        public void Update()
        {
            _player.Update();
        }

        public void Draw(bool editor = false)
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Tile tile = _tiles[y, x];
                    if (tile == SimpleTile.Air) 
                        continue;

                    if (editor)
                    {
                        tile.Draw(x, y, tile.IsCollidable ? 1f : 0.5f);
                    }
                    else
                    {
                        tile.Draw(x, y);
                    }
                }
            }

            _player.Draw();
        }

        public Color BackgroundColor => Background switch
        {
            WorldBackground.Sky => new Color(92, 148, 252, 255),
            _ => Color.Black
        };
    }
}
