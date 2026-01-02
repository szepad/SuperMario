using System.Diagnostics.CodeAnalysis;

namespace Mario2
{
    public class World
    {
        public int Cols { get; private set; } = 100;
        public const int Rows = 14;
        public WorldBackground Background { get; private set; }
        public ParallaxBackground Parallax { get; private set; }

        public Player Player { get; }
        public Tile[,] Tiles { get; private set; }

        public World()
        {
            Player = new Player(this);
            Player.Position.X = 50;

            Tiles = new Tile[Rows, Cols];
            Clear(Tiles);

            SetBackground(WorldBackground.Sky);
        }

        [MemberNotNull(nameof(Parallax))]
        public void SetBackground(WorldBackground background)
        {
            Background = background;
            Parallax = WorldBackgroundExtension.GetParallax(background);
        }

        public void Clear() => Clear(Tiles);
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

            tile = Tiles[row, col];
            return true;
        }

        public void SetTile(int col, int row, Tile tile)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows)
                return;

            Tiles[row, col] = tile;
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
                    newMtx[row, col] = Tiles[row, col];
                }
            }

            Cols = newCols;
            Tiles = newMtx;
        }

        public void Update()
        {
            Player.Update();
        }

        public void UpdateParallax(Camera2D camera)
        {
            Parallax.Update(camera);
        }

        public void Draw(bool editor = false)
        {
            if (!editor)
            {
                Parallax.Draw();
            }

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    Tile tile = Tiles[y, x];
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

            Player.Draw();
        }
    }
}
