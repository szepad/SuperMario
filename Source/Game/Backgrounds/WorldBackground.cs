namespace Mario2
{
    public enum WorldBackground
    {
        Sky,
        Underground,
        Castle,
        Water,
        Night,
        Black,
        Waterfall
    }

    public static class WorldBackgroundExtension
    {
        public static ParallaxBackground GetParallax(WorldBackground background)
        {
            ParallaxBackground parallax = new(Color.Black);

            if (background == WorldBackground.Sky)
            {
                parallax = new ParallaxBackground(new Color(80, 136, 160));
                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_clouds"), new Vector2(0f, 16f).Scale(), 0.2f));

                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_hills"), new Vector2(0f, 0f).Scale(), 0.5f));
            }
            else if (background == WorldBackground.Underground)
            {
                parallax = new ParallaxBackground(Color.Black);
                ParallaxLayer bg = parallax.AddLayer(new ParallaxLayer(Assets.GetTex("bg_underground"),
                    new Vector2(0f, -16f).Scale(), 0.5f, 6, 1f));

                bg.AnimatedObjects.Add(new ParallaxAnimatedObject(
                    new Sprite("lantern", new Animation(8, 32, 32, 0, 0.15f)), 
                    [new Vector2(96f, 48f), new Vector2(352f, 48f)]));
            }
            else if (background == WorldBackground.Waterfall)
            {
                parallax = new ParallaxBackground(new Color(80, 136, 160));
                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_clouds"), new Vector2(0f, 16f).Scale(), 0.2f));

                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_rocks"), new Vector2(0f, -152f).Scale(), 0.5f, 2));

                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_waterfall"), new Vector2(0f, -152f).Scale(), 0.5f, 2)
                {
                    Palette = new PaletteShader(Assets.GetTex("palette_waterfall"), 8)
                });

                parallax.AddLayer(new WaterfallSteamParallaxLayer(new Vector2(0f, -152f).Scale(), 0.5f, 2));
            }
            else if (background == WorldBackground.Castle)
            {
                parallax = new ParallaxBackground(Color.Black);
                parallax.AddLayer(new ParallaxLayer(
                    Assets.GetTex("bg_castle"), new Vector2(0f, -16f).Scale(), 0.5f));
            }

            return parallax;
        }
    }
}
