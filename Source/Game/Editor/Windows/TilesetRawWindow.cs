
namespace Mario2.Editor
{
    public class TilesetRawWindow : SelectorWindow<Tile>
    {
        public override List<ISelectableObject<Tile>> Selectables { get; } = [];

        private static readonly int[] _notCollidableIds =
        [
            29, 37, 40,
            49, 50, 51, 52,
            57, 58, 59, 60,
            64, 65, 66, 67, 68, 69,
            72, 73, 74, 75, 76, 77,
            80, 81, 82, 83, 84, 85,
            88, 89, 90, 91, 92,
            96, 97, 98, 99, 100,
        ];

        public TilesetRawWindow(EditorScene editor)
            : base(editor, "Raw")
        {
            const int TileSizePx = 16;
            Texture2D tex = Assets.GetTex("tileset");
            int count = tex.Width / TileSizePx * (tex.Height / TileSizePx);

            for (int i = 0; i < count; i++)
            {
                Selectables.Add(new TilesetRawObject(tex, i, !_notCollidableIds.Contains(i)));
            }
        }

        public override void Update() { }
    }

    public record TilesetRawObject(Texture2D Tileset, int Id, bool IsCollidable) : ISelectableObject<Tile>
    {
        public int PreviewCol { get; } = Id % 8;
        public int PreviewRow { get; } = Id / 8;

        public Tile OnPlace(World world, int col, int row)
        {
            return new SimpleTile(Id, IsCollidable);
        }
    }
}
