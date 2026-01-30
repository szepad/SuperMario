namespace Mario2.Editor
{
    public abstract record TileObject(Texture2D Tileset, int PreviewCol, int PreviewRow)
        : ISelectableObject<Tile>
    {
        public TileObject(int previewCol, int previewRow)
            : this(Assets.GetTex("tileset"), previewCol, previewRow) 
        { }

        public abstract Tile OnPlace(World world, int col, int row);
    }
}
