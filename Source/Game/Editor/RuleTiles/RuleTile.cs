namespace Mario2.Editor
{
    public sealed record RuleTile(bool IsCollidable, int PreviewCol, int PreviewRow, Dictionary<int, int> Rules)
        : TileObject(PreviewCol, PreviewRow)
    {
        // 0bNESW = Id
        public static readonly RuleTile Grass = new(true, 3, 0, new()
        {
            [0b_0000] = 3,
            [0b_0100] = 0,
            [0b_0101] = 1,
            [0b_0001] = 2,

            [0b_0010] = 3,
            [0b_0110] = 0,
            [0b_0111] = 1,
            [0b_0011] = 2,

            [0b_1000] = 11,
            [0b_1100] = 8,
            [0b_1101] = 9,
            [0b_1001] = 10,

            [0b_1010] = 11,
            [0b_1110] = 8,
            [0b_1111] = 9,
            [0b_1011] = 10,
        });

        private bool CheckTileAt(World world, int col, int row)
        {
            if (world.GetTile(col, row, out Tile tile))
            {
                return tile.Rule == this;
            }

            // Always connect to edge of map
            return true;
        }

        public override Tile OnPlace(World world, int col, int row)
        {
            int rule = 0;

            if (CheckTileAt(world, col, row - 1)) rule |= 0b1000;
            if (CheckTileAt(world, col + 1, row)) rule |= 0b0100;
            if (CheckTileAt(world, col, row + 1)) rule |= 0b0010;
            if (CheckTileAt(world, col - 1, row)) rule |= 0b0001;

            if (Rules.Count == 0)
                return SimpleTile.Air;

            if (Rules.TryGetValue(rule, out int id))
            {
                SimpleTile tile = new SimpleTile(id, IsCollidable);
                tile.Rule = this;
                return tile;
            }

            return SimpleTile.Air;
        }
    }
}
