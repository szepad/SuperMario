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

                layer.UpdateObjects();
            }
        }

        public void Draw()
        {
            foreach (ParallaxLayer layer in Layers)
            {
                for (int i = 0; i < layer.RepeatCount; i++)
                {
                    Vector2 pos = layer.Position - layer.Offset;
                    Raylib.DrawTextureEx(layer.Tex, new Vector2(pos.X + layer.Tex.Width * Game.TextureScale * i,
                        pos.Y), 0f, Game.TextureScale, Color.White.Alpha(layer.Opacity));

                    foreach (ParallaxAnimatedObject obj in layer.AnimatedObjects)
                    {
                        foreach (Vector2 relPos in obj.RelativePositions)
                        {
                            obj.Sprite.Draw(pos + relPos.Scale(), layer.Opacity);
                        }
                    }
                }
            }
        }
    }
}
