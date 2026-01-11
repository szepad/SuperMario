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
            
            return entity;
        }
    }

    public static class EntityObjectDefs
    {
        public static readonly EntityObject<Player> Player = new(0, 0);
        public static readonly EntityObject<Goomba> Goomba = new(1, 0);
        public static readonly EntityObject<Koopa> Koopa = new(2, 0);
    }
}
