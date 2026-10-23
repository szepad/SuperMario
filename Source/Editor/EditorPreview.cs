using System.Diagnostics.CodeAnalysis;

namespace Mario2.Editor
{
    /// <summary>
    /// Mirrors the document as drawable game objects: resolved tiles (autotiles applied) and real
    /// entity instances inside a preview <see cref="World"/> that is drawn but never updated.
    /// Only what changed is rebuilt when the document changes.
    /// </summary>
    public sealed class EditorPreview
    {
        private sealed record PreviewEntity(EntityPlacement Placement, Entity Root, List<Entity> Spawned);

        public World World { get; private set; } = null!;
        public int Width => _resolved.GetLength(1);

        private TileSpec[,] _resolved = new TileSpec[0, 0];
        private readonly Dictionary<TileSpec, Tile> _tileCache = [];
        private readonly Dictionary<int, PreviewEntity> _entities = [];
        private readonly List<PreviewEntity> _ordered = [];

        private LevelData? _syncedLevel;
        private int _syncedVersion = -1;

        public void Sync(LevelData level, int version)
        {
            if (level == _syncedLevel && version == _syncedVersion)
                return;

            if (World is null || Width != level.Width)
                Reset(level);

            if (World.Background != level.Meta.Background)
                World.SetBackground(level.Meta.Background);

            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    _resolved[row, col] = AutoTiles.ResolveAt(level, col, row);
                }
            }

            SyncEntities(level);
            World.Player.SetFeetPos(new Vector2(level.Meta.SpawnX, level.Meta.SpawnY) * Game.TextureScale);

            _syncedLevel = level;
            _syncedVersion = version;
        }

        public TileSpec ResolvedAt(int col, int row)
            => col >= 0 && col < Width && row >= 0 && row < LevelData.Height ? _resolved[row, col] : TileSpec.Air;

        public Tile TileFor(TileSpec spec)
        {
            if (!_tileCache.TryGetValue(spec, out Tile? tile))
            {
                tile = spec.CreateTile();
                _tileCache[spec] = tile;
            }

            return tile;
        }

        public Entity? RootOf(int placementId) => _entities.GetValueOrDefault(placementId)?.Root;

        /// <summary>Returns the topmost placement whose entity covers the point (in world units).</summary>
        public int? HitTest(Vector2 point)
        {
            for (int i = _ordered.Count - 1; i >= 0; i--)
            {
                if (Raylib.CheckCollisionPointRec(point, _ordered[i].Root.Rect))
                    return _ordered[i].Placement.Id;
            }

            return null;
        }

        public IEnumerable<int> PlacementsInside(Rectangle area)
            => _ordered.Where(p => Raylib.CheckCollisionRecs(area, p.Root.Rect)).Select(p => p.Placement.Id);

        public bool SpawnHit(Vector2 point) => Raylib.CheckCollisionPointRec(point, World.Player.Rect);

        public void UpdateAnimations()
        {
            foreach (Entity entity in World.Entities)
                entity.Sprite.Update();

            foreach (ParallaxLayer layer in World.Parallax.Layers)
                layer.Update();
        }

        [MemberNotNull(nameof(World))]
        private void Reset(LevelData level)
        {
            World = new World { IsEditorPreview = true };
            World.SetCols(level.Width);
            _resolved = new TileSpec[LevelData.Height, level.Width];
            _entities.Clear();
            _ordered.Clear();
        }

        private void SyncEntities(LevelData level)
        {
            HashSet<int> alive = [];
            foreach (EntityPlacement placement in level.Entities)
            {
                alive.Add(placement.Id);
                if (_entities.TryGetValue(placement.Id, out PreviewEntity? existing))
                {
                    if (existing.Placement == placement)
                        continue;

                    Remove(existing);
                }

                if (Create(placement) is { } created)
                    _entities[placement.Id] = created;
            }

            foreach (int id in _entities.Keys.Where(id => !alive.Contains(id)).ToList())
            {
                Remove(_entities[id]);
                _entities.Remove(id);
            }

            // Draw/hit-test order: by layer, then by order in the level
            _ordered.Clear();
            _ordered.AddRange(level.Entities
                .Select(p => _entities.GetValueOrDefault(p.Id))
                .OfType<PreviewEntity>()
                .OrderBy(p => p.Root.LayerDepth));
        }

        private PreviewEntity? Create(EntityPlacement placement)
        {
            int before = World.Entities.Count;
            Entity? root = LevelBuilder.Spawn(World, placement);
            if (root is null)
                return null;

            // Entities like firebars spawn children of their own
            List<Entity> spawned = [.. World.Entities.Skip(before)];
            foreach (Entity entity in spawned)
                entity.Activate();

            return new PreviewEntity(placement, root, spawned);
        }

        private void Remove(PreviewEntity preview)
        {
            foreach (Entity entity in preview.Spawned)
                World.RemoveEntity(entity);

            World.RemoveEntity(preview.Root);
        }
    }
}
