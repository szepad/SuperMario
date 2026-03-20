namespace Mario2
{
    public class PaletteShader
    {
        public Texture2D Palette { get; }
        public int RowCount { get; }

        public int CurrentRow { get; set; }

        private readonly Shader _paletteShader;
        private readonly int _paletteSamplerLoc;
        private readonly int _paletteOpacityLoc;
        private readonly int _paletteRowCountLoc;
        private readonly int _paletteRowLoc;

        public PaletteShader(Texture2D palette, int rowCount)
        {
            Palette = palette;
            RowCount = rowCount;

            Assets.TryGetShader("palette", out _paletteShader);
            _paletteSamplerLoc = Raylib.GetShaderLocation(_paletteShader, "palette");
            _paletteOpacityLoc = Raylib.GetShaderLocation(_paletteShader, "opacity");
            _paletteRowCountLoc = Raylib.GetShaderLocation(_paletteShader, "row_count");
            _paletteRowLoc = Raylib.GetShaderLocation(_paletteShader, "row");
        }

        public void Begin(float opacity = 1f)
        {
            Raylib.BeginShaderMode(_paletteShader);
            Raylib.SetShaderValueTexture(_paletteShader, _paletteSamplerLoc, Palette);
            Raylib.SetShaderValue(_paletteShader, _paletteOpacityLoc, opacity, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(_paletteShader, _paletteRowCountLoc, RowCount, ShaderUniformDataType.Int);
            Raylib.SetShaderValue(_paletteShader, _paletteRowLoc, CurrentRow, ShaderUniformDataType.Int);
        }

        public void End()
        {
            Raylib.EndShaderMode();
        }
    }
}
