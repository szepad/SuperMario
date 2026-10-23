using System.Collections.Immutable;

namespace Mario2
{
    public enum EntityCategory
    {
        Enemy,
        Platform,
        Object,
        LevelFlow
    }

    /// <summary>
    /// Everything needed to save, load and edit one kind of placeable entity: a stable key
    /// (what level files store), a factory and a typed property schema.
    /// </summary>
    public sealed class EntityDef(string key, string name, EntityCategory category, Type entityType,
        Func<World, Entity> factory)
    {
        public string Key { get; } = key;
        public string Name { get; } = name;
        public EntityCategory Category { get; } = category;
        public Type EntityType { get; } = entityType;

        /// <summary>Only one may exist per level; placing another one moves the existing one.</summary>
        public bool IsUnique { get; internal set; }

        public IReadOnlyList<PropertyDef> Properties => _properties;
        internal readonly List<PropertyDef> _properties = [];

        public PropertyDef? FindProperty(string key) => _properties.FirstOrDefault(p => p.Key == key);

        public string GetValue(EntityPlacement placement, PropertyDef property)
            => placement.Props.TryGetValue(property.Key, out string? value) ? value : property.DefaultText;

        /// <summary>
        /// Creates the entity standing at the placement's feet, with all of its properties applied.
        /// The entity is not spawned into the world.
        /// </summary>
        public Entity Create(World world, EntityPlacement placement)
        {
            Entity entity = factory(world);
            Vector2 feet = new Vector2(placement.X, placement.Y) * Game.TextureScale;

            // Some property setters use the position (a firebar builds its arm from it)...
            entity.SetFeetPos(feet);

            foreach (PropertyDef property in _properties)
            {
                property.Apply(entity, GetValue(placement, property));
            }

            // ...and some change the size (platforms), which moves the feet
            entity.SetFeetPos(feet);
            return entity;
        }

        public ImmutableSortedDictionary<string, string> DefaultProps()
            => _properties.ToImmutableSortedDictionary(p => p.Key, p => p.DefaultText);

        public override string ToString() => Key;
    }

    public sealed class EntityDefBuilder<T>(EntityDef def) where T : Entity
    {
        public EntityDef Def { get; } = def;

        public EntityDefBuilder<T> Unique()
        {
            Def.IsUnique = true;
            return this;
        }

        public EntityDefBuilder<T> Bool(string key, string label, bool defaultValue, Action<T, bool> setter,
            Func<IReadOnlyDictionary<string, string>, bool>? isEnabled = null)
            => Add(new PropertyDef<T, bool>(key, label, PropertyKind.Bool, defaultValue, setter, ValueCodecs.Bool) { IsEnabled = isEnabled });

        public EntityDefBuilder<T> Float(string key, string label, float defaultValue, Action<T, float> setter)
            => Add(new PropertyDef<T, float>(key, label, PropertyKind.Float, defaultValue, setter, ValueCodecs.Float));

        public EntityDefBuilder<T> String(string key, string label, string defaultValue, Action<T, string> setter,
            Func<IReadOnlyDictionary<string, string>, bool>? isEnabled = null)
            => Add(new PropertyDef<T, string>(key, label, PropertyKind.String, defaultValue, setter, ValueCodecs.String) { IsEnabled = isEnabled });

        public EntityDefBuilder<T> Path(string key, string label, string defaultValue, Action<T, string> setter,
            Func<IReadOnlyDictionary<string, string>, bool>? isEnabled = null)
            => Add(new PropertyDef<T, string>(key, label, PropertyKind.Path, defaultValue, setter, ValueCodecs.String) { IsEnabled = isEnabled });

        public EntityDefBuilder<T> Enum<TEnum>(string key, string label, TEnum defaultValue, Action<T, TEnum> setter)
            where TEnum : struct, Enum
            => Add(new PropertyDef<T, TEnum>(key, label, PropertyKind.Enum, defaultValue, setter, ValueCodecs.Enum<TEnum>()));

        private EntityDefBuilder<T> Add(PropertyDef property)
        {
            Def._properties.Add(property);
            return this;
        }
    }

