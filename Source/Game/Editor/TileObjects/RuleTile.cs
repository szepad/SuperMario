namespace Mario2.Editor
{
    public sealed record RuleTile(bool IsCollidable, int PreviewCol, int PreviewRow, 
        Dictionary<byte, TileObject> Rules, bool ConnectToSolid = false)
        : TileObject(PreviewCol, PreviewRow)
    {
        private bool CheckTileAt(World world, int col, int row)
        {
            if (world.GetTile(col, row, out Tile tile))
            {
                if (tile.Rule == this)
                    return true;

                if (ConnectToSolid && tile.IsCollidable)
                    return true;

                return false;
            }

            // Always connect to edge of map
            return true;
        }

        public override Tile OnPlace(World world, int col, int row)
        {
            byte rule = 0;

            if (CheckTileAt(world, col, row - 1)) rule |= 0b1000;
            if (CheckTileAt(world, col + 1, row)) rule |= 0b0100;
            if (CheckTileAt(world, col, row + 1)) rule |= 0b0010;
            if (CheckTileAt(world, col - 1, row)) rule |= 0b0001;

            if (Rules.Count == 0)
                return SimpleTile.Air;

            if (Rules.TryGetValue(rule, out TileObject? obj))
            {
                Tile tile = obj.OnPlace(world, col, row);
                tile.Rule = this;

                return tile;
            }

            return SimpleTile.Air;
        }
    }
}
