using System.Collections.Immutable;
using System.Globalization;

namespace Mario2
{
    /// <summary>
    /// Reads the original (pre-v2) level format so old levels can be migrated:
    /// a <c>;</c>-separated meta line, 14 rows of <c>simple,id,collidable,</c> /
    /// <c>anim,Mario2.Type,data</c> cells and one <c>Mario2.Type;x;y;data</c> line per entity.
    /// </summary>
    public static class LegacyLevelParser
    {
        public static LevelData Parse(string text, List<string>? warnings = null)
        {
            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            if (lines.Length < LevelData.Height + 1)
                throw new LevelFormatException(0, "Legacy level is truncated.");

            // Meta: cols;background;playerX;playerY;music;pipeIntro;time
            string[] meta = lines[0].Split(';');
            if (meta.Length < 7)
                throw new LevelFormatException(1, "Legacy meta line is incomplete.");

            // The old format stored the player's and entities' positions; levels now store feet
            int width = Int(meta[0], 1);
            (int spawnX, int spawnY) = EntityMetrics.PlayerFeetFromPosition(new Vector2(Int(meta[2], 1), Int(meta[3], 1)));
            LevelData level = new(new LevelMeta(
                width,
                (WorldBackground)Int(meta[1], 1),
                (MusicName)Int(meta[4], 1),
                Int(meta[6], 1),
                bool.Parse(meta[5]),
                spawnX,
                spawnY));

            for (int row = 0; row < LevelData.Height; row++)
            {
                string[] cells = lines[row + 1].Split(';');
                for (int col = 0; col < width && col < cells.Length; col++)
                {
                    level.Set(col, row, ParseCell(cells[col], row + 2, warnings));
                }
            }

            for (int i = LevelData.Height + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                ParseEntity(level, lines[i].Split(';', 4), i + 1, warnings);
            }

            PromoteAutotiles(level);
            return level;
        }

        private static TileSpec ParseCell(string cell, int lineNo, List<string>? warnings)
        {
            string[] attr = cell.Split(',');
            if (attr[0] == "simple")
            {
                int id = Int(attr[1], lineNo);
                return id < 0 ? TileSpec.Air : new SimpleSpec(id, bool.Parse(attr[2]));
            }

            if (attr[0] != "anim")
                return TileSpec.Air;

            string[] data = attr[2..];
            switch (attr[1])
            {
                case "Mario2.QuestionTile":
                    return new QuestionSpec((ItemType)Int(data[0], lineNo), bool.Parse(data[1]));

                case "Mario2.BrickTile":
                    return new BrickSpec((ItemType)Int(data[0], lineNo), bool.Parse(data[1]), (TilePalette)Int(data[2], lineNo));

                case "Mario2.CoinTile": return new AnimSpec(AnimTileKind.Coin);
                case "Mario2.GrassDecorTileLeft": return new AnimSpec(AnimTileKind.GrassLeft);
                case "Mario2.GrassDecorTileMiddle": return new AnimSpec(AnimTileKind.GrassMiddle);
                case "Mario2.GrassDecorTileRight": return new AnimSpec(AnimTileKind.GrassRight);
                case "Mario2.LavaTile": return new AnimSpec(AnimTileKind.Lava);
                case "Mario2.LavaTileBottom": return new AnimSpec(AnimTileKind.LavaBottom);

                default:
                    warnings?.Add($"Line {lineNo}: unknown legacy tile '{attr[1]}', using air.");
                    return TileSpec.Air;
            }
        }

