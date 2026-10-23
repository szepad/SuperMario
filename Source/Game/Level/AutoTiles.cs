namespace Mario2
{
    /// <summary>
    /// The four horizontal variants of an autotile row: the left end (neighbour only on the right),
    /// the middle (neighbours on both sides), the right end and a single, unconnected piece.
    /// </summary>
    public sealed record AutoTileStrip(TileSpec Left, TileSpec Middle, TileSpec Right, TileSpec Single)
    {
        public static AutoTileStrip Ids(int left, int middle, int right, int single, bool solid)
            => new(new SimpleSpec(left, solid), new SimpleSpec(middle, solid),
                new SimpleSpec(right, solid), new SimpleSpec(single, solid));

        public IEnumerable<TileSpec> All => [Left, Middle, Right, Single];
    }

    /// <summary>
    /// A group of tiles that pick their visual from their neighbours. Every group in the game
    /// only cares about its left/right neighbours, plus (optionally) whether something of the
    /// same group sits on top of it, which switches it to the <see cref="Body"/> strip.
    /// </summary>
    public sealed class AutoTileGroup(string key, string name, bool isSolid,
        AutoTileStrip top, AutoTileStrip? body = null, bool connectToSolid = false)
    {
        public string Key { get; } = key;
        public string Name { get; } = name;
        public bool IsSolid { get; } = isSolid;
        public bool ConnectToSolid { get; } = connectToSolid;

        public AutoTileStrip Top { get; } = top;
        public AutoTileStrip? Body { get; } = body;

        public AutoSpec Spec => _spec ??= new AutoSpec(this);
        private AutoSpec? _spec;

        public IEnumerable<TileSpec> Outputs => Body is null ? Top.All : Top.All.Concat(Body.All);

        public TileSpec Resolve(bool north, bool east, bool west)
        {
            AutoTileStrip strip = north && Body is not null ? Body : Top;
            return (east, west) switch
            {
                (true, true) => strip.Middle,
                (true, false) => strip.Left,
                (false, true) => strip.Right,
                _ => strip.Single
            };
        }

        public override string ToString() => Key;
    }

    public static class AutoTiles
    {
        public static readonly AutoTileGroup Grass = new("grass", "Grass Ground", true,
            AutoTileStrip.Ids(0, 1, 2, 3, true),
            AutoTileStrip.Ids(8, 9, 10, 11, true));

        public static readonly AutoTileGroup Underground = new("underground", "Underground Ground", true,
            AutoTileStrip.Ids(4, 5, 6, 7, true),
            AutoTileStrip.Ids(12, 13, 14, 15, true));

        public static readonly AutoTileGroup Tree = new("tree", "Treetop", true,
            AutoTileStrip.Ids(41, 42, 43, 42, true));

        public static readonly AutoTileGroup TreeTrunk = new("tree_trunk", "Tree Trunk", false,
            AutoTileStrip.Ids(44, 45, 46, 47, false));

        public static readonly AutoTileGroup CastleFloor = new("castle_floor", "Castle Floor", true,
            AutoTileStrip.Ids(105, 106, 107, 106, true),
            AutoTileStrip.Ids(113, 114, 115, 114, true));

        public static readonly AutoTileGroup GrassDecor = new("grass_decor", "Bush", false,
            new AutoTileStrip(
                new AnimSpec(AnimTileKind.GrassLeft),
                new AnimSpec(AnimTileKind.GrassMiddle),
                new AnimSpec(AnimTileKind.GrassRight),
                new AnimSpec(AnimTileKind.GrassMiddle)),
            connectToSolid: true);

        public static IReadOnlyList<AutoTileGroup> All { get; } =
            [Grass, Underground, Tree, TreeTrunk, CastleFloor, GrassDecor];

        public static AutoTileGroup? Get(string key) => All.FirstOrDefault(g => g.Key == key);

        /// <summary>
        /// Returns the concrete spec of a cell, resolving autotiles from their neighbours.
        /// </summary>
        public static TileSpec ResolveAt(LevelData level, int col, int row)
        {
            TileSpec spec = level.Get(col, row);
            return spec is AutoSpec auto ? ResolveFor(level, auto.Group, col, row) : spec;
        }

        /// <summary>What a cell of <paramref name="group"/> would look like at the given position.</summary>
        public static TileSpec ResolveFor(LevelData level, AutoTileGroup group, int col, int row)
            => group.Resolve(
                Connects(level, group, col, row - 1),
                Connects(level, group, col + 1, row),
                Connects(level, group, col - 1, row));

        private static bool Connects(LevelData level, AutoTileGroup group, int col, int row)
        {
            // Always connect to the edge of the map
            if (!level.InBounds(col, row))
                return true;

            TileSpec other = level.Get(col, row);
            if (other is AutoSpec auto && auto.Group == group)
                return true;

            return group.ConnectToSolid && other.IsSolid;
        }
    }
}