    public static class EntityCatalog
    {
        private static readonly List<EntityDef> _defs = [];
        private static readonly Dictionary<string, EntityDef> _byKey = [];

        public static IReadOnlyList<EntityDef> All => _defs;

        static EntityCatalog()
        {
            // Enemies
            Add("goomba", "Goomba", EntityCategory.Enemy, w => new Goomba(w))
                .Enum("palette", "Palette", Palette.Overworld, (e, v) => e.SetPalette(v));

            Add("koopa", "Koopa Troopa", EntityCategory.Enemy, w => new Koopa(w))
                .Enum("palette", "Palette", Palette.Overworld, (e, v) => e.SetPalette(v));

            Add("red_koopa", "Red Koopa Troopa", EntityCategory.Enemy, w => new RedKoopa(w));

            Add("red_paratroopa", "Red Koopa Paratroopa", EntityCategory.Enemy, w => new RedFlyingKoopa(w))
                .Float("tile_offset", "Fly Distance (tiles)", 0f, (e, v) => e.TileOffset = v);

            Add("bowser", "Bowser", EntityCategory.Enemy, w => new Bowser(w));

            Add("firebar", "Firebar", EntityCategory.Enemy, w => new Firebar(w))
                .Enum("length", "Length", FirebarLength.Short, (e, v) => e.SetLength(v))
                .Bool("clockwise", "Clockwise", false, (e, v) => e.IsGoingRight = v);

            // Platforms
            Add("platform_constant", "Lift (Constant)", EntityCategory.Platform, w => new ConstantPlatform(w))
                .Enum("size", "Size", PlatformType.Long, (e, v) => e.Resize(v))
                .Enum("direction", "Direction", PlatformDirection.Up, (e, v) => e.Dir = v);

            Add("platform_pingpong", "Lift (Back & Forth)", EntityCategory.Platform, w => new PingPongPlatform(w))
                .Enum("size", "Size", PlatformType.Long, (e, v) => e.Resize(v))
                .Float("tile_offset", "Distance (tiles)", 0f, (e, v) => e.TileOffset = v)
                .Bool("horizontal", "Horizontal", false, (e, v) => e.IsHorizontal = v);

            // Objects
            Add("axe", "Axe", EntityCategory.Object, w => new Axe(w));
            Add("toad", "Toad", EntityCategory.Object, w => new Toad(w));

            // Level flow
            Add("warp_pipe", "Warp Pipe", EntityCategory.LevelFlow, w => new WarpPipe(w))
                .Path("dest", "Destination", string.Empty, (e, v) => e.Destination = v,
                    isEnabled: props => !props.TryGetValue("return_to_parent", out string? r) || r != "true")
                .String("tag", "Exit Tag", string.Empty, (e, v) => e.Tag = v)
                .Enum("dir", "Direction", PipeDirection.Down, (e, v) => e.Dir = v)
                .Bool("return_to_parent", "Return to Parent", false, (e, v) => e.ReturnToSubworld = v);

            Add("warp_exit", "Warp Exit", EntityCategory.LevelFlow, w => new WarpExit(w))
                .String("tag", "Tag", string.Empty, (e, v) => e.Tag = v);

            Add("midway", "Midway Checkpoint", EntityCategory.LevelFlow, w => new Midway(w)).Unique();
            Add("no_plant", "No Piranha Plant", EntityCategory.LevelFlow, w => new NoPlant(w));
            Add("flagpole", "Flagpole", EntityCategory.LevelFlow, w => new Flagpole(w));
            Add("castle_entrance", "Castle Entrance", EntityCategory.LevelFlow, w => new CastleEntrance(w));
        }

        public static EntityDef? Get(string key) => _byKey.GetValueOrDefault(key);

        private static EntityDefBuilder<T> Add<T>(string key, string name, EntityCategory category, Func<World, T> factory)
            where T : Entity
        {
            EntityDef def = new(key, name, category, typeof(T), factory);
            _defs.Add(def);
            _byKey.Add(key, def);
            return new EntityDefBuilder<T>(def);
        }
    }
}
