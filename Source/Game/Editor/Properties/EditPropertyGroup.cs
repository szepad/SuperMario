namespace Mario2.Editor
{
    public interface IEditPropertyGroup
    {
        public void Add(dynamic property);
        public void Render(dynamic propertyHolder);
    }

    public class EditPropertyGroup<T>(string name, EditorScene editor) 
        : IEditPropertyGroup
    {
        public string Name { get; } = name;
        public EditorScene Editor { get; } = editor;

        private readonly List<IEditProperty<T>> _properties = [];

        public void Add(dynamic property)
        {
            if (property is not IEditProperty<T>)
                return;

            property.Group = this;
            _properties.Add(property);
        }

        public void Render(dynamic propertyHolder)
        {
            if (propertyHolder is not T)
                return;

            if (Name != string.Empty)
                Gui.SeparatorText(Name);

            foreach (IEditProperty<T> prop in _properties)
            {
                prop.Render(propertyHolder);
            }
        }
    }
}
