namespace Mario2.Editor
{
    public sealed record InstanceTile<T>(int PreviewCol, int PreviewRow)
        : TileObject(PreviewCol, PreviewRow) where T : Tile, new()
    {
        public override Tile OnPlace(World world, int col, int row)
        {
            return new T();
        }
    }
}
