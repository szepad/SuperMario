namespace Mario2
{
    public class CastleEntrance : Entity
    {
        public CastleEntrance(World world) : base(world)
        {
            Size = HitboxSize = new Vector2(16f).Scale();
            IsCollidable = false;
            IsHittable = false;
            Gravity = 0f;
        }

        protected override void OnSpawn()
        {
            
        }

        public override void Draw()
        {
            if (Game.Instance.ActiveScene is EditorScene editor && editor.SubScene is null)
            {
                Raylib.DrawTextureEx(Assets.GetTex("editor_exit"), Position, 0f, Game.TextureScale, Color.White);
            }

            base.Draw();
        }
    }
}
