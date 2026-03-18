namespace Mario2.Editor
{
    public interface ISelectableObject<T>
    {
        public Texture2D Tileset { get; }
        public int PreviewCol { get; }
        public int PreviewRow { get; }

        public int Index => PreviewRow * Tileset.Width / SelectorWindow<T>.TileSizePx + PreviewCol;

        public abstract T OnPlace(World world, int col, int row);
    }
}
