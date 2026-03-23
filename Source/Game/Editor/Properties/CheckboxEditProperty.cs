namespace Mario2
{
    public record CheckboxEditProperty<TProp>(string Name, Func<TProp, bool> Getter, Action<TProp, bool> Setter)
        : EditProperty<TProp, bool>(Name, Getter, Setter, default)
    {
        protected override bool GuiFunction() => Gui.Checkbox(Name, ref CurrentValue);
    }
}
