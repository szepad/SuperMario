namespace Mario2
{
    public interface IEditProperty<T>
    {
        public EditPropertyGroup<T>? Group { get; set; }
        public void Render(T prop);
    }

    public abstract record EditProperty<TProp, TValue>(string Name, Func<TProp, TValue> Getter, 
        Action<TProp, TValue> Setter, TValue Default)
        : IEditProperty<TProp> where TValue : notnull
    {
        public EditPropertyGroup<TProp>? Group { get; set; }
        protected TValue CurrentValue = Default;

        protected abstract bool GuiFunction();

        public void Render(TProp property)
        {
            CurrentValue = Getter.Invoke(property);
            if (GuiFunction())
            {
                Setter.Invoke(property, CurrentValue);
                Group?.Editor.Dirty();
            }
        }
    }

    //public record InputIntEditProperty(string Name) : EditProperty(Name);

    //public record InputFloatEditProperty(string Name) : EditProperty(Name);
}
