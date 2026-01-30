namespace Mario2.Editor
{
    public class EntityWindow(EditorScene editor)
        : SelectorWindow<Entity>(editor, "Entities")
    {
        public override List<ISelectableObject<Entity>> Selectables { get; } = 
        [
            EntityObjectDefs.Player,
            EntityObjectDefs.Goomba,
            EntityObjectDefs.Koopa,
            EntityObjectDefs.Pipe,
            EntityObjectDefs.WarpExit,
        ];

        public override void Update() { }
    }

    public static class EntityObjectDefs
    {
        public static readonly EntityObject<Player> Player = new(0, 0);
        public static readonly EntityObject<Goomba> Goomba = new(1, 0);
        public static readonly EntityObject<Koopa> Koopa = new(2, 0);
        public static readonly EntityObject<WarpPipe> Pipe = new(3, 0);
        public static readonly EntityObject<WarpExit> WarpExit = new(4, 0);
    }
}
