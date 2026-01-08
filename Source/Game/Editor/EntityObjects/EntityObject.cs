namespace Mario2.Editor
{
    public sealed record EntityObject<T>(Texture2D Tileset, int PreviewCol, int PreviewRow)
        : ISelectableObject<Entity> where T : Entity
    {
        public EntityObject(int previewCol, int previewRow)
            : this(Assets.GetTex("entity_selector"), previewCol, previewRow)
        { }

        public Entity OnPlace(World world, int col, int row)
        {
            T? entity = (T?)Activator.CreateInstance(typeof(T), world)
                ?? throw new NullReferenceException($"Reflection failed to create {typeof(T).FullName}");
            
            entity.SetFeetPos((col + 0.5f) * Tile.Size, (row + 1f) * Tile.Size);
            return entity;
        }
    }

    public static class EntityObjectDefs
    {
        public static readonly EntityObject<Player> Player = new(0, 0);
    }
}
