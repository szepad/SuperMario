namespace Mario2.Editor
{
    public class TileWindow(EditorScene editor) : SelectorWindow<Tile>(editor, "Tileset")
    {
        public override List<ISelectableObject<Tile>> Selectables { get; } = 
        [
            TileObjectDefs.Grass,
            TileObjectDefs.Block,
            TileObjectDefs.QuestionTile,
            TileObjectDefs.QuestionTile_Empty,
            TileObjectDefs.Brick,
            TileObjectDefs.Pipe,
            TileObjectDefs.GrassDecor,
            TileObjectDefs.Coin,
        ];

        public override void Update()
        {
            
        }
    }
}
