namespace Mario2.Editor
{
    // Editing operations. "Now" methods mutate the level inside the caller's open
    // transaction (used while dragging); the rest are complete undo steps.
    public sealed partial class LevelEditor
    {
        private const int Px = TilesetInfo.TileSizePx;

        public void PaintLineNow(int col0, int row0, int col1, int row1, TileSpec spec)
        {
            bool changed = false;
            foreach ((int col, int row) in Line(col0, row0, col1, row1))
            {
                changed |= Document.Level.Set(col, row, spec);
            }

            if (changed)
                Document.MarkChanged();
        }

        public void RemoveEntityNow(int id)
        {
            if (Document.Level.RemoveEntity(id))
                Document.MarkChanged();
        }

        public void MoveSelectionTo(IReadOnlyDictionary<int, (int X, int Y)> origins, (int X, int Y) spawnOrigin, int dx, int dy)
        {
            LevelData level = Document.Level;
            bool changed = false;

            foreach ((int id, (int x, int y)) in origins)
            {
                if (level.FindEntity(id) is { } placement)
                    changed |= level.ReplaceEntity(placement.MovedTo(x + dx, y + dy));
            }

            if (Selection.Spawn)
            {
                LevelMeta moved = level.Meta with { SpawnX = spawnOrigin.X + dx, SpawnY = spawnOrigin.Y + dy };
                if (moved != level.Meta)
                {
                    level.Meta = moved;
                    changed = true;
                }
            }

            if (changed)
                Document.MarkChanged();
        }

        public void PlaceEntity(EntityBrush brush, (int X, int Y) feet)
        {
            (int x, int y) = feet;
            int id = 0;

            Document.Execute($"Place {brush.Name}", level =>
            {
                if (brush.Def.IsUnique && level.Entities.FirstOrDefault(e => e.Key == brush.Def.Key) is { } existing)
                {
                    id = existing.Id;
                    return level.ReplaceEntity(existing with { X = x, Y = y, Props = brush.Props });
                }

                id = level.AddEntity(brush.Def.Key, x, y, brush.Props).Id;
                return true;
            });

            Selection.SelectEntity(id);
        }

        public void PlaceStamp(Stamp stamp, int col, int row)
        {
            Document.Execute($"Place {stamp.Name}", level => stamp.Apply(level, col, row));
        }

        public void FillRect(TileRect rect, TileSpec spec, string label)
        {
            rect = rect.ClampTo(Document.Level);
            Document.Execute(label, level =>
            {
                bool changed = false;
                for (int row = rect.Row; row < rect.Bottom; row++)
                {
                    for (int col = rect.Col; col < rect.Right; col++)
                    {
                        changed |= level.Set(col, row, spec);
                    }
                }

                return changed;
            });
        }

        /// <summary>Turns the entity or tile under the cursor into the current brush.</summary>
        public bool PickAt(ToolInput input)
        {
            if (Preview.HitTest(input.World) is int id && Document.Level.FindEntity(id) is { Def: { } def } placement)
            {
                EntityBrush brush = new(def, placement.Props, def.Name);
                UpdateBrush(new EntityBrush(def, placement.Props, def.Name) { Source = PaletteCatalog.FindMatch(brush) });
                Notify($"Picked {def.Name}");
                return true;
            }

            TileSpec spec = Document.Level.Get(input.Col, input.Row);
            if (spec.IsAir)
                return false;

            TileBrush tileBrush = new(spec, TileNames.Describe(spec));
            UpdateBrush(new TileBrush(spec, tileBrush.Name) { Source = PaletteCatalog.FindMatch(tileBrush) });
            Notify($"Picked {tileBrush.Name}");
            return true;
        }

        #region Selection commands

