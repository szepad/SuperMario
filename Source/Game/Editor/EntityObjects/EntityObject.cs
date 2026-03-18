namespace Mario2.Editor
{
    public sealed record EntityObject<T>(Texture2D Tileset, int PreviewCol, int PreviewRow,
        Action<T>? PlaceAction)
        : ISelectableObject<Entity> where T : Entity
    {
        public EntityObject(int previewCol, int previewRow)
            : this(Assets.GetTex("entity_selector"), previewCol, previewRow, null)
        { }

        public EntityObject(int previewCol, int previewRow, Action<T> placeAction)
            : this(Assets.GetTex("entity_selector"), previewCol, previewRow, placeAction)
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
