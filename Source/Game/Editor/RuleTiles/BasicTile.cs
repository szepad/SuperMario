namespace Mario2.Editor
{
    public sealed record BasicTile(int Id, bool IsCollidable, int PreviewCol, int PreviewRow)
        : TileObject(PreviewCol, PreviewRow)
    {
        public override Tile OnPlace(World world, int col, int row)
        {
            return new SimpleTile(Id, IsCollidable);
        }
    }
}
