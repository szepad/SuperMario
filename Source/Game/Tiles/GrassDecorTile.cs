namespace Mario2
{
    public class GrassDecorTileLeft : AnimatedTile
    {
        public GrassDecorTileLeft() : base(false)
        {
            DrawSprite = GrassSpriteLeft;
        }
    }

    public class GrassDecorTileMiddle : AnimatedTile
    {
        public GrassDecorTileMiddle() : base(false)
        {
            DrawSprite = GrassSpriteMiddle;
        }
    }

    public class GrassDecorTileRight : AnimatedTile
    {
        public GrassDecorTileRight() : base(false)
        {
            DrawSprite = GrassSpriteRight;
        }
    }
}
