namespace Mario2.Editor
{
    public class TileWindow(EditorScene editor) : SelectorWindow<Tile>(editor, "Tileset")
    {
        public override List<ISelectableObject<Tile>> Selectables { get; } = 
        [
            TileObjectDefs.Grass,
            TileObjectDefs.Block,
            TileObjectDefs.QuestionTile,
            TileObjectDefs.QuestionTile_Empty,
            TileObjectDefs.QuestionTile_Empty_Castle,
            TileObjectDefs.Brick,
            TileObjectDefs.Pipe,
            TileObjectDefs.GrassDecor,
            TileObjectDefs.Coin,
            TileObjectDefs.CastleSmall,
            TileObjectDefs.Flagpole,
            TileObjectDefs.UndergroundFloor,
            TileObjectDefs.BlockUg,
            TileObjectDefs.Tree,
            TileObjectDefs.TreeTrunk,
            TileObjectDefs.CastleFloor,
            TileObjectDefs.CastleFloorInnerLeft,
            TileObjectDefs.CastleFloorInnerRight,
            TileObjectDefs.CastleStair1,
            TileObjectDefs.CastleStair2,
            TileObjectDefs.CastleStair3,
            TileObjectDefs.CastleStair4,
            TileObjectDefs.CastleCeiling,
            TileObjectDefs.CastleCeilingSingle,
            TileObjectDefs.CastleBridge,
            TileObjectDefs.Chain,
            TileObjectDefs.Lava,
        ];

        public override void Update() { }
    }

    public static class TileObjectDefs
    {
        public static readonly BasicTile Block = new(16, true, 0, 2);
        public static readonly BasicTile BlockUg = new(20, true, 4, 2);
        public static readonly BasicTile QuestionTile_Empty = new(18, true, 2, 2);
        public static readonly BasicTile QuestionTile_Empty_Castle = new(31, true, 7, 3);
        public static readonly BasicTile CastleCeilingSingle = new(126, true, 6, 15);
        public static readonly BasicTile CastleBridge = new(118, true, 6, 14);
        public static readonly BasicTile Chain = new(110, false, 6, 13);
        public static readonly BasicTile CastleFloorInnerLeft = new(120, true, 0, 15);
        public static readonly BasicTile CastleFloorInnerRight = new(112, true, 0, 14);
        public static readonly BasicTile CastleStair1 = new(108, true, 4, 13);
        public static readonly BasicTile CastleStair2 = new(109, true, 5, 13);
        public static readonly BasicTile CastleStair3 = new(116, true, 4, 14);
        public static readonly BasicTile CastleStair4 = new(117, true, 5, 14);

        public static readonly InstanceTile<QuestionTile> QuestionTile = new(1, 2);
        public static readonly InstanceTile<BrickTile> Brick = new(3, 2);
        public static readonly InstanceTile<CoinTile> Coin = new(7, 2);

        public static readonly PrefabTile Lava = new(false, 0, 13,
        [
            new PrefabActionTile(0, 0, new LavaTile()),
            new PrefabActionRecurse(0, 1, new LavaTileBottom(), 0, 1)
        ]);

        public static readonly PrefabTile Pipe = new(true, 0, 3,
        [
            new PrefabActionTile(0, 0, new SimpleTile(24, true)),
            new PrefabActionTile(1, 0, new SimpleTile(25, true)),
            new PrefabActionRecurse(0, 1, new SimpleTile(32, true), 0, 1),
            new PrefabActionRecurse(1, 1, new SimpleTile(33, true), 0, 1),
        ]);

        public static readonly PrefabTile Flagpole = new(true, 6, 3,
        [
            new PrefabActionTile(0, 0, new SimpleTile(29, false)),
            new PrefabActionRecurse(0, 1, new SimpleTile(37, false), 0, 1),
            new PrefabActionEntity(6f, 16f, typeof(Flagpole))
        ]);

