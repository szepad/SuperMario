namespace Mario2.Editor
{
    public record ComboEditProperty<TProp, TEnum>(string Name, Func<TProp, TEnum> Getter, Action<TProp, TEnum> Setter)
        : EditProperty<TProp, TEnum>(Name, Getter, Setter, default) where TEnum : struct, Enum
    {
        private readonly string[] _choices = Enum.GetNames<TEnum>();
        private readonly TEnum[] _values = Enum.GetValues<TEnum>();
        private int _currChoice;

        public ComboEditProperty(string name, Func<TProp, TEnum> getter, Action<TProp, TEnum> setter, int firstNthValue)
            : this(name, getter, setter)
        {
            _choices = _choices[0..firstNthValue];
            _values = _values[0..firstNthValue];
        }

        protected override bool GuiFunction()
        {
            _currChoice = _values.IndexOf(CurrentValue);
            if (Gui.Combo(Name, ref _currChoice, _choices, _choices.Length))
            {
                CurrentValue = _values[_currChoice];
                return true;
            }

            return false;
        }
    }
}
