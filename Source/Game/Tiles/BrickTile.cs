namespace Mario2
{
    public class BrickTile : ContainerTile
    {
        public BrickTile() : base(true)
        {
            Sprite.Play("brick_ow");
            OnlyHitOnce = false;
        }

        protected override void OnHitFirstFrame(Entity entity)
        {
            if (ItemInside != ItemType.None)
            {
                Sprite.Play("block_ow");
                OnlyHitOnce = true;
            }

            base.OnHitFirstFrame(entity);
        }
    }
}
