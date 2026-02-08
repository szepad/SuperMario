namespace Mario2
{
    public enum SingleCutsceneState { Always }

    public interface ICutscene
    {
        public bool IsFinished { get; }

        public void Start();
        public void Update();
        public void OnTileCollision(Tile tile, CollisionSide side);
    }

    public abstract class Cutscene<TEnum>(Player player) : ICutscene where TEnum : struct, Enum
    {
        public Player Player { get; } = player;
        public TEnum State { get; protected set; }
        public float StateTimer { get; protected set; }
        public bool IsFinished { get; protected set; }

        public abstract Action GetFunction(TEnum state);
        public abstract void Start();

        public void Update()
        {
            if (!IsFinished)
            {
                GetFunction(State).Invoke();
            }
        }

        public virtual void OnTileCollision(Tile tile, CollisionSide side) { }
    }
}
