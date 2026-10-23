using System.Collections.Immutable;

namespace Mario2.Editor
{
    public enum PaletteCategory
    {
        Terrain,
        Blocks,
        Structures,
        Enemies,
        Platforms,
        Objects,
        LevelFlow,
        Tileset
    }

    /// <summary>A region of a texture used as a palette thumbnail.</summary>
    public readonly record struct Icon(string Texture, Rectangle Source)
    {
        private const int Px = TilesetInfo.TileSizePx;

        public static Icon Tile(int id) => new(TilesetInfo.TextureName, TilesetInfo.SourceRect(id));
        public static Icon TileRegion(int col, int row, int width, int height)
            => new(TilesetInfo.TextureName, new Rectangle(col * Px, row * Px, width * Px, height * Px));
        public static Icon Entity(int col, int row) => new("entity_selector", new Rectangle(col * Px, row * Px, Px, Px));
        public static Icon Item(ItemType item) => new("items", new Rectangle(0, ((int)item - 1) * Px, Px, Px));
        public static Icon Frame(string texture, int y = 0, int width = Px, int height = Px) => new(texture, new Rectangle(0, y, width, height));
    }

    public sealed class PaletteItem(string name, PaletteCategory category, Icon icon, Func<PaletteItem, Brush> createBrush,
        Icon? badge = null)
    {
        public string Name { get; } = name;
        public PaletteCategory Category { get; } = category;
        public Icon Icon { get; } = icon;
        public Icon? Badge { get; } = badge;

        public Brush CreateBrush() => createBrush(this);
    }

    public static class PaletteCatalog
    {
        public static IReadOnlyList<PaletteItem> Items { get; }

        public static string DisplayName(PaletteCategory category) => category switch
        {
            PaletteCategory.LevelFlow => "Level Flow",
            _ => category.ToString()
        };

        public static IEnumerable<PaletteItem> In(PaletteCategory category) => Items.Where(i => i.Category == category);

        public static PaletteItem Default => Items[0];

