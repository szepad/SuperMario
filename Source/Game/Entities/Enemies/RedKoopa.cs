namespace Mario2
{
    public class RedKoopa : Koopa
    {
        public RedKoopa(World world) 
            : base(world)
        {
            Sprite.SetSheet("koopa_red");
            NormalAi = new EdgeDetectionAi(this, Goomba.WalkSpeed);
        }

        public override void SetPalette(Palette _)
        {

        }
    }
}
