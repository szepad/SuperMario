namespace Mario2
{
    public class AnimatedTile(bool isCollidable) : Tile(isCollidable)
    {
        public static readonly Sprite GrassSpriteLeft = new("grass", new Animation(4, 16, 16, 0, 0.25f));
        public static readonly Sprite GrassSpriteMiddle = new("grass", new Animation(4, 16, 16, 16, 0.25f));
        public static readonly Sprite GrassSpriteRight = new("grass", new Animation(4, 16, 16, 32, 0.25f));

        public static readonly Sprite QuestionSprite = new("question", new Animation(3, 16, 16, 0, 0.15f));
        public static readonly Sprite BlockSpriteOw = new("question", new Animation(1, 16, 16, 16, 1f));
        public static readonly Sprite BlockSpriteUg = new("question", new Animation(1, 16, 16, 32, 1f));
        public static readonly Sprite BlockSpriteCastle = new("question", new Animation(1, 16, 16, 48, 1f));
        public static readonly Sprite BrickSpriteOw = new("question", new Animation(1, 16, 16, 64, 1f));
        public static readonly Sprite BrickSpriteUg = new("question", new Animation(1, 16, 16, 80, 1f));
        public static readonly Sprite BrickSpriteCastle = new("question", new Animation(1, 16, 16, 96, 1f));

        public static readonly Sprite CoinSprite = new("items", new Animation(3, 16, 16, 0, 0.15f));
        public static readonly Sprite HudCoinSprite = new("hud_coin", new Animation(3, 16, 8, 0, 0.15f));

        public Vector2 SpriteOffset;

        public Sprite? DrawSprite { private get; set; }

        public static void UpdateSpriteInstances()
        {
            GrassSpriteLeft.Update();
            GrassSpriteMiddle.Update();
            GrassSpriteRight.Update();

            QuestionSprite.Update();
            BlockSpriteOw.Update();
            BlockSpriteUg.Update();
            BlockSpriteCastle.Update();
            BrickSpriteOw.Update();
            BrickSpriteUg.Update();
            BrickSpriteCastle.Update();

            CoinSprite.Update();
            HudCoinSprite.Update();
        }

        public override void Update() { }

        public override void Draw(int col, int row, float opacity = 1)
        {
            DrawSprite?.Draw(new Vector2(col, row) * Size - SpriteOffset, opacity);
        }
    }
}
