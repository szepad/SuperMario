using System.Collections.Immutable;

namespace Mario2
{
    /// <summary>
    /// Converts an entity's <see cref="Entity.Position"/> into its feet, which is what level data
    /// stores. Only needed for the old level format, which stored positions.
    /// </summary>
    public static class EntityMetrics
    {
        private static readonly World _scratch = new() { IsEditorPreview = true };
        private static readonly Dictionary<(string, ImmutableSortedDictionary<string, string>), (Vector2 Size, Vector2 Offset)> _cache = [];

        /// <summary>
        /// The feet (bottom-center, texture pixels) of an entity whose position is <paramref name="positionPx"/>.
        /// </summary>
        public static (int X, int Y) FeetFromPosition(EntityDef def, ImmutableSortedDictionary<string, string> props, Vector2 positionPx)
        {
            (Vector2 size, Vector2 offset) = Measure(def, props);
            return FeetOf(positionPx, size, offset);
        }

        /// <summary>The feet of the player (as a level starts: small Mario) at <paramref name="positionPx"/>.</summary>
        public static (int X, int Y) PlayerFeetFromPosition(Vector2 positionPx)
        {
            Player player = _scratch.Player;
            return FeetOf(positionPx, player.Size, player.SizeOffset);
        }

        private static (int X, int Y) FeetOf(Vector2 positionPx, Vector2 size, Vector2 offset)
        {
            // Same as Entity.Feet, in texture pixels
            Vector2 feet = positionPx - (offset - new Vector2(size.X / 2f, size.Y)) / Game.TextureScale;
            return ((int)MathF.Round(feet.X), (int)MathF.Round(feet.Y));
        }

        /// <summary>Size and origin offset (scaled), which can depend on properties (platform size).</summary>
        private static (Vector2 Size, Vector2 Offset) Measure(EntityDef def, ImmutableSortedDictionary<string, string> props)
        {
            if (_cache.TryGetValue((def.Key, props), out var metrics))
                return metrics;

            int before = _scratch.Entities.Count;
            Entity entity = def.Create(_scratch, new EntityPlacement(0, def.Key, 0, 0, props));
            metrics = (entity.Size, entity.SizeOffset);

            // Creating may have spawned children (firebars); don't let them pile up
            foreach (Entity spawned in _scratch.Entities.Skip(before).ToList())
                _scratch.RemoveEntity(spawned);

            _cache[(def.Key, props)] = metrics;
            return metrics;
        }
    }
}
