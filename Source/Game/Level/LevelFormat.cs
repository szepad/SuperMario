using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Mario2
{
    public sealed class LevelFormatException(int line, string message)
        : Exception(line > 0 ? $"Line {line}: {message}" : message)
    {
        public int Line { get; } = line;
    }

    /// <summary>
    /// Reader and writer of the line-based <c>.lvl</c> v2 format.
    /// <code>
    /// MARIOLVL 2
    /// size 211 14
    /// background Sky
    /// music Overworld
    /// time 400
    /// pipe_intro false
    /// spawn 48 192
    /// def a tile 1 solid
    /// def b auto grass
    /// def c question item=Mushroom hidden=false
    /// grid
    /// .*20 a*3 .*188        (one line per row, run-length encoded legend codes, '.' is air)
    /// end
    /// entity goomba 1320 64 palette=Overworld
    /// </code>
    /// <c>;</c> starts a comment. Values may be double-quoted, with <c>\</c> escapes.
    /// Spawn and entity coordinates are feet (bottom-center) in texture pixels.
    /// </summary>
    public static class LevelFormat
    {
        public const string Magic = "MARIOLVL";
        public const int Version = 2;
        public const string Extension = ".lvl";

        private const string AirCode = ".";

        public static bool IsCurrentFormat(string text)
        {
            using StringReader reader = new(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                return line.TrimStart().StartsWith(Magic, StringComparison.Ordinal);
            }

            return false;
        }

        public static void Save(LevelData level, string path)
        {
            // Write to a temp file first so a crash mid-write can't corrupt an existing level
            string temp = path + ".tmp";
            File.WriteAllText(temp, Write(level));
            File.Move(temp, path, overwrite: true);
        }

        #region Writing

        public static string Write(LevelData level)
        {
            StringBuilder sb = new();
            LevelMeta meta = level.Meta;

            sb.AppendLine($"{Magic} {Version}");
            sb.AppendLine(Invariant($"size {level.Width} {LevelData.Height}"));
            sb.AppendLine($"background {meta.Background}");
            sb.AppendLine($"music {meta.Music}");
            sb.AppendLine(Invariant($"time {meta.Time}"));
            sb.AppendLine($"pipe_intro {ValueCodecs.Bool.Format(meta.PipeIntro)}");
            sb.AppendLine(Invariant($"spawn {meta.SpawnX} {meta.SpawnY}"));
            sb.AppendLine();

            // Legend, in order of first appearance
            Dictionary<TileSpec, string> codes = [];
            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    TileSpec spec = level.Get(col, row);
                    if (spec.IsAir || codes.ContainsKey(spec))
                        continue;

                    string code = CodeFor(codes.Count);
                    codes.Add(spec, code);
                    sb.AppendLine($"def {code} {WriteSpec(spec)}");
                }
            }

            sb.AppendLine("grid");
            for (int row = 0; row < LevelData.Height; row++)
            {
                List<string> runs = [];
                int col = 0;
                while (col < level.Width)
                {
                    TileSpec spec = level.Get(col, row);
                    int start = col;
                    while (col < level.Width && level.Get(col, row) == spec)
                        col++;

                    string code = spec.IsAir ? AirCode : codes[spec];
                    int count = col - start;
                    runs.Add(count == 1 ? code : Invariant($"{code}*{count}"));
                }

                sb.AppendLine(string.Join(' ', runs));
            }
            sb.AppendLine("end");
            sb.AppendLine();

            foreach (EntityPlacement entity in level.Entities)
            {
                sb.Append(Invariant($"entity {entity.Key} {entity.X} {entity.Y}"));

                // Schema order reads nicer than alphabetical; unknown props go last
                EntityDef? def = entity.Def;
                IEnumerable<string> keys = def is null
                    ? entity.Props.Keys
                    : def.Properties.Select(p => p.Key).Where(entity.Props.ContainsKey)
                        .Concat(entity.Props.Keys.Where(k => def.FindProperty(k) is null));

                foreach (string key in keys)
                {
                    sb.Append($" {key}={Quote(entity.Props[key])}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string WriteSpec(TileSpec spec) => spec switch
        {
            SimpleSpec s => Invariant($"tile {s.Id} {(s.Solid ? "solid" : "passable")}"),
            AutoSpec a => $"auto {a.Group.Key}",
            QuestionSpec q => $"question item={q.Item} hidden={ValueCodecs.Bool.Format(q.Hidden)}",
            BrickSpec b => $"brick item={b.Item} hidden={ValueCodecs.Bool.Format(b.Hidden)} palette={b.Palette}",
            AnimSpec { Kind: AnimTileKind.Coin } => "coin",
            AnimSpec { Kind: AnimTileKind.GrassLeft } => "grass_left",
            AnimSpec { Kind: AnimTileKind.GrassMiddle } => "grass_middle",
            AnimSpec { Kind: AnimTileKind.GrassRight } => "grass_right",
            AnimSpec { Kind: AnimTileKind.Lava } => "lava",
            AnimSpec { Kind: AnimTileKind.LavaBottom } => "lava_bottom",
            _ => throw new ArgumentException($"Unsupported tile spec: {spec}")
        };

        private static string CodeFor(int index)
        {
            const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            StringBuilder code = new();
            do
            {
                code.Insert(0, Alphabet[index % Alphabet.Length]);
                index = index / Alphabet.Length - 1;
            }
            while (index >= 0);

            return code.ToString();
        }

        private static string Quote(string value)
        {
            bool needsQuotes = value.Length == 0 || value.Any(c => char.IsWhiteSpace(c) || c is '"' or '\\' or ';');
            if (!needsQuotes)
                return value;

            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

        #endregion

        #region Reading

        public static LevelData Read(string text, List<string>? warnings = null)
        {
            string[] lines = text.Split('\n');

            LevelMeta meta = LevelMeta.Default;
            bool sawHeader = false;
            bool sawSize = false;
            Dictionary<string, TileSpec> legend = new() { [AirCode] = TileSpec.Air };
            List<TileSpec[]> rows = [];
            bool inGrid = false;
            bool gridDone = false;
            List<(int Line, string Key, int X, int Y, ImmutableSortedDictionary<string, string> Props)> entities = [];

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                List<string> tokens = Tokenize(lines[i].TrimEnd('\r'), lineNo);
                if (tokens.Count == 0)
                    continue;

                if (!sawHeader)
                {
                    if (tokens[0] != Magic || tokens.Count < 2)
                        throw new LevelFormatException(lineNo, $"Not a level file (expected '{Magic} {Version}' header).");

                    int version = ParseInt(tokens[1], lineNo);
                    if (version > Version)
                        throw new LevelFormatException(lineNo, $"Level was saved by a newer version (format {version}).");

                    sawHeader = true;
                    continue;
                }

                if (inGrid)
                {
                    if (tokens[0] == "end")
                    {
                        inGrid = false;
                        gridDone = true;
                        continue;
                    }

                    rows.Add(ReadRow(tokens, legend, meta.Width, lineNo));
                    continue;
                }

                switch (tokens[0])
                {
                    case "size":
                        Expect(tokens, 3, lineNo);
                        meta = meta with { Width = ParseInt(tokens[1], lineNo) };
                        if (ParseInt(tokens[2], lineNo) != LevelData.Height)
                            throw new LevelFormatException(lineNo, $"Only levels {LevelData.Height} tiles high are supported.");
                        if (meta.Width < LevelData.MinWidth || meta.Width > LevelData.MaxWidth)
                            throw new LevelFormatException(lineNo, $"Width must be between {LevelData.MinWidth} and {LevelData.MaxWidth}.");
                        sawSize = true;
                        break;

                    case "background":
                        Expect(tokens, 2, lineNo);
                        meta = meta with { Background = ParseEnum<WorldBackground>(tokens[1], lineNo) };
                        break;

                    case "music":
                        Expect(tokens, 2, lineNo);
                        meta = meta with { Music = ParseEnum<MusicName>(tokens[1], lineNo) };
                        break;

                    case "time":
                        Expect(tokens, 2, lineNo);
                        meta = meta with { Time = ParseInt(tokens[1], lineNo) };
                        break;

                    case "pipe_intro":
                        Expect(tokens, 2, lineNo);
                        meta = meta with { PipeIntro = ParseBool(tokens[1], lineNo) };
                        break;

                    case "spawn":
                        Expect(tokens, 3, lineNo);
                        meta = meta with { SpawnX = ParseInt(tokens[1], lineNo), SpawnY = ParseInt(tokens[2], lineNo) };
                        break;

                    case "def":
                        Expect(tokens, 3, lineNo);
                        if (tokens[1] == AirCode || legend.ContainsKey(tokens[1]))
                            throw new LevelFormatException(lineNo, $"Tile code '{tokens[1]}' is defined twice.");

                        legend[tokens[1]] = ReadSpec(tokens, lineNo, warnings);
                        break;

                    case "grid":
                        if (gridDone)
                            throw new LevelFormatException(lineNo, "Only one grid is allowed.");
                        if (!sawSize)
                            throw new LevelFormatException(lineNo, "'size' must come before the grid.");

                        inGrid = true;
                        break;

                    case "entity":
                        Expect(tokens, 4, lineNo);
                        entities.Add((lineNo, tokens[1], ParseInt(tokens[2], lineNo), ParseInt(tokens[3], lineNo),
                            ReadKeyValues(tokens, 4, lineNo)));
                        break;

                    default:
                        warnings?.Add($"Line {lineNo}: unknown record '{tokens[0]}' was skipped.");
                        break;
                }
            }

            if (!sawHeader)
                throw new LevelFormatException(0, "File is empty.");
            if (inGrid)
                throw new LevelFormatException(lines.Length, "Grid is missing its 'end'.");
            if (rows.Count != LevelData.Height)
                throw new LevelFormatException(0, $"Expected {LevelData.Height} grid rows, found {rows.Count}.");

            LevelData level = new(meta);
            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = 0; col < meta.Width; col++)
                {
                    level.Set(col, row, rows[row][col]);
                }
            }

            foreach (var (line, key, x, y, props) in entities)
            {
                EntityDef? def = EntityCatalog.Get(key);
                if (def is null)
                {
                    warnings?.Add($"Line {line}: unknown entity '{key}' was skipped.");
                    continue;
                }

                ImmutableSortedDictionary<string, string>.Builder normalized = ImmutableSortedDictionary.CreateBuilder<string, string>();
                foreach (PropertyDef property in def.Properties)
                {
                    string value = property.DefaultText;
                    if (props.TryGetValue(property.Key, out string? raw))
                    {
                        if (property.TryNormalize(raw, out string norm))
                            value = norm;
                        else
                            warnings?.Add($"Line {line}: invalid {key}.{property.Key} value '{raw}', using '{value}'.");
                    }

                    normalized[property.Key] = value;
                }

                foreach (string unknown in props.Keys.Where(k => def.FindProperty(k) is null))
                {
                    warnings?.Add($"Line {line}: unknown property '{unknown}' on '{key}' was skipped.");
                }

                level.AddEntity(key, x, y, normalized.ToImmutable());
            }

            return level;
        }

        private static TileSpec ReadSpec(List<string> tokens, int lineNo, List<string>? warnings)
        {
            string kind = tokens[2];
            ImmutableSortedDictionary<string, string> args = ReadKeyValues(tokens, 3, lineNo, allowPositional: true);

            switch (kind)
            {
                case "tile":
                    if (tokens.Count < 5)
                        throw new LevelFormatException(lineNo, "Expected 'tile <id> solid|passable'.");

                    int id = ParseInt(tokens[3], lineNo);
                    bool solid = tokens[4] switch
                    {
                        "solid" => true,
                        "passable" => false,
                        _ => throw new LevelFormatException(lineNo, $"Expected 'solid' or 'passable', got '{tokens[4]}'.")
                    };
                    return new SimpleSpec(id, solid);

                case "auto":
                    if (tokens.Count < 4)
                        throw new LevelFormatException(lineNo, "Expected 'auto <group>'.");

                    AutoTileGroup? group = AutoTiles.Get(tokens[3]);
                    if (group is null)
                    {
                        warnings?.Add($"Line {lineNo}: unknown autotile '{tokens[3]}', using air.");
                        return TileSpec.Air;
                    }
                    return group.Spec;

                case "question":
                    return new QuestionSpec(
                        ArgEnum(args, "item", ItemType.None, lineNo),
                        ArgBool(args, "hidden", false, lineNo));

                case "brick":
                    return new BrickSpec(
                        ArgEnum(args, "item", ItemType.None, lineNo),
                        ArgBool(args, "hidden", false, lineNo),
                        ArgEnum(args, "palette", TilePalette.Overworld, lineNo));

                case "coin": return new AnimSpec(AnimTileKind.Coin);
                case "grass_left": return new AnimSpec(AnimTileKind.GrassLeft);
                case "grass_middle": return new AnimSpec(AnimTileKind.GrassMiddle);
                case "grass_right": return new AnimSpec(AnimTileKind.GrassRight);
                case "lava": return new AnimSpec(AnimTileKind.Lava);
                case "lava_bottom": return new AnimSpec(AnimTileKind.LavaBottom);

                default:
                    warnings?.Add($"Line {lineNo}: unknown tile kind '{kind}', using air.");
                    return TileSpec.Air;
            }
        }

        private static TileSpec[] ReadRow(List<string> tokens, Dictionary<string, TileSpec> legend, int width, int lineNo)
        {
            TileSpec[] row = new TileSpec[width];
            int col = 0;

            foreach (string token in tokens)
            {
                string code = token;
                int count = 1;

                int star = token.IndexOf('*');
                if (star >= 0)
                {
                    code = token[..star];
                    count = ParseInt(token[(star + 1)..], lineNo);
                    if (count <= 0)
                        throw new LevelFormatException(lineNo, $"Invalid run length in '{token}'.");
                }

                if (!legend.TryGetValue(code, out TileSpec? spec))
                    throw new LevelFormatException(lineNo, $"Unknown tile code '{code}'.");

                if (col + count > width)
                    throw new LevelFormatException(lineNo, $"Row is wider than the level ({width}).");

                for (int i = 0; i < count; i++)
                    row[col++] = spec;
            }

            if (col != width)
                throw new LevelFormatException(lineNo, $"Row has {col} tiles, expected {width}.");

            return row;
        }

        private static ImmutableSortedDictionary<string, string> ReadKeyValues(List<string> tokens, int start, int lineNo,
            bool allowPositional = false)
        {
            ImmutableSortedDictionary<string, string>.Builder values = ImmutableSortedDictionary.CreateBuilder<string, string>();
            for (int i = start; i < tokens.Count; i++)
            {
                int eq = tokens[i].IndexOf('=');
                if (eq <= 0)
                {
                    if (allowPositional)
                        continue;

                    throw new LevelFormatException(lineNo, $"Expected key=value, got '{tokens[i]}'.");
                }

                values[tokens[i][..eq]] = tokens[i][(eq + 1)..];
            }

            return values.ToImmutable();
        }

        private static TEnum ArgEnum<TEnum>(ImmutableSortedDictionary<string, string> args, string key, TEnum fallback, int lineNo)
            where TEnum : struct, Enum
            => args.TryGetValue(key, out string? value) ? ParseEnum<TEnum>(value, lineNo) : fallback;

        private static bool ArgBool(ImmutableSortedDictionary<string, string> args, string key, bool fallback, int lineNo)
            => args.TryGetValue(key, out string? value) ? ParseBool(value, lineNo) : fallback;

        private static void Expect(List<string> tokens, int count, int lineNo)
        {
            if (tokens.Count < count)
                throw new LevelFormatException(lineNo, $"'{tokens[0]}' expects {count - 1} value(s).");
        }

        private static int ParseInt(string text, int lineNo)
            => ValueCodecs.Int.TryParse(text, out int value) ? value
                : throw new LevelFormatException(lineNo, $"Expected a number, got '{text}'.");

        private static bool ParseBool(string text, int lineNo)
            => ValueCodecs.Bool.TryParse(text, out bool value) ? value
                : throw new LevelFormatException(lineNo, $"Expected true or false, got '{text}'.");

        private static TEnum ParseEnum<TEnum>(string text, int lineNo) where TEnum : struct, Enum
            => ValueCodecs.Enum<TEnum>().TryParse(text, out TEnum value) ? value
                : throw new LevelFormatException(lineNo, $"Unknown {typeof(TEnum).Name} '{text}'.");

        /// <summary>
        /// Splits a line on whitespace. Double quotes group text (and may appear mid-token,
        /// as in <c>key="a b"</c>), backslash escapes inside quotes, and an unquoted
        /// <c>;</c> at the start of a token begins a comment.
        /// </summary>
        private static List<string> Tokenize(string line, int lineNo)
        {
            List<string> tokens = [];
            StringBuilder current = new();
            bool inToken = false;
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '\\' && i + 1 < line.Length)
                    {
                        current.Append(line[++i]);
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }

                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    if (inToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        inToken = false;
                    }

                    continue;
                }

                if (c == ';' && !inToken)
                    break;

                inToken = true;
                if (c == '"')
                    inQuotes = true;
                else
                    current.Append(c);
            }

            if (inQuotes)
                throw new LevelFormatException(lineNo, "Unterminated quote.");

            if (inToken)
                tokens.Add(current.ToString());

            return tokens;
        }

        #endregion
    }
}