        public void DeleteSelection()
        {
            if (Selection.Entities.Count > 0)
            {
                List<int> ids = [.. Selection.Entities];
                Document.Execute(ids.Count == 1 ? "Delete Entity" : "Delete Entities",
                    level => ids.Aggregate(false, (changed, id) => level.RemoveEntity(id) | changed));
                Selection.Clear();
            }
            else if (Selection.Region is { } region)
            {
                FillRect(region, TileSpec.Air, "Clear Area");
            }
            else if (Selection.Tile is { } tile)
            {
                Document.Execute("Delete Tile", level => level.Set(tile.Col, tile.Row, TileSpec.Air));
                Selection.Clear();
            }
            else if (Selection.Spawn)
            {
                Notify("The player start can't be deleted, only moved.");
            }
        }

        public void DuplicateSelection()
        {
            if (Selection.Entities.Count == 0)
            {
                Notify("Select entities to duplicate them.");
                return;
            }

            List<int> copies = [];
            Document.Execute("Duplicate", level =>
            {
                foreach (int id in Selection.Entities)
                {
                    if (level.FindEntity(id) is { } placement && placement.Def is { IsUnique: false })
                        copies.Add(level.AddEntity(placement.Key, placement.X + Px, placement.Y, placement.Props).Id);
                }

                return copies.Count > 0;
            });

            if (copies.Count > 0)
                Selection.SelectEntities(copies);
        }

        public void CopySelection()
        {
            LevelData level = Document.Level;
            Stamp? stamp = null;

            if (Selection.Region is { } region)
            {
                stamp = Stamp.FromRegion(level, region, EntitiesIn(region), $"Copied {region.Width}x{region.Height} area");
            }
            else if (Selection.Entities.Count > 0)
            {
                List<EntityPlacement> entities = [.. Selection.Entities.Select(level.FindEntity).OfType<EntityPlacement>()];

                // Pasting puts the top-left entity in the hovered tile
                int col = entities.Min(e => e.Cell.Col);
                int row = Math.Max(0, entities.Min(e => e.Cell.Row));
                List<StampEntity> stampEntities = [.. entities.Select(e => new StampEntity(e.X - col * Px, e.Y - row * Px, e.Key, e.Props))];
                stamp = new Stamp(entities.Count == 1 ? $"Copied {entities[0].Def?.Name}" : $"Copied {entities.Count} entities",
                    [new StampCell(0, 0, TileSpec.Air, OnlyOnEmpty: true)], stampEntities);
            }
            else if (Selection.Tile is { } tile)
            {
                TileSpec spec = level.Get(tile.Col, tile.Row);
                stamp = new Stamp($"Copied {TileNames.Describe(spec)}", [new StampCell(0, 0, spec)]);
            }

            if (stamp is null)
            {
                Notify("Nothing to copy. Select something with the Select tool first.");
                return;
            }

            Clipboard = stamp;
            Notify($"{stamp.Name}. Paste with {Shortcuts.Mod}+V.");
        }

        public void CutSelection()
        {
            CopySelection();
            if (Clipboard is null)
                return;

            if (Selection.Region is { } region)
            {
                List<int> inside = [.. EntitiesIn(region).Select(e => e.Id)];
                Document.Execute("Cut", level =>
                {
                    bool changed = inside.Aggregate(false, (c, id) => level.RemoveEntity(id) | c);
                    for (int row = region.Row; row < region.Bottom; row++)
                        for (int col = region.Col; col < region.Right; col++)
                            changed |= level.Set(col, row, TileSpec.Air);

                    return changed;
                });
                Selection.Clear();
            }
            else
            {
                DeleteSelection();
            }
        }

        public void Paste()
        {
            if (Clipboard is null)
            {
                Notify("The clipboard is empty.");
                return;
            }

            SetBrush(new StampBrush(Clipboard));
            Notify("Click in the level to paste. Pick another brush when you're done.");
        }

        public void SelectAllEntities()
        {
            Selection.SelectEntities(Document.Level.Entities.Select(e => e.Id));
        }

        public void Nudge(int dx, int dy)
        {
            if (!Selection.HasMovables)
                return;

            Dictionary<int, (int X, int Y)> origins = [];
            foreach (int id in Selection.Entities)
            {
                if (Document.Level.FindEntity(id) is { } placement)
                    origins[id] = (placement.X, placement.Y);
            }

            LevelMeta meta = Document.Level.Meta;
            Document.Begin("Nudge");
            MoveSelectionTo(origins, (meta.SpawnX, meta.SpawnY), dx, dy);
            Document.Commit();
        }

