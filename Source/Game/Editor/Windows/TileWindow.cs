namespace Mario2.Editor
{
    public class TileWindow(EditorScene editor) : EditorWindow(editor, "Tileset")
    {
        private int _selectedIndex = 0;
        private static float _tileSizeScale = 1f;

        private const int TileSizePx = 16;

        public static List<TileObject> Selectables => _selectables;
        private static readonly List<TileObject> _selectables = 
        [
            TileObjectDefs.Grass,
            TileObjectDefs.Block,
            TileObjectDefs.QuestionTile,
            TileObjectDefs.QuestionBlock_Empty,
            TileObjectDefs.Brick,
            TileObjectDefs.Pipe,
            TileObjectDefs.GrassDecor
        ];
        
        public Tile GetSelected(World world, int col, int row)
            => _selectables[_selectedIndex].OnPlace(world, col, row);

        public override void Update()
        {

        }

        protected override void OnRender()
        {
            //Texture2D tileset = Assets.GetTex("tileset");
            int tileCount = _selectables.Count;

            Gui.SetNextItemWidth(64f);
            Gui.SliderFloat("Zoom", ref _tileSizeScale, 1f, 3f, "");

            int itemSize = (int)(32f * _tileSizeScale);
            int cols = (int)(Gui.GetWindowWidth() / (itemSize + 9));
            if (cols <= 0) cols = 1;
            int rows = (int)MathF.Ceiling(tileCount / (float)cols);
            int itemIndex = 0;

            if (Gui.BeginTable("tiles", cols, ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.NoPadInnerX))
            {
                for (int row = 0; row < rows; row++)
                {
                    Gui.TableNextRow();
                    for (int col = 0; col < cols; col++)
                    {
                        Gui.TableSetColumnIndex(col);

                        if (itemIndex >= tileCount)
                            break;

                        //DrawTile(itemIndex, itemSize, TileSizePx, tileset);
                        DrawTile(_selectables[itemIndex], itemIndex, itemSize);
                        itemIndex++;
                    }
                }

                Gui.EndTable();
            }
        }

        private void DrawTile(TileObject tileObj, int itemIndex, int previewSize)
        {
            if (_selectedIndex == itemIndex)
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0, 0, 1));
                DrawTileButton(tileObj, itemIndex, previewSize);
                Gui.PopStyleColor(3);
            }
            else
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 0));
                DrawTileButton(tileObj, itemIndex, previewSize);
                Gui.PopStyleColor(1);
            }
        }

        private void DrawTileButton(TileObject tileObj, int itemIndex, int previewSize)
        {
            //int cols = (tileset.Width / tileSizePx);
            //int col = index % cols;
            //int row = index / cols;

            Rectangle src = new(tileObj.PreviewCol * TileSizePx, tileObj.PreviewRow * TileSizePx, TileSizePx, TileSizePx);
            Vector2 uv0 = new(src.X / tileObj.Tileset.Width, src.Y / tileObj.Tileset.Height);
            Vector2 uv1 = uv0 + new Vector2(
                1f / (tileObj.Tileset.Width / src.Width), 
                1f / (tileObj.Tileset.Height / src.Height));

            if (Gui.ImageButton($"tile,{tileObj.PreviewCol},{tileObj.PreviewRow}",
                (nint)tileObj.Tileset.Id, new Vector2(previewSize), uv0, uv1))
            {
                _selectedIndex = itemIndex;
            }
        }
    }
}
