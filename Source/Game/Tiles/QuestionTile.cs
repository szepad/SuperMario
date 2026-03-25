namespace Mario2
{
    public class QuestionTile : ContainerTile
    {
        public QuestionTile() : base(true)
        {
            DrawSprite = QuestionSprite;
        }

        protected override void OnHitFirstFrame(Entity entity)
        {
            DrawSprite = entity.World.Background switch
            {
                WorldBackground.Underground => BlockSpriteUg,
                WorldBackground.Castle => BlockSpriteCastle,
                _ => BlockSpriteOw,
            };

            base.OnHitFirstFrame(entity);
        }
    }
}
