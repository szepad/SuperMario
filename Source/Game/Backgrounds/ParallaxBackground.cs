namespace Mario2
{
    public class ParallaxBackground(Color clearColor)
    {
        public Color ClearColor { get; } = clearColor;
        public List<ParallaxLayer> Layers { get; } = [];
        
        public ParallaxLayer AddLayer(ParallaxLayer layer)
        {
            Layers.Add(layer);
            return layer;
        }

        public void Update(Camera2D camera)
        {
            foreach (ParallaxLayer layer in Layers)
            {
                layer.Position = new Vector2(
                    -(camera.Target.X - camera.Offset.X) * layer.Speed,
                    -(camera.Target.Y - camera.Offset.Y)
                    );

                layer.Update();
            }
        }

        public void Draw()
        {
            foreach (ParallaxLayer layer in Layers)
            {
                layer.Draw();
            }
        }
    }
}
