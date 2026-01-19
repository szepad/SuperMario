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
            DrawSprite = BlockSpriteOw;
            base.OnHitFirstFrame(entity);
        }
    }
}
