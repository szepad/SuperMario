namespace Mario2.Editor
{
    public readonly record struct TileRect(int Col, int Row, int Width, int Height)
    {
        public int Right => Col + Width;
        public int Bottom => Row + Height;

        public static TileRect FromCorners(int col1, int row1, int col2, int row2)
            => new(Math.Min(col1, col2), Math.Min(row1, row2), Math.Abs(col2 - col1) + 1, Math.Abs(row2 - row1) + 1);

        public bool Contains(int col, int row) => col >= Col && col < Right && row >= Row && row < Bottom;

        public TileRect ClampTo(LevelData level)
        {
            int col = Math.Clamp(Col, 0, level.Width);
            int row = Math.Clamp(Row, 0, LevelData.Height);
            return new(col, row, Math.Clamp(Right, 0, level.Width) - col, Math.Clamp(Bottom, 0, LevelData.Height) - row);
        }

        public Rectangle ToWorld() => new(Col * Tile.Size, Row * Tile.Size, Width * Tile.Size, Height * Tile.Size);
    }

    /// <summary>
    /// What's selected: entities (by placement id, so selections survive undo), the player
    /// start, a single tile, or a rectangular tile region.
    /// </summary>
    public sealed class EditorSelection
    {
        public IReadOnlyCollection<int> Entities => _entities;
        public bool Spawn { get; private set; }
        public (int Col, int Row)? Tile { get; private set; }
        public TileRect? Region { get; private set; }

        public bool IsEmpty => _entities.Count == 0 && !Spawn && Tile is null && Region is null;
        public bool HasMovables => _entities.Count > 0 || Spawn;

        /// <summary>Increments whenever the selection changes.</summary>
        public int Version { get; private set; }

        private readonly HashSet<int> _entities = [];

        public bool Contains(int entityId) => _entities.Contains(entityId);

        public void Clear()
        {
            if (IsEmpty)
                return;

            _entities.Clear();
            Spawn = false;
            Tile = null;
            Region = null;
            Version++;
        }

        public void SelectEntity(int id, bool additive = false)
        {
            if (!additive)
                ClearSilently();

            _entities.Add(id);
            Tile = null;
            Version++;
        }

        public void ToggleEntity(int id)
        {
            if (!_entities.Remove(id))
                _entities.Add(id);

            Tile = null;
            Version++;
        }

        public void SelectEntities(IEnumerable<int> ids, bool additive = false)
        {
            if (!additive)
                ClearSilently();

            _entities.UnionWith(ids);
            Version++;
        }

        public void SelectSpawn(bool additive = false)
        {
            if (!additive)
                ClearSilently();

            Spawn = true;
            Tile = null;
            Version++;
        }

        public void ToggleSpawn()
        {
            Spawn = !Spawn;
            Tile = null;
            Version++;
        }

        public void SelectTile(int col, int row)
        {
            ClearSilently();
            Tile = (col, row);
            Version++;
        }

        public void SetRegion(TileRect? region)
        {
            Region = region;
            Tile = null;
            Version++;
        }

        /// <summary>Drops references to entities or cells that no longer exist (e.g. after undo).</summary>
        public void Prune(LevelData level)
        {
            int removed = _entities.RemoveWhere(id => level.FindEntity(id) is null);
            bool changed = removed > 0;

            if (Tile is { } tile && !level.InBounds(tile.Col, tile.Row))
            {
                Tile = null;
                changed = true;
            }

            if (Region is { } region)
            {
                TileRect clamped = region.ClampTo(level);
                Region = clamped.Width > 0 && clamped.Height > 0 ? clamped : null;
                changed |= Region != region;
            }

            if (changed)
                Version++;
        }

        private void ClearSilently()
        {
            _entities.Clear();
            Spawn = false;
            Tile = null;
            Region = null;
        }
    }
}
