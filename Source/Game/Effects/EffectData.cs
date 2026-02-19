namespace Mario2;

public readonly record struct EffectData(string AnimName, float LifeTime, Vector2 SpriteOffset, float Gravity = 0f)
{
    public static readonly EffectData Skid = new(Effect.AnimSkid, 1f, new Vector2(5f).Scale());
    public static readonly EffectData Hit = new(Effect.AnimHit, 1f, new Vector2(14f).Scale());
    public static readonly EffectData BrickOw = new(Effect.AnimBrickOw, 5f, new Vector2(4f).Scale(), Entity.DefaultGravity * 1.25f);
    public static readonly EffectData BrickUg = new(Effect.AnimBrickUg, 5f, new Vector2(4f).Scale(), Entity.DefaultGravity * 1.25f);
    public static readonly EffectData BrickCastle = new(Effect.AnimBrickCastle, 5f, new Vector2(4f).Scale(), Entity.DefaultGravity * 1.25f);
}
