using System.Threading.Tasks;

namespace Mario2
{
    public enum WorldBackground
    {
        Sky,
        Underground,
        Castle,
        Water,
        Night
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

            return parallax;
        }
    }
}
