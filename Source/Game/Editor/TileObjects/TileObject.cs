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

    public static class TileObjectDefs
    {
        public static readonly BasicTile Block = new(16, true, 0, 2);
        public static readonly BasicTile QuestionBlock = new(17, true, 1, 2);
        public static readonly BasicTile QuestionBlock_Empty = new(18, true, 2, 2);
        public static readonly BasicTile Brick = new(19, true, 3, 2);

        public static readonly InstanceTile<QuestionTile> QuestionTile = new(1, 2);

        public static readonly PrefabTile Pipe = new(true, 0, 3,
        [
            new PrefabAction(0, 0, new SimpleTile(24, true)),
            new PrefabAction(1, 0, new SimpleTile(25, true)),
            new PrefabActionRecurse(0, 1, new SimpleTile(32, true), 0, 1),
            new PrefabActionRecurse(1, 1, new SimpleTile(33, true), 0, 1),
        ]);

        // 0bNESW = Id
        public static readonly RuleTile Grass = new(true, 3, 0, new()
        {
            [0b_0000] = new BasicTile(3, true, 0, 0),
            [0b_0100] = new BasicTile(0, true, 0, 0),
            [0b_0101] = new BasicTile(1, true, 0, 0),
            [0b_0001] = new BasicTile(2, true, 0, 0),

            [0b_0010] = new BasicTile(3, true, 0, 0),
            [0b_0110] = new BasicTile(0, true, 0, 0),
            [0b_0111] = new BasicTile(1, true, 0, 0),
            [0b_0011] = new BasicTile(2, true, 0, 0),

            [0b_1000] = new BasicTile(11, true, 0, 0),
            [0b_1100] = new BasicTile(8, true, 0, 0),
            [0b_1101] = new BasicTile(9, true, 0, 0),
            [0b_1001] = new BasicTile(10, true, 0, 0),

            [0b_1010] = new BasicTile(11, true, 0, 0),
            [0b_1110] = new BasicTile(8, true, 0, 0),
            [0b_1111] = new BasicTile(9, true, 0, 0),
            [0b_1011] = new BasicTile(10, true, 0, 0),
        });

        public static readonly RuleTile GrassDecor = new(false, 0, 5, new()
        {
            [0b_0000] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            [0b_1111] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            [0b_1000] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            [0b_0010] = new InstanceTile<GrassDecorTileMiddle>(0, 0),

            [0b_0101] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            [0b_1101] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            [0b_0111] = new InstanceTile<GrassDecorTileMiddle>(0, 0),
            
            [0b_0100] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_1100] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_1110] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_0110] = new InstanceTile<GrassDecorTileLeft>(0, 0),

            [0b_0001] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_1001] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_1011] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_0011] = new InstanceTile<GrassDecorTileRight>(0, 0),
        }, ConnectToSolid: true);
    }
}
