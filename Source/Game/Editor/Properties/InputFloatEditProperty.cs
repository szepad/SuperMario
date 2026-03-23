namespace Mario2.Editor
{
    public record InputFloatEditProperty<TProp>(string Name, Func<TProp, float> Getter,
        Action<TProp, float> Setter, float Step = 0.5f, float StepFast = 0.5f, string Format = "%.1f")
        : EditProperty<TProp, float>(Name, Getter, Setter, default)
    {
        protected override bool GuiFunction()
            => Gui.InputFloat(Name, ref CurrentValue, Step, StepFast, Format);
    }

    public record InputIntEditProperty<TProp>(string Name, Func<TProp, int> Getter,
        Action<TProp, int> Setter, int Step = 1, int StepFast = 1)
        : EditProperty<TProp, int>(Name, Getter, Setter, default)
    {
        protected override bool GuiFunction()
            => Gui.InputInt(Name, ref CurrentValue, Step, StepFast);
    }
}