        static PaletteCatalog()
        {
            List<PaletteItem> items = [];

            void Tile(PaletteCategory category, string name, Icon icon, TileSpec spec, Icon? badge = null)
                => items.Add(new PaletteItem(name, category, icon, item => new TileBrush(spec, name) { Source = item }, badge));

            void Auto(string name, int iconId, AutoTileGroup group)
                => Tile(PaletteCategory.Terrain, name, Icon.Tile(iconId), group.Spec);

            void Stamp(string name, Icon icon, Stamp stamp)
                => items.Add(new PaletteItem(name, PaletteCategory.Structures, icon, item => new StampBrush(stamp) { Source = item }));

            void Entity(PaletteCategory category, string name, Icon icon, string key, params (string Key, string Value)[] preset)
            {
                EntityDef def = EntityCatalog.Get(key) ?? throw new InvalidOperationException($"Unknown entity '{key}'");
                ImmutableSortedDictionary<string, string> props = def.DefaultProps();
                foreach ((string k, string v) in preset)
                    props = props.SetItem(k, v);

                items.Add(new PaletteItem(name, category, icon, item => new EntityBrush(def, props, name) { Source = item }));
            }

            // Terrain (autotiles)
            Auto("Grass Ground", 3, AutoTiles.Grass);
            Auto("Underground Ground", 7, AutoTiles.Underground);
            Auto("Treetop", 42, AutoTiles.Tree);
            Auto("Tree Trunk", 47, AutoTiles.TreeTrunk);
            Auto("Castle Floor", 106, AutoTiles.CastleFloor);
            Auto("Bush", 40, AutoTiles.GrassDecor);

            // Blocks
            Tile(PaletteCategory.Blocks, "Block", Icon.Tile(16), new SimpleSpec(16, true));
            Tile(PaletteCategory.Blocks, "Underground Block", Icon.Tile(20), new SimpleSpec(20, true));
            Tile(PaletteCategory.Blocks, "? Block (Coin)", Icon.Tile(17), new QuestionSpec(ItemType.Coin), Icon.Item(ItemType.Coin));
            Tile(PaletteCategory.Blocks, "? Block (Power-up)", Icon.Tile(17), new QuestionSpec(ItemType.Mushroom), Icon.Item(ItemType.Mushroom));
            Tile(PaletteCategory.Blocks, "Hidden Block (1-Up)", Icon.Tile(18), new QuestionSpec(ItemType.OneUp, Hidden: true), Icon.Item(ItemType.OneUp));
            Tile(PaletteCategory.Blocks, "Brick", Icon.Tile(19), new BrickSpec());
            Tile(PaletteCategory.Blocks, "Brick (Coins)", Icon.Tile(19), new BrickSpec(ItemType.CoinStash), Icon.Item(ItemType.Coin));
            Tile(PaletteCategory.Blocks, "Brick (Star)", Icon.Tile(19), new BrickSpec(ItemType.Star), Icon.Item(ItemType.Star));
            Tile(PaletteCategory.Blocks, "Underground Brick", Icon.Frame("question", 80), new BrickSpec(Palette: TilePalette.Underground));
            Tile(PaletteCategory.Blocks, "Castle Brick", Icon.Frame("question", 96), new BrickSpec(Palette: TilePalette.Castle));
            Tile(PaletteCategory.Blocks, "Empty Block", Icon.Tile(18), new SimpleSpec(18, true));
            Tile(PaletteCategory.Blocks, "Empty Block (Castle)", Icon.Tile(31), new SimpleSpec(31, true));
            Tile(PaletteCategory.Blocks, "Coin", Icon.Tile(23), new AnimSpec(AnimTileKind.Coin));
            Tile(PaletteCategory.Blocks, "Castle Stair 1", Icon.Tile(108), new SimpleSpec(108, true));
            Tile(PaletteCategory.Blocks, "Castle Stair 2", Icon.Tile(109), new SimpleSpec(109, true));
            Tile(PaletteCategory.Blocks, "Castle Stair 3", Icon.Tile(116), new SimpleSpec(116, true));
            Tile(PaletteCategory.Blocks, "Castle Stair 4", Icon.Tile(117), new SimpleSpec(117, true));
            Tile(PaletteCategory.Blocks, "Castle Wall (Left)", Icon.Tile(120), new SimpleSpec(120, true));
            Tile(PaletteCategory.Blocks, "Castle Wall (Right)", Icon.Tile(112), new SimpleSpec(112, true));
            Tile(PaletteCategory.Blocks, "Castle Ceiling (Single)", Icon.Tile(126), new SimpleSpec(126, true));
            Tile(PaletteCategory.Blocks, "Bridge", Icon.Tile(118), new SimpleSpec(118, true));
            Tile(PaletteCategory.Blocks, "Chain", Icon.Tile(110), new SimpleSpec(110, false));

            // Structures
            Stamp("Pipe", Icon.TileRegion(0, 3, 2, 2), Stamps.Pipe);
            Stamp("Flagpole", Icon.Tile(30), Stamps.Flagpole);
            Stamp("Small Castle", Icon.TileRegion(0, 6, 6, 5), Stamps.SmallCastle);
            Stamp("Castle Ceiling", Icon.TileRegion(4, 15, 2, 1), Stamps.CastleCeiling);
            Stamp("Lava", Icon.Frame("lava"), Stamps.Lava);

            // Enemies
            Entity(PaletteCategory.Enemies, "Goomba", Icon.Entity(1, 0), "goomba");
            Entity(PaletteCategory.Enemies, "Goomba (Underground)", Icon.Entity(2, 0), "goomba", ("palette", nameof(Palette.Underground)));
            Entity(PaletteCategory.Enemies, "Goomba (Castle)", Icon.Entity(3, 0), "goomba", ("palette", nameof(Palette.Castle)));
            Entity(PaletteCategory.Enemies, "Koopa Troopa", Icon.Entity(4, 0), "koopa");
            Entity(PaletteCategory.Enemies, "Koopa Troopa (Underground)", Icon.Entity(5, 0), "koopa", ("palette", nameof(Palette.Underground)));
            Entity(PaletteCategory.Enemies, "Koopa Troopa (Castle)", Icon.Frame("koopa_castle", height: 24), "koopa", ("palette", nameof(Palette.Castle)));
            Entity(PaletteCategory.Enemies, "Red Koopa Troopa", Icon.Entity(6, 0), "red_koopa");
            Entity(PaletteCategory.Enemies, "Red Koopa Paratroopa", Icon.Entity(4, 1), "red_paratroopa", ("tile_offset", "3"));
            Entity(PaletteCategory.Enemies, "Firebar", Icon.Entity(5, 1), "firebar");
            Entity(PaletteCategory.Enemies, "Bowser", Icon.Entity(6, 1), "bowser");

            // Platforms
            Entity(PaletteCategory.Platforms, "Lift (Constant)", Icon.Entity(1, 1), "platform_constant");
            Entity(PaletteCategory.Platforms, "Lift (Back & Forth)", Icon.Frame("platform", 16, 32, 16), "platform_pingpong", ("tile_offset", "3"));

            // Objects
            Entity(PaletteCategory.Objects, "Axe", Icon.Entity(7, 1), "axe");
            Entity(PaletteCategory.Objects, "Toad", Icon.Entity(0, 2), "toad");

            // Level flow
            Entity(PaletteCategory.LevelFlow, "Warp Pipe", Icon.Entity(7, 0), "warp_pipe");
            Entity(PaletteCategory.LevelFlow, "Warp Exit", Icon.Entity(0, 1), "warp_exit");
            Entity(PaletteCategory.LevelFlow, "Midway Checkpoint", Icon.Entity(2, 1), "midway");
            Entity(PaletteCategory.LevelFlow, "No Piranha Plant", Icon.Entity(3, 1), "no_plant");
            Entity(PaletteCategory.LevelFlow, "Flagpole Trigger", Icon.Frame("flag"), "flagpole");
            Entity(PaletteCategory.LevelFlow, "Castle Entrance", Icon.Frame("editor_exit"), "castle_entrance");

            Items = items;
        }

