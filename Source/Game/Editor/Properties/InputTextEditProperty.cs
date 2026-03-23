namespace Mario2
{
    public record InputTextEditProperty<TProp>(string Name, Func<TProp, string> Getter,
        Action<TProp, string> Setter, uint MaxLength = 30)
        : EditProperty<TProp, string>(Name, Getter, Setter, string.Empty)
    {
        protected override bool GuiFunction()
            => Gui.InputText(Name, ref CurrentValue, MaxLength);
    }
}
