using System.Collections.Immutable;

namespace Mario2.Editor
{
    /// <param name="OnlyOnEmpty">Skips cells that already hold a tile.</param>
    /// <param name="FillDown">Repeats the cell downward until it hits a tile (or the bottom of the level).</param>
    public sealed record StampCell(int Dx, int Dy, TileSpec Spec, bool OnlyOnEmpty = false, bool FillDown = false);

    /// <summary>An entity of a stamp: where its feet go, in texture pixels from the stamp's top-left corner.</summary>
    public sealed record StampEntity(int Dx, int Dy, string Key, ImmutableSortedDictionary<string, string> Props);

    /// <summary>
    /// A multi-tile structure (pipe, castle, a copied region...) placed in one click.
    /// </summary>
    public sealed class Stamp(string name, IReadOnlyList<StampCell> cells, IReadOnlyList<StampEntity>? entities = null)
    {
        public string Name { get; } = name;
        public IReadOnlyList<StampCell> Cells { get; } = cells;
        public IReadOnlyList<StampEntity> Entities { get; } = entities ?? [];

        public int Width => Cells.Count == 0 ? 1 : Cells.Max(c => c.Dx) + 1;
        public int Height => Cells.Count == 0 ? 1 : Cells.Max(c => c.Dy) + 1;

        /// <summary>The cells this stamp would write if placed with its top-left corner at the given cell.</summary>
        public IEnumerable<(int Col, int Row, TileSpec Spec)> ResolveCells(LevelData level, int col, int row)
        {
            foreach (StampCell cell in Cells)
            {
                int c = col + cell.Dx;
                int r = row + cell.Dy;

                if (!cell.FillDown)
                {
                    if (level.InBounds(c, r) && !(cell.OnlyOnEmpty && !level.Get(c, r).IsAir))
                        yield return (c, r, cell.Spec);

                    continue;
                }

                for (; r < LevelData.Height; r++)
                {
                    if (!level.InBounds(c, r))
                        continue;

                    if (cell.OnlyOnEmpty && !level.Get(c, r).IsAir)
                        break;

                    yield return (c, r, cell.Spec);
                }
            }
        }

        public IEnumerable<EntityPlacement> ResolveEntities(int col, int row)
        {
            foreach (StampEntity entity in Entities)
            {
                yield return new EntityPlacement(0, entity.Key,
                    col * TilesetInfo.TileSizePx + entity.Dx,
                    row * TilesetInfo.TileSizePx + entity.Dy,
                    entity.Props);
            }
        }

        public bool Apply(LevelData level, int col, int row)
        {
            bool changed = false;
            foreach ((int c, int r, TileSpec spec) in ResolveCells(level, col, row).ToList())
            {
                changed |= level.Set(c, r, spec);
            }

            foreach (EntityPlacement entity in ResolveEntities(col, row))
            {
                // Don't stack duplicates when stamping over the same spot again
                bool exists = level.Entities.Any(e => e.Key == entity.Key && e.X == entity.X && e.Y == entity.Y);
                if (exists)
                    continue;

                if (EntityCatalog.Get(entity.Key) is { IsUnique: true })
                    level.Entities.RemoveAll(e => e.Key == entity.Key);

                level.AddEntity(entity.Key, entity.X, entity.Y, entity.Props);
                changed = true;
            }

            return changed;
        }

        /// <summary>Captures a region of a level (tiles and the entities inside it) as a stamp.</summary>
        public static Stamp FromRegion(LevelData level, TileRect region, IEnumerable<EntityPlacement> entities, string name)
        {
            List<StampCell> cells = [];
            for (int row = region.Row; row < region.Bottom; row++)
            {
                for (int col = region.Col; col < region.Right; col++)
                {
                    TileSpec spec = level.Get(col, row);
                    if (!spec.IsAir)
                        cells.Add(new StampCell(col - region.Col, row - region.Row, spec));
                }
            }

            int originX = region.Col * TilesetInfo.TileSizePx;
            int originY = region.Row * TilesetInfo.TileSizePx;
            List<StampEntity> stampEntities = [.. entities.Select(e => new StampEntity(e.X - originX, e.Y - originY, e.Key, e.Props))];

            // Keep the stamp's footprint even if its right/bottom edges are empty
            if (cells.Count == 0 || cells.Max(c => c.Dx) < region.Width - 1 || cells.Max(c => c.Dy) < region.Height - 1)
                cells.Add(new StampCell(region.Width - 1, region.Height - 1, TileSpec.Air, OnlyOnEmpty: true));

            return new Stamp(name, cells, stampEntities);
        }
    }
}
