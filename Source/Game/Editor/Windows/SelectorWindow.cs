namespace Mario2.Editor
{
    public abstract class SelectorWindow<T>(EditorScene editor, string name)
        : EditorWindow(editor, name)
    {
        public int SelectedIndex { get; private set; } = 0;

        public abstract List<ISelectableObject<T>> Selectables { get; }
        
        private float _tileSizeScale = 1f;
        private const int TileSizePx = 16;

        public ISelectableObject<T> Selected => Selectables[SelectedIndex];

        public T PlaceSelected(World world, int col, int row)
            => Selectables[SelectedIndex].OnPlace(world, col, row);

        protected override void OnRender()
        {
            int tileCount = Selectables.Count;

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

                        DrawTile(Selectables[itemIndex], itemIndex, itemSize);
                        itemIndex++;
                    }
                }

                Gui.EndTable();
            }
        }

        private void DrawTile(ISelectableObject<T> obj, int itemIndex, int previewSize)
        {
            if (SelectedIndex == itemIndex && Editor.ActiveSelector == this)
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1, 0, 0, 1));
                Gui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0, 0, 1));
                DrawTileButton(obj, itemIndex, previewSize);
                Gui.PopStyleColor(3);
            }
            else
            {
                Gui.PushStyleColor(ImGuiCol.Button, new Vector4(1, 0, 0, 0));
                DrawTileButton(obj, itemIndex, previewSize);
                Gui.PopStyleColor(1);
            }
        }

        private void DrawTileButton(ISelectableObject<T> obj, int itemIndex, int previewSize)
        {
            Rectangle src = new(obj.PreviewCol * TileSizePx, obj.PreviewRow * TileSizePx, TileSizePx, TileSizePx);
            Vector2 uv0 = new(src.X / obj.Tileset.Width, src.Y / obj.Tileset.Height);
            Vector2 uv1 = uv0 + new Vector2(
                1f / (obj.Tileset.Width / src.Width),
                1f / (obj.Tileset.Height / src.Height));

            if (Gui.ImageButton($"tile,{obj.PreviewCol},{obj.PreviewRow}",
                (nint)obj.Tileset.Id, new Vector2(previewSize), uv0, uv1))
            {
                SelectedIndex = itemIndex;
                Editor.SetActiveSelector(this);
            }
        }
    }
}