        private static void ParseEntity(LevelData level, string[] parts, int lineNo, List<string>? warnings)
        {
            if (parts.Length < 3)
            {
                warnings?.Add($"Line {lineNo}: malformed entity line was skipped.");
                return;
            }

            int x = Int(parts[1], lineNo);
            int y = Int(parts[2], lineNo);
            string[] data = parts.Length > 3 ? parts[3].Split(',') : [string.Empty];

            (string Key, Dictionary<string, string> Props)? converted = parts[0] switch
            {
                "Mario2.Goomba" => ("goomba", new() { ["palette"] = data[0] }),
                "Mario2.Koopa" => ("koopa", new() { ["palette"] = data[0] }),
                "Mario2.RedKoopa" => ("red_koopa", []),
                "Mario2.RedFlyingKoopa" => ("red_paratroopa", new() { ["tile_offset"] = Tenths(data[0], lineNo) }),
                "Mario2.Bowser" => ("bowser", []),
                "Mario2.Firebar" => ("firebar", new() { ["length"] = data[0], ["clockwise"] = data[1].ToLowerInvariant() }),
                "Mario2.ConstantPlatform" => ("platform_constant", new() { ["size"] = data[0], ["direction"] = data[1] }),
                "Mario2.PingPongPlatform" => ("platform_pingpong", new()
                {
                    ["size"] = data[0],
                    ["horizontal"] = data[1].ToLowerInvariant(),
                    ["tile_offset"] = Tenths(data[2], lineNo)
                }),
                "Mario2.Axe" => ("axe", []),
                "Mario2.Toad" => ("toad", []),
                "Mario2.WarpPipe" => ("warp_pipe", new()
                {
                    ["dest"] = data[0],
                    ["tag"] = data[1],
                    ["dir"] = ((PipeDirection)Int(data[2], lineNo)).ToString(),
                    ["return_to_parent"] = data[3].ToLowerInvariant()
                }),
                "Mario2.WarpExit" => ("warp_exit", new() { ["tag"] = parts.Length > 3 ? parts[3] : string.Empty }),
                "Mario2.Midway" => ("midway", []),
                "Mario2.NoPlant" => ("no_plant", []),
                "Mario2.Flagpole" => ("flagpole", []),
                "Mario2.CastleEntrance" => ("castle_entrance", []),
                _ => null
            };

            if (converted is null)
            {
                warnings?.Add($"Line {lineNo}: unknown legacy entity '{parts[0]}' was skipped.");
                return;
            }

            EntityDef def = EntityCatalog.Get(converted.Value.Key)!;
            ImmutableSortedDictionary<string, string> props = def.DefaultProps();
            foreach ((string key, string value) in converted.Value.Props)
            {
                PropertyDef property = def.FindProperty(key)!;
                if (property.TryNormalize(value, out string normalized))
                    props = props.SetItem(key, normalized);
                else
                    warnings?.Add($"Line {lineNo}: invalid {def.Key}.{key} value '{value}', using default.");
            }

            (int feetX, int feetY) = EntityMetrics.FeetFromPosition(def, props, new Vector2(x, y));
            level.AddEntity(def.Key, feetX, feetY, props);
        }

        /// <summary>
        /// Legacy levels store the already-resolved autotile pieces. Turn every cell that could
        /// belong to an autotile group back into that group, then revert any cell whose
        /// resolution doesn't reproduce the original piece, until stable. The result looks
        /// exactly like the original but re-tiles itself when edited.
        /// </summary>
        private static void PromoteAutotiles(LevelData level)
        {
            Dictionary<TileSpec, AutoTileGroup> owners = [];
            foreach (AutoTileGroup group in AutoTiles.All)
            {
                foreach (TileSpec output in group.Outputs)
                    owners.TryAdd(output, group);
            }

            List<(int Col, int Row, TileSpec Original)> promoted = [];
            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    TileSpec original = level.Get(col, row);
                    if (owners.TryGetValue(original, out AutoTileGroup? group))
                    {
                        level.Set(col, row, group.Spec);
                        promoted.Add((col, row, original));
                    }
                }
            }

            bool changed;
            do
            {
                changed = false;
                for (int i = promoted.Count - 1; i >= 0; i--)
                {
                    (int col, int row, TileSpec original) = promoted[i];
                    if (AutoTiles.ResolveAt(level, col, row) != original)
                    {
                        level.Set(col, row, original);
                        promoted.RemoveAt(i);
                        changed = true;
                    }
                }
            }
            while (changed);
        }

        private static int Int(string text, int lineNo)
            => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value
                : throw new LevelFormatException(lineNo, $"Expected a number, got '{text}'.");

        // Legacy files stored tile offsets multiplied by ten
        private static string Tenths(string text, int lineNo)
            => ValueCodecs.Float.Format(Int(text, lineNo) / 10f);
    }
}