        private IEnumerable<EntityPlacement> EntitiesIn(TileRect region)
        {
            Rectangle area = region.ToWorld();
            HashSet<int> inside = [.. Preview.PlacementsInside(area)];
            return Document.Level.Entities.Where(e => inside.Contains(e.Id));
        }

        #endregion

        #region Property edits (inspector)

        public void SetLevelMeta(Func<LevelMeta, LevelMeta> change, string label)
        {
            Document.Execute(label, level =>
            {
                LevelMeta meta = change(level.Meta);
                if (meta == level.Meta)
                    return false;

                level.Meta = meta;
                return true;
            });
        }

        public bool SetLevelWidth(string text)
        {
            if (!int.TryParse(text, out int width) || width < LevelData.MinWidth || width > LevelData.MaxWidth)
            {
                Notify($"Width must be between {LevelData.MinWidth} and {LevelData.MaxWidth} tiles.");
                return false;
            }

            SetLevelMeta(m => m with { Width = width }, "Resize Level");
            Selection.Prune(Document.Level);
            return true;
        }

        public bool SetEntityProperty(int id, PropertyDef property, string text)
        {
            if (!property.TryNormalize(text, out string value))
            {
                Notify($"'{text}' isn't a valid {property.Label.ToLowerInvariant()}.");
                return false;
            }

            Document.Execute($"Edit {property.Label}", level =>
                level.FindEntity(id) is { } placement && level.ReplaceEntity(placement.WithProp(property.Key, value)));
            return true;
        }

        public bool SetEntityPosition(int id, string xText, string yText)
        {
            if (!int.TryParse(xText, out int x) || !int.TryParse(yText, out int y))
                return false;

            Document.Execute("Move", level =>
                level.FindEntity(id) is { } placement && level.ReplaceEntity(placement.MovedTo(x, y)));
            return true;
        }

        public bool SetTileSpec(int col, int row, TileSpec spec, string label)
        {
            Document.Execute(label, level => level.Set(col, row, spec));
            return true;
        }

        /// <summary>Applies a property edit to the current entity brush.</summary>
        public bool SetBrushProperty(PropertyDef property, string text)
        {
            if (Brush is not EntityBrush brush || !property.TryNormalize(text, out string value))
                return false;

            UpdateBrush(brush.With(brush.Props.SetItem(property.Key, value)));
            return true;
        }

        #endregion

        #region Columns

        public void InsertColumns(int count)
        {
            if (TargetColumn() is not int col)
                return;

            Document.Execute("Insert Columns", level =>
            {
                int width = level.Width;
                level.InsertColumns(col, count);
                return level.Width != width;
            });
            Notify($"Inserted {count} column(s) at {col}.");
        }

        public void DeleteColumns(int count)
        {
            if (TargetColumn() is not int col)
                return;

            Document.Execute("Delete Columns", level =>
            {
                int width = level.Width;
                level.DeleteColumns(col, count);
                return level.Width != width;
            });
            Selection.Prune(Document.Level);
            Notify($"Deleted {count} column(s) at {col}.");
        }

        /// <summary>The selected column, or the one under the cursor.</summary>
        private int? TargetColumn()
        {
            if (Selection.Region is { } region)
                return region.Col;

            if (Selection.Tile is { } tile)
                return tile.Col;

            if (Viewport.HasHoverCell)
                return Viewport.Hover.Col;

            Notify("Select a tile or hover the level to choose a column.");
            return null;
        }

        #endregion

        private static IEnumerable<(int Col, int Row)> Line(int col0, int row0, int col1, int row1)
        {
            int dx = Math.Abs(col1 - col0), sx = col0 < col1 ? 1 : -1;
            int dy = -Math.Abs(row1 - row0), sy = row0 < row1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                yield return (col0, row0);
                if (col0 == col1 && row0 == row1)
                    yield break;

                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; col0 += sx; }
                if (e2 <= dx) { err += dx; row0 += sy; }
            }
        }
    }
}
