namespace Mario2
{
    public class GrassDecorTileLeft : AnimatedTile
    {
        public GrassDecorTileLeft() : base(false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 16, 0.25f));
        }
    }

    public class GrassDecorTileMiddle : AnimatedTile
    {
        public GrassDecorTileMiddle() : base(false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 32, 0.25f));
        }
    }

    public class GrassDecorTileRight : AnimatedTile
    {
        public GrassDecorTileRight() : base(false)
        {
            Sprite.Add("grass", new Animation(4, 16, 16, 48, 0.25f));
        }
    }
}
