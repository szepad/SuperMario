namespace Mario2
{
    public class ParallaxBackground(Color clearColor)
    {
        public Color ClearColor { get; } = clearColor;
        public List<ParallaxLayer> Layers { get; } = [];
        
        public void AddLayer(ParallaxLayer layer)
        {
            Layers.Add(layer);
        }

        public void Update(Camera2D camera)
        {
            foreach (ParallaxLayer layer in Layers)
            {
                //layer.Position = new Vector2(
                //    camera.Target.X - camera.Target.X * layer.Speed,
                //    camera.Target.Y
                //    ) - layer.Offset - new Vector2(camera.Offset.X, 0f);

                layer.Position = -(camera.Target - camera.Offset) * layer.Speed;
            }
        }

        public void Draw()
        {
            foreach (ParallaxLayer layer in Layers)
            {
                Raylib.DrawTextureEx(layer.Tex, layer.Position - layer.Offset, 0f, Game.TextureScale, Color.White);
            }
        }
    }
}