        /// <summary>The palette item that produces an equivalent brush, if any.</summary>
        public static PaletteItem? FindMatch(Brush brush) => Items.FirstOrDefault(item => (item.CreateBrush(), brush) switch
        {
            (TileBrush a, TileBrush b) => a.Spec == b.Spec,
            (EntityBrush a, EntityBrush b) => a.Def == b.Def && a.Props.SequenceEqual(b.Props),
            (StampBrush a, StampBrush b) => a.Stamp == b.Stamp,
            _ => false
        });

        /// <summary>Marks brushes picked from the raw tileset view.</summary>
        public static readonly object TilesetSource = new();

        /// <summary>A brush for any cell of the raw tileset, or a stamp for a rectangular region of it.</summary>
        public static Brush TilesetBrush(TileRect region)
        {
            if (region.Width == 1 && region.Height == 1)
            {
                int id = region.Row * TilesetInfo.Columns + region.Col;
                return new TileBrush(new SimpleSpec(id), $"Tile #{id}") { Source = TilesetSource };
            }

            List<StampCell> cells = [];
            for (int dy = 0; dy < region.Height; dy++)
            {
                for (int dx = 0; dx < region.Width; dx++)
                {
                    int id = (region.Row + dy) * TilesetInfo.Columns + region.Col + dx;
                    cells.Add(new StampCell(dx, dy, new SimpleSpec(id)));
                }
            }

            return new StampBrush(new Stamp($"Tiles {region.Width}x{region.Height}", cells)) { Source = TilesetSource };
        }
    }
}
