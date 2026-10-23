namespace Mario2.Editor
{
    /// <summary>
    /// A scratch world for drawing translucent "ghost" entities that follow the cursor
    /// (brush previews, pasted regions). Entities are only rebuilt when the requested set changes.
    /// </summary>
    public sealed class GhostWorld
    {
        private readonly World _world = new() { IsEditorPreview = true };
        private List<EntityPlacement> _placements = [];
        private readonly List<Entity> _entities = [];

        public void Set(IEnumerable<EntityPlacement> placements)
        {
            List<EntityPlacement> next = [.. placements];
            if (next.SequenceEqual(_placements, PlacementComparer.Instance))
                return;

            Clear();
            _placements = next;

            foreach (EntityPlacement placement in next)
            {
                int before = _world.Entities.Count;
                LevelBuilder.Spawn(_world, placement);
                foreach (Entity entity in _world.Entities.Skip(before))
                {
                    entity.Activate();
                    _entities.Add(entity);
                }
            }
        }

        public void Clear()
        {
            foreach (Entity entity in _entities)
                _world.RemoveEntity(entity);

            _entities.Clear();
            _placements = [];
        }

        public void Draw(float opacity)
        {
            foreach (Entity entity in _entities.OrderBy(e => e.LayerDepth))
            {
                float previous = entity.Sprite.Opacity;
                entity.Sprite.Opacity = opacity;
                entity.Draw();
                entity.Sprite.Opacity = previous;
            }
        }

        private sealed class PlacementComparer : IEqualityComparer<EntityPlacement>
        {
            public static readonly PlacementComparer Instance = new();

            public bool Equals(EntityPlacement? a, EntityPlacement? b)
                => a is not null && b is not null && a.Key == b.Key && a.X == b.X && a.Y == b.Y && a.Props == b.Props;

            public int GetHashCode(EntityPlacement p) => HashCode.Combine(p.Key, p.X, p.Y);
        }
    }
}
