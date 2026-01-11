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
        ];

        public override void Update()
        {

        }
    }
}
