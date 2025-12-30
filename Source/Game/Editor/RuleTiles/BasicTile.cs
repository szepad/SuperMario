namespace Mario2.Editor
{
    public sealed record BasicTile(int Id, bool IsCollidable, int PreviewCol, int PreviewRow)
        : TileObject(PreviewCol, PreviewRow)
    {
        public static readonly BasicTile Block = new(16, true, 0, 2);
        public static readonly BasicTile QuestionBlock = new(17, true, 1, 2);
        public static readonly BasicTile QuestionBlock_Empty = new(18, true, 2, 2);
        public static readonly BasicTile Brick = new(19, true, 3, 2);

        public override Tile OnPlace(World world, int col, int row)
        {
            return new SimpleTile(Id, IsCollidable);
        }
    }
}
