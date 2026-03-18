using System.Reflection;

namespace Mario2.Editor
{
    public class EntityWindow : SelectorWindow<Entity>
    {
        public override List<ISelectableObject<Entity>> Selectables { get; }

        public EntityWindow(EditorScene editor) : base(editor, "Entities")
        {
            FieldInfo[] fields = [..typeof(EntityObjectDefs).GetFields()
                .Where(f => f.GetValue(null) is ISelectableObject<Entity>)
                .OrderBy(f => ((ISelectableObject<Entity>)f.GetValue(null)!).Index)];

            Selectables = [];
            foreach (FieldInfo field in fields)
            {
                if (field.GetValue(null) is ISelectableObject<Entity> obj)
                {
                    Selectables.Add(obj);
                }
            }
        }

        public override void Update() { }
    }

    public static class EntityObjectDefs
    {
        public static readonly EntityObject<Player> Player = new(0, 0);
        public static readonly EntityObject<Goomba> Goomba = new(1, 0);
        public static readonly EntityObject<Goomba> Goomba_Ug = new(2, 0, goomba =>
        {
            goomba.SetPalette(Palette.Underground);
        });

        public static readonly EntityObject<Goomba> Goomba_Castle = new(3, 0, goomba =>
        {
            goomba.SetPalette(Palette.Castle);
        });

        public static readonly EntityObject<Koopa> Koopa = new(4, 0);
        public static readonly EntityObject<WarpPipe> Pipe = new(5, 0);
        public static readonly EntityObject<WarpExit> WarpExit = new(6, 0);
        public static readonly EntityObject<ConstantPlatform> ConstantPlatform = new(7, 0);
    }
}
