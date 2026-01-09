namespace Mario2
{
    public class GrassDecorTileLeft : AnimatedTile
    {
        public GrassDecorTileLeft() : base("grass", false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 0, 0.25f));
        }
    }

    public class GrassDecorTileMiddle : AnimatedTile
    {
        public GrassDecorTileMiddle() : base("grass", false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 16, 0.25f));
        }
    }

    public class GrassDecorTileRight : AnimatedTile
    {
        public GrassDecorTileRight() : base("grass", false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 32, 0.25f));
        }
    }
}
