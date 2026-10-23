namespace Mario2.Editor
{
    public static class TileNames
    {
        public static string Describe(TileSpec spec) => spec switch
        {
            AirSpec => "Empty",
            AutoSpec auto => auto.Group.Name,
            SimpleSpec simple => PaletteCatalog.FindMatch(new TileBrush(simple, string.Empty))?.Name ?? $"Tile #{simple.Id}",
            QuestionSpec q => (q.Hidden ? "Hidden Block" : "? Block") + ItemSuffix(q.Item),
            BrickSpec b => (b.Palette == TilePalette.Overworld ? "Brick" : $"{b.Palette} Brick")
                + (b.Hidden ? " (hidden)" : string.Empty) + ItemSuffix(b.Item),
            AnimSpec anim => anim.Kind switch
            {
                AnimTileKind.Coin => "Coin",
                AnimTileKind.GrassLeft => "Bush (left end)",
                AnimTileKind.GrassMiddle => "Bush (middle)",
                AnimTileKind.GrassRight => "Bush (right end)",
                AnimTileKind.Lava => "Lava",
                AnimTileKind.LavaBottom => "Lava (body)",
                _ => anim.Kind.ToString()
            },
            _ => spec.GetType().Name
        };

        public static string ItemName(ItemType item) => item switch
        {
            ItemType.CoinStash => "Coins",
            ItemType.FireFlower => "Fire Flower",
            ItemType.OneUp => "1-Up",
            _ => item.ToString()
        };

        private static string ItemSuffix(ItemType item) => item == ItemType.None ? string.Empty : $" ({ItemName(item)})";
    }
}
