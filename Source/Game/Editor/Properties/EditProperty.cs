namespace Mario2.Editor
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
        public Func<TProp, bool>? IsDisabled { get; init; }

        protected TValue CurrentValue = Default;

        protected abstract bool GuiFunction();

        public void Render(TProp property)
        {
            bool disabled = IsDisabled?.Invoke(property) ?? false;

            CurrentValue = Getter.Invoke(property);
            if (disabled)
                Gui.BeginDisabled();

            if (GuiFunction())
            {
                Setter.Invoke(property, CurrentValue);
                Group?.Editor.Dirty();
            }

            if (disabled)
                Gui.EndDisabled();
        }
    }
}
