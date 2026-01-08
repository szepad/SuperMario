namespace Mario2.Editor
{
    public interface ISelectableObject<T>
    {
        public Texture2D Tileset { get; }
        public int PreviewCol { get; }
        public int PreviewRow { get; }

        public abstract T OnPlace(World world, int col, int row);
    }
}
