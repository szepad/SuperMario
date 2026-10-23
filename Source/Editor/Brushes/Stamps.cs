using System.Collections.Immutable;

namespace Mario2.Editor
{
    /// <summary>Built-in structures.</summary>
    public static class Stamps
    {
        public static readonly Stamp Pipe = new("Pipe",
        [
            new StampCell(0, 0, new SimpleSpec(24, true), OnlyOnEmpty: true),
            new StampCell(1, 0, new SimpleSpec(25, true), OnlyOnEmpty: true),
            new StampCell(0, 1, new SimpleSpec(32, true), OnlyOnEmpty: true, FillDown: true),
            new StampCell(1, 1, new SimpleSpec(33, true), OnlyOnEmpty: true, FillDown: true),
        ]);

        public static readonly Stamp Flagpole = new("Flagpole",
        [
            new StampCell(0, 0, new SimpleSpec(29, false), OnlyOnEmpty: true),
            new StampCell(0, 1, new SimpleSpec(37, false), OnlyOnEmpty: true, FillDown: true),
        ],
        [
            new StampEntity(8, 160, "flagpole", ImmutableSortedDictionary<string, string>.Empty),
        ]);

        public static readonly Stamp Lava = new("Lava",
        [
            new StampCell(0, 0, new AnimSpec(AnimTileKind.Lava), OnlyOnEmpty: true),
            new StampCell(0, 1, new AnimSpec(AnimTileKind.LavaBottom), OnlyOnEmpty: true, FillDown: true),
        ]);

        public static readonly Stamp CastleCeiling = new("Castle Ceiling",
        [
            new StampCell(0, 0, new SimpleSpec(124, true), OnlyOnEmpty: true),
            new StampCell(1, 0, new SimpleSpec(125, true), OnlyOnEmpty: true),
        ]);

        public static readonly Stamp SmallCastle = new("Small Castle", SmallCastleCells(),
        [
            new StampEntity(48, 80, "castle_entrance", ImmutableSortedDictionary<string, string>.Empty),
        ]);

        private static List<StampCell> SmallCastleCells()
        {
            // The castle occupies a 6x5 block of the tileset starting at id 48; its top two rows are narrower
            List<StampCell> cells = [];
            for (int dy = 0; dy < 5; dy++)
            {
                (int from, int to) = dy < 2 ? (1, 4) : (0, 5);
                for (int dx = from; dx <= to; dx++)
                {
                    cells.Add(new StampCell(dx, dy, new SimpleSpec(48 + dy * TilesetInfo.Columns + dx, false)));
                }
            }

            return cells;
        }
    }
}