        public static readonly PrefabTile CastleSmall = new(false, 1, 6,
        [
            new PrefabActionTile(1, 0, new SimpleTile(49, false), OnlyOnAir: false),
            new PrefabActionTile(2, 0, new SimpleTile(50, false), OnlyOnAir: false),
            new PrefabActionTile(3, 0, new SimpleTile(51, false), OnlyOnAir: false),
            new PrefabActionTile(4, 0, new SimpleTile(52, false), OnlyOnAir: false),

            new PrefabActionTile(1, 1, new SimpleTile(49 + 8, false), OnlyOnAir: false),
            new PrefabActionTile(2, 1, new SimpleTile(50 + 8, false), OnlyOnAir: false),
            new PrefabActionTile(3, 1, new SimpleTile(51 + 8, false), OnlyOnAir: false),
            new PrefabActionTile(4, 1, new SimpleTile(52 + 8, false), OnlyOnAir: false),

            new PrefabActionTile(0, 2, new SimpleTile(48 + 16, false), OnlyOnAir: false),
            new PrefabActionTile(1, 2, new SimpleTile(49 + 16, false), OnlyOnAir: false),
            new PrefabActionTile(2, 2, new SimpleTile(50 + 16, false), OnlyOnAir: false),
            new PrefabActionTile(3, 2, new SimpleTile(51 + 16, false), OnlyOnAir: false),
            new PrefabActionTile(4, 2, new SimpleTile(52 + 16, false), OnlyOnAir: false),
            new PrefabActionTile(5, 2, new SimpleTile(53 + 16, false), OnlyOnAir: false),

            new PrefabActionTile(0, 3, new SimpleTile(48 + 24, false), OnlyOnAir: false),
            new PrefabActionTile(1, 3, new SimpleTile(49 + 24, false), OnlyOnAir: false),
            new PrefabActionTile(2, 3, new SimpleTile(50 + 24, false), OnlyOnAir: false),
            new PrefabActionTile(3, 3, new SimpleTile(51 + 24, false), OnlyOnAir: false),
            new PrefabActionTile(4, 3, new SimpleTile(52 + 24, false), OnlyOnAir: false),
            new PrefabActionTile(5, 3, new SimpleTile(53 + 24, false), OnlyOnAir: false),

            new PrefabActionTile(0, 4, new SimpleTile(48 + 32, false), OnlyOnAir: false),
            new PrefabActionTile(1, 4, new SimpleTile(49 + 32, false), OnlyOnAir: false),
            new PrefabActionTile(2, 4, new SimpleTile(50 + 32, false), OnlyOnAir: false),
            new PrefabActionTile(3, 4, new SimpleTile(51 + 32, false), OnlyOnAir: false),
            new PrefabActionTile(4, 4, new SimpleTile(52 + 32, false), OnlyOnAir: false),
            new PrefabActionTile(5, 4, new SimpleTile(53 + 32, false), OnlyOnAir: false),

            new PrefabActionEntity(40, 64, typeof(CastleEntrance))
        ]);

