namespace Mario2
{
    public class QuestionTile : ContainerTile
    {
        public QuestionTile() : base(true)
        {
            Sprite.Play("?");
        }

        protected override void OnHitFirstFrame(Entity entity)
        {
            Sprite.Play("block_ow");
            base.OnHitFirstFrame(entity);
        }
    }
}
