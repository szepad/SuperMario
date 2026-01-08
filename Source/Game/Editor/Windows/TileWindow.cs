namespace Mario2.Editor
{
    public class TileWindow(EditorScene editor) : SelectorWindow<Tile>(editor, "Tileset")
    {
        public override List<ISelectableObject<Tile>> Selectables { get; } = 
        [
            TileObjectDefs.Grass,
            TileObjectDefs.Block,
            TileObjectDefs.QuestionTile,
            TileObjectDefs.QuestionBlock_Empty,
            TileObjectDefs.Brick,
            TileObjectDefs.Pipe,
            TileObjectDefs.GrassDecor
        ];

        public override void Update()
        {
            
        }
    }
}
