namespace Mario2.Editor
{
    public abstract record EntityObjectBase(Texture2D Tileset, int PreviewCol, int PreviewRow,
        bool Unique = false)
    { }

    public sealed record EntityObject<T>(Texture2D Tileset, int PreviewCol, int PreviewRow,
        Action<T>? PlaceAction, bool Unique = false)
        : EntityObjectBase(Tileset, PreviewCol, PreviewRow, Unique), 
        ISelectableObject<Entity> where T : Entity
    {
        public EntityObject(int previewCol, int previewRow, bool unique = false)
            : this(Assets.GetTex("entity_selector"), previewCol, previewRow, null, unique)
        { }

        public EntityObject(int previewCol, int previewRow, Action<T> placeAction, bool unique = false)
            : this(Assets.GetTex("entity_selector"), previewCol, previewRow, placeAction, unique)
        { }

        public Entity OnPlace(World world, int col, int row)
        {
            T? entity = (T?)Activator.CreateInstance(typeof(T), world)
                ?? throw new NullReferenceException($"Reflection failed to create {typeof(T).FullName}");

            PlaceAction?.Invoke(entity);
            return entity;
        }
    }
}
