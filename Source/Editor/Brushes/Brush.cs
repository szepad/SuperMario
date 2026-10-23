using System.Collections.Immutable;

namespace Mario2.Editor
{
    /// <summary>What the Brush and Rectangle tools paint with.</summary>
    public abstract class Brush
    {
        public abstract string Name { get; }

        /// <summary>Whether the brush paints single tiles, so it supports drag painting and rectangle fills.</summary>
        public virtual bool PaintsTiles => false;

        /// <summary>The palette item this brush came from, used to highlight it.</summary>
        public object? Source { get; init; }
    }

    public sealed class TileBrush(TileSpec spec, string name) : Brush
    {
        public TileSpec Spec { get; } = spec;
        public override string Name => name;
        public override bool PaintsTiles => true;

        public TileBrush With(TileSpec spec) => new(spec, name) { Source = Source };
    }

    public sealed class EntityBrush(EntityDef def, ImmutableSortedDictionary<string, string> props, string name) : Brush
    {
        public EntityDef Def { get; } = def;
        public ImmutableSortedDictionary<string, string> Props { get; } = props;
        public override string Name => name;

        public EntityBrush With(ImmutableSortedDictionary<string, string> props) => new(Def, props, name) { Source = Source };
    }

    public sealed class StampBrush(Stamp stamp) : Brush
    {
        public Stamp Stamp { get; } = stamp;
        public override string Name => Stamp.Name;
    }
}
