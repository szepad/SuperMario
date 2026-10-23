namespace Mario2
{
    /// <summary>
    /// Turns <see cref="LevelData"/> into a playable <see cref="World"/>.
    /// </summary>
    public static class LevelBuilder
    {
        public static World Build(LevelData level, string sourcePath, List<string>? warnings = null)
        {
            LevelMeta meta = level.Meta;
            World world = new() { LoadedPath = sourcePath };

            world.SetCols(level.Width);
            world.SetBackground(meta.Background);
            world.SetMusic(meta.Music);
            world.IsPipeIntro = meta.PipeIntro;
            world.TimeLeft = meta.Time;
            world.Player.SetFeetPos(new Vector2(meta.SpawnX, meta.SpawnY) * Game.TextureScale);
            world.PlayerSpawnPosition = world.Player.Position;

            for (int row = 0; row < LevelData.Height; row++)
            {
                for (int col = 0; col < level.Width; col++)
                {
                    TileSpec spec = AutoTiles.ResolveAt(level, col, row);
                    if (!spec.IsAir)
                        world.SetTile(col, row, spec.CreateTile());
                }
            }

            foreach (EntityPlacement placement in level.Entities)
            {
                Spawn(world, placement, warnings);
            }

            return world;
        }

        /// <summary>
        /// Creates and spawns the entity of a placement. Returns null if its key is unknown.
        /// </summary>
        public static Entity? Spawn(World world, EntityPlacement placement, List<string>? warnings = null)
        {
            EntityDef? def = placement.Def;
            if (def is null)
            {
                warnings?.Add($"Unknown entity '{placement.Key}' was skipped.");
                return null;
            }

            return world.SpawnEntity(def.Create(world, placement));
        }
    }
}
