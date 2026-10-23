using System.Collections.Immutable;

namespace Mario2
{
    /// <summary>
    /// Level-wide settings. Spawn is where the player's feet start (bottom-center, see
    /// <see cref="Entity.Feet"/>) in unscaled (texture) pixels.
    /// </summary>
    public sealed record LevelMeta(int Width, WorldBackground Background, MusicName Music,
        int Time, bool PipeIntro, int SpawnX, int SpawnY)
    {
        public static LevelMeta Default { get; } = new(100, WorldBackground.Sky, MusicName.Overworld,
            400, false, 40, 192);
    }

    /// <summary>
    /// An entity in a level. X/Y are its feet (the bottom-center of its box, see <see cref="Entity.Feet"/>)
    /// in unscaled (texture) pixels, so an entity standing on a tile has its feet on that tile's top edge.
    /// The id is only stable within one <see cref="LevelData"/> (and its clones); it isn't saved.
    /// </summary>
    public sealed record EntityPlacement(int Id, string Key, int X, int Y,
        ImmutableSortedDictionary<string, string> Props)
    {
        public EntityDef? Def => EntityCatalog.Get(Key);

        /// <summary>The tile the entity stands in: its feet are on that tile's bottom edge.</summary>
        public (int Col, int Row) Cell
            => ((int)MathF.Floor(X / (float)TilesetInfo.TileSizePx), (int)MathF.Floor((Y - 1) / (float)TilesetInfo.TileSizePx));

        public EntityPlacement WithProp(string key, string value) => this with { Props = Props.SetItem(key, value) };
        public EntityPlacement MovedTo(int x, int y) => this with { X = x, Y = y };
    }

    /// <summary>
    /// The pure-data model of a level: what files store, what the editor edits and what
    /// <see cref="LevelBuilder"/> turns into a playable <see cref="World"/>.
    /// </summary>
    public sealed class LevelData
    {
        public const int Height = World.Rows;
        public const int MinWidth = 16;
        public const int MaxWidth = 4096;

        public LevelMeta Meta
        {
            get => _meta;
            set
            {
                value = value with { Width = Math.Clamp(value.Width, MinWidth, MaxWidth) };
                if (value.Width != _meta.Width)
                    ResizeCells(value.Width);

                _meta = value;
            }
        }

        public int Width => _meta.Width;
        public List<EntityPlacement> Entities { get; } = [];

        private LevelMeta _meta;
        private TileSpec[,] _cells;
        private int _nextEntityId = 1;

        public LevelData() : this(LevelMeta.Default) { }

        public LevelData(LevelMeta meta)
        {
            _meta = meta with { Width = Math.Clamp(meta.Width, MinWidth, MaxWidth) };
            _cells = new TileSpec[Height, _meta.Width];
            Fill(_cells, TileSpec.Air);
        }

        private LevelData(LevelData other)
        {
            _meta = other._meta;
            _cells = (TileSpec[,])other._cells.Clone();
            _nextEntityId = other._nextEntityId;
            Entities.AddRange(other.Entities);
        }

        public LevelData Clone() => new(this);

        public bool InBounds(int col, int row) => col >= 0 && col < Width && row >= 0 && row < Height;

        public TileSpec Get(int col, int row) => InBounds(col, row) ? _cells[row, col] : TileSpec.Air;

        public bool Set(int col, int row, TileSpec spec)
        {
            if (!InBounds(col, row) || _cells[row, col] == spec)
                return false;

            _cells[row, col] = spec;
            return true;
        }

        public EntityPlacement AddEntity(string key, int x, int y, ImmutableSortedDictionary<string, string>? props = null)
        {
            EntityPlacement placement = new(_nextEntityId++, key, x, y,
                props ?? ImmutableSortedDictionary<string, string>.Empty);
            Entities.Add(placement);
            return placement;
        }

        public EntityPlacement? FindEntity(int id) => Entities.FirstOrDefault(e => e.Id == id);

        public bool ReplaceEntity(EntityPlacement placement)
        {
            int index = Entities.FindIndex(e => e.Id == placement.Id);
            if (index < 0 || Entities[index] == placement)
                return false;

            Entities[index] = placement;
            return true;
        }

        public bool RemoveEntity(int id) => Entities.RemoveAll(e => e.Id == id) > 0;

        /// <summary>
        /// Inserts empty columns at <paramref name="at"/>, shifting tiles and entities right of it.
        /// </summary>
        public void InsertColumns(int at, int count)
        {
            at = Math.Clamp(at, 0, Width);
            count = Math.Min(count, MaxWidth - Width);
            if (count <= 0)
                return;

            TileSpec[,] cells = new TileSpec[Height, Width + count];
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width + count; col++)
                {
                    if (col < at) cells[row, col] = _cells[row, col];
                    else if (col < at + count) cells[row, col] = TileSpec.Air;
                    else cells[row, col] = _cells[row, col - count];
                }
            }

            ShiftEntities(at * TilesetInfo.TileSizePx, count * TilesetInfo.TileSizePx);
            _cells = cells;
            _meta = _meta with { Width = Width + count };

            if (_meta.SpawnX >= at * TilesetInfo.TileSizePx)
                _meta = _meta with { SpawnX = _meta.SpawnX + count * TilesetInfo.TileSizePx };
        }

        /// <summary>
        /// Deletes columns starting at <paramref name="at"/> along with the entities inside them,
        /// shifting everything to the right of them left.
        /// </summary>
        public void DeleteColumns(int at, int count)
        {
            if (at < 0 || at >= Width)
                return;

            count = Math.Min(count, Math.Min(Width - at, Width - MinWidth));
            if (count <= 0)
                return;

            TileSpec[,] cells = new TileSpec[Height, Width - count];
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width - count; col++)
                {
                    cells[row, col] = col < at ? _cells[row, col] : _cells[row, col + count];
                }
            }

            int fromPx = at * TilesetInfo.TileSizePx;
            int toPx = (at + count) * TilesetInfo.TileSizePx;
            Entities.RemoveAll(e => e.X >= fromPx && e.X < toPx);
            ShiftEntities(toPx, -count * TilesetInfo.TileSizePx);

            _cells = cells;
            _meta = _meta with { Width = Width - count };

            if (_meta.SpawnX >= toPx)
                _meta = _meta with { SpawnX = _meta.SpawnX - count * TilesetInfo.TileSizePx };
            else if (_meta.SpawnX >= fromPx)
            {
                // The start was in the deleted columns: stand on the first column after them (or the last one)
                int col = Math.Min(at, Width - 1);
                _meta = _meta with { SpawnX = col * TilesetInfo.TileSizePx + TilesetInfo.TileSizePx / 2 };
            }
        }

        private void ShiftEntities(int fromPx, int offsetPx)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                if (Entities[i].X >= fromPx)
                    Entities[i] = Entities[i] with { X = Entities[i].X + offsetPx };
            }
        }

        private void ResizeCells(int width)
        {
            TileSpec[,] cells = new TileSpec[Height, width];
            Fill(cells, TileSpec.Air);

            int copy = Math.Min(width, _cells.GetLength(1));
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < copy; col++)
                {
                    cells[row, col] = _cells[row, col];
                }
            }

            _cells = cells;
        }

        private static void Fill(TileSpec[,] cells, TileSpec spec)
        {
            for (int row = 0; row < cells.GetLength(0); row++)
            {
                for (int col = 0; col < cells.GetLength(1); col++)
                {
                    cells[row, col] = spec;
                }
            }
        }
    }
}