        public static readonly PrefabTile CastleCeiling = new(true, 4, 15,
        [
            new PrefabActionTile(0, 0, new SimpleTile(124, true)),
            new PrefabActionTile(1, 0, new SimpleTile(125, true)),
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

        public static readonly RuleTile UndergroundFloor = new(true, 7, 0, new()
        {
            [0b_0000] = new BasicTile(7, true, 0, 0),
            [0b_0100] = new BasicTile(4, true, 0, 0),
            [0b_0101] = new BasicTile(5, true, 0, 0),
            [0b_0001] = new BasicTile(6, true, 0, 0),

            [0b_0010] = new BasicTile(7, true, 0, 0),
            [0b_0110] = new BasicTile(4, true, 0, 0),
            [0b_0111] = new BasicTile(5, true, 0, 0),
            [0b_0011] = new BasicTile(6, true, 0, 0),

            [0b_1000] = new BasicTile(15, true, 0, 0),
            [0b_1100] = new BasicTile(12, true, 0, 0),
            [0b_1101] = new BasicTile(13, true, 0, 0),
            [0b_1001] = new BasicTile(14, true, 0, 0),

            [0b_1010] = new BasicTile(15, true, 0, 0),
            [0b_1110] = new BasicTile(12, true, 0, 0),
            [0b_1111] = new BasicTile(13, true, 0, 0),
            [0b_1011] = new BasicTile(14, true, 0, 0),
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
            [0b_1010] = new InstanceTile<GrassDecorTileMiddle>(0, 0),

            [0b_0100] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_1100] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_1110] = new InstanceTile<GrassDecorTileLeft>(0, 0),
            [0b_0110] = new InstanceTile<GrassDecorTileLeft>(0, 0),

            [0b_0001] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_1001] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_1011] = new InstanceTile<GrassDecorTileRight>(0, 0),
            [0b_0011] = new InstanceTile<GrassDecorTileRight>(0, 0),
        }, ConnectToSolid: true);

        public static readonly RuleTile Tree = new(true, 2, 5, new()
        {
            [0b_0000] = new BasicTile(42, true, 0, 0),
            [0b_1000] = new BasicTile(42, true, 0, 0),
            [0b_1010] = new BasicTile(42, true, 0, 0),
            [0b_0010] = new BasicTile(42, true, 0, 0),
            
            [0b_0101] = new BasicTile(42, true, 0, 0),
            [0b_1101] = new BasicTile(42, true, 0, 0),
            [0b_1111] = new BasicTile(42, true, 0, 0),
            [0b_0111] = new BasicTile(42, true, 0, 0),

            [0b_0100] = new BasicTile(41, true, 0, 0),
            [0b_1100] = new BasicTile(41, true, 0, 0),
            [0b_1110] = new BasicTile(41, true, 0, 0),
            [0b_0110] = new BasicTile(41, true, 0, 0),

            [0b_0001] = new BasicTile(43, true, 0, 0),
            [0b_1001] = new BasicTile(43, true, 0, 0),
            [0b_1011] = new BasicTile(43, true, 0, 0),
            [0b_0011] = new BasicTile(43, true, 0, 0),
        });

        public static readonly RuleTile TreeTrunk = new(false, 7, 5, new()
        {
            [0b_0000] = new BasicTile(47, false, 0, 0),
            [0b_1000] = new BasicTile(47, false, 0, 0),
            [0b_1010] = new BasicTile(47, false, 0, 0),
            [0b_0010] = new BasicTile(47, false, 0, 0),

            [0b_0100] = new BasicTile(44, false, 0, 0),
            [0b_1100] = new BasicTile(44, false, 0, 0),
            [0b_1110] = new BasicTile(44, false, 0, 0),
            [0b_0110] = new BasicTile(44, false, 0, 0),

            [0b_0001] = new BasicTile(46, false, 0, 0),
            [0b_1001] = new BasicTile(46, false, 0, 0),
            [0b_1011] = new BasicTile(46, false, 0, 0),
            [0b_0011] = new BasicTile(46, false, 0, 0),

            [0b_0101] = new BasicTile(45, false, 0, 0),
            [0b_1101] = new BasicTile(45, false, 0, 0),
            [0b_1111] = new BasicTile(45, false, 0, 0),
            [0b_0111] = new BasicTile(45, false, 0, 0),
        });

        public static readonly RuleTile CastleFloor = new(true, 2, 13, new()
        {
            [0b_0000] = new BasicTile(106, true, 0, 0),
            [0b_0101] = new BasicTile(106, true, 0, 0),
            [0b_0111] = new BasicTile(106, true, 0, 0),
            [0b_0100] = new BasicTile(105, true, 0, 0),
            [0b_0110] = new BasicTile(105, true, 0, 0),
            [0b_0001] = new BasicTile(107, true, 0, 0),
            [0b_0011] = new BasicTile(107, true, 0, 0),
            [0b_1000] = new BasicTile(114, true, 0, 0),
            [0b_1010] = new BasicTile(114, true, 0, 0),
            [0b_1111] = new BasicTile(114, true, 0, 0),
            [0b_1101] = new BasicTile(114, true, 0, 0),
            [0b_1110] = new BasicTile(113, true, 0, 0),
            [0b_1011] = new BasicTile(115, true, 0, 0),

            [0b_0010] = new BasicTile(106, true, 0, 0),
            [0b_1001] = new BasicTile(115, true, 0, 0),
            [0b_1100] = new BasicTile(113, true, 0, 0),
        });
    }
}
