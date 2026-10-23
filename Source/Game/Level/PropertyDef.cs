using System.Globalization;

namespace Mario2
{
    public enum PropertyKind
    {
        Bool,
        Int,
        Float,
        String,
        Path,
        Enum
    }

    /// <summary>
    /// Describes one editable property of an entity. Values travel as invariant strings
    /// (that's what levels store and what the editor edits) and are parsed on apply.
    /// </summary>
    public abstract class PropertyDef(string key, string label, PropertyKind kind)
    {
        public string Key { get; } = key;
        public string Label { get; } = label;
        public PropertyKind Kind { get; } = kind;

        /// <summary>Optional predicate over the placement's properties, used by the editor to grey a field out.</summary>
        public Func<IReadOnlyDictionary<string, string>, bool>? IsEnabled { get; init; }

        public abstract string DefaultText { get; }
        public virtual IReadOnlyList<string> Choices => [];

        /// <summary>Validates <paramref name="text"/> and returns its canonical form.</summary>
        public abstract bool TryNormalize(string text, out string normalized);

        public abstract void Apply(Entity entity, string text);

        public bool EnabledFor(IReadOnlyDictionary<string, string> props) => IsEnabled?.Invoke(props) ?? true;
    }

    public sealed class PropertyDef<TEntity, TValue>(string key, string label, PropertyKind kind,
        TValue defaultValue, Action<TEntity, TValue> setter, ValueCodec<TValue> codec)
        : PropertyDef(key, label, kind) where TEntity : Entity
    {
        public override string DefaultText => codec.Format(defaultValue);
        public override IReadOnlyList<string> Choices => codec.Choices;

        public override bool TryNormalize(string text, out string normalized)
        {
            if (codec.TryParse(text, out TValue value))
            {
                normalized = codec.Format(value);
                return true;
            }

            normalized = string.Empty;
            return false;
        }

        public override void Apply(Entity entity, string text)
        {
            if (entity is not TEntity typed)
                return;

            setter(typed, codec.TryParse(text, out TValue value) ? value : defaultValue);
        }
    }

    public sealed class ValueCodec<T>(TryParseFunc<T> tryParse, Func<T, string> format, IReadOnlyList<string>? choices = null)
    {
        public IReadOnlyList<string> Choices { get; } = choices ?? [];

        public bool TryParse(string text, out T value) => tryParse(text, out value);
        public string Format(T value) => format(value);
    }

    public delegate bool TryParseFunc<T>(string text, out T value);

    public static class ValueCodecs
    {
        public static readonly ValueCodec<bool> Bool = new(
            (string text, out bool value) => bool.TryParse(text, out value),
            value => value ? "true" : "false");

        public static readonly ValueCodec<int> Int = new(
            (string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            value => value.ToString(CultureInfo.InvariantCulture));

        public static readonly ValueCodec<float> Float = new(
            (string text, out float value) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && float.IsFinite(value),
            value => value.ToString("0.###", CultureInfo.InvariantCulture));

        public static readonly ValueCodec<string> String = new(
            (string text, out string value) =>
            {
                value = text;
                return true;
            },
            value => value);

        public static ValueCodec<TEnum> Enum<TEnum>() where TEnum : struct, Enum => new(
            (string text, out TEnum value) => System.Enum.TryParse(text, ignoreCase: true, out value)
                && System.Enum.IsDefined(value),
            value => value.ToString(),
            System.Enum.GetNames<TEnum>());
    }
}
