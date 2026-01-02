namespace Mario2
{
    public class ParallaxBackground(Color clearColor)
    {
        public Color ClearColor { get; } = clearColor;
        private readonly List<ParallaxLayer> _layers = [];
        
        public void AddLayer(ParallaxLayer layer)
        {
            _layers.Add(layer);
        }

        public void Update(Camera2D camera)
        {
            foreach (ParallaxLayer layer in _layers)
            {
                layer.Position = new Vector2(
                    camera.Target.X - camera.Target.X * layer.Speed,
                    camera.Target.Y
                    ) + layer.Offset - camera.Offset;
            }
        }

        public void Draw()
        {
            foreach (ParallaxLayer layer in _layers)
            {
                Raylib.DrawTextureEx(layer.Tex, layer.Position, 0f, Game.TextureScale, Color.White);
            }
        }
    }
}
