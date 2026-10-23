namespace Mario2
{
    public enum AnimTileKind
    {
        Coin,
        GrassLeft,
        GrassMiddle,
        GrassRight,
        Lava,
        LavaBottom
    }

    /// <summary>
    /// Immutable description of a single level cell. Specs are pure data: they're what
    /// levels store and what the editor paints, and they create the runtime <see cref="Tile"/>s.
    /// </summary>
    public abstract record TileSpec
    {
        public static readonly AirSpec Air = new();

        public abstract bool IsSolid { get; }
        public bool IsAir => this is AirSpec;

        public abstract Tile CreateTile();
    }

    public sealed record AirSpec : TileSpec
    {
        public override bool IsSolid => false;
        public override Tile CreateTile() => SimpleTile.Air;
    }

    public sealed record SimpleSpec(int Id, bool Solid) : TileSpec
    {
        public SimpleSpec(int id) : this(id, TilesetInfo.IsSolidByDefault(id)) { }

        public override bool IsSolid => Solid;
        public override Tile CreateTile() => new SimpleTile(Id, Solid);
    }

    /// <summary>
    /// A cell belonging to an autotile group. It has no visual on its own; it's resolved
    /// into a concrete spec from its neighbours (see <see cref="AutoTiles.ResolveAt"/>).
    /// </summary>
    public sealed record AutoSpec(AutoTileGroup Group) : TileSpec
    {
        public override bool IsSolid => Group.IsSolid;

        public override Tile CreateTile()
            => throw new InvalidOperationException($"Autotile '{Group.Key}' must be resolved before creating a tile.");
    }

    public sealed record QuestionSpec(ItemType Item = ItemType.None, bool Hidden = false) : TileSpec
    {
        public override bool IsSolid => true;

        public override Tile CreateTile()
        {
            QuestionTile tile = new();
            tile.SetItem(Item);
            if (Hidden)
                tile.SetHidden();

            return tile;
        }
    }

    public sealed record BrickSpec(ItemType Item = ItemType.None, bool Hidden = false,
        TilePalette Palette = TilePalette.Overworld) : TileSpec
    {
        public override bool IsSolid => true;

        public override Tile CreateTile()
        {
            BrickTile tile = new();
            tile.SetItem(Item);
            if (Hidden)
                tile.SetHidden();

            tile.SetPalette(Palette);
            return tile;
        }
    }

    public sealed record AnimSpec(AnimTileKind Kind) : TileSpec
    {
        public override bool IsSolid => false;

        public override Tile CreateTile() => Kind switch
        {
            AnimTileKind.Coin => new CoinTile(),
            AnimTileKind.GrassLeft => new GrassDecorTileLeft(),
            AnimTileKind.GrassMiddle => new GrassDecorTileMiddle(),
            AnimTileKind.GrassRight => new GrassDecorTileRight(),
            AnimTileKind.Lava => new LavaTile(),
            AnimTileKind.LavaBottom => new LavaTileBottom(),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null)
        };
    }
}
