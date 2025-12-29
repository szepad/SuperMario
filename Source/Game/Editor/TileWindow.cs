namespace Mario2.Editor
{
    public class TileWindow(EditorScene editor) : EditorWindow(editor, "Tileset")
    {
        public int SelectedTile { get; private set; } = 0;
        private static float _tileSizeScale = 1f;

        public override void Update()
        {

        }

        protected override void OnRender()
        {
            Texture2D tileset = Assets.GetTex("tileset");
            const int TileCount = 120;
            const int TileSizePx = 16;

            Gui.SetNextItemWidth(64f);
            Gui.SliderFloat("Zoom", ref _tileSizeScale, 1f, 3f, "");

            int itemSize = (int)(32f * _tileSizeScale);
            int cols = (int)(Gui.GetWindowWidth() / (itemSize + 9));
            if (cols <= 0) cols = 1;
            int rows = (int)MathF.Ceiling(TileCount / (float)cols);
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

                        if (itemIndex >= TileCount)
                            break;

                        DrawTile(itemIndex, itemSize, TileSizePx, tileset);
                        itemIndex++;
                    }
                }

                Gui.EndTable();
            }
        }

        private void DrawTile(int itemIndex, int itemSize, int tileSizePx, Texture2D tileset)
        {
            if (SelectedTile == itemIndex)
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0, 0, 1));
                DrawTileButton(itemIndex, itemSize, tileSizePx, tileset);
                Gui.PopStyleColor(3);
            }
            else
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 0));
                DrawTileButton(itemIndex, itemSize, tileSizePx, tileset);
                Gui.PopStyleColor(1);
            }
        }

        private void DrawTileButton(int index, int displaySize, int tileSizePx, Texture2D tileset)
        {
            int cols = (tileset.Width / tileSizePx);
            int col = index % cols;
            int row = index / cols;

            Rectangle src = new(col * tileSizePx, row * tileSizePx, tileSizePx, tileSizePx);
            Vector2 uv0 = new(src.X / tileset.Width, src.Y / tileset.Height);
            Vector2 uv1 = uv0 + new Vector2(1f / (tileset.Width / src.Width), 1f / (tileset.Height / src.Height));

            if (Gui.ImageButton($"tile,{col},{row}", (nint)tileset.Id, new Vector2(displaySize), uv0, uv1))
            {
                SelectedTile = index;
            }
        }
    }
}
