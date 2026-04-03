namespace Mario2
{
    public class EffectPool
    {
        private const int Capacity = 50;
        private readonly Effect[] _effects = new Effect[Capacity];

        public EffectPool()
        {
            for (int i = 0; i < _effects.Length; i++)
            {
                _effects[i] = new Effect();
            }
        }

        public Effect Acquire()
        {
            foreach (Effect effect in _effects)
            {
                if (!effect.IsInUse)
                {
                    return effect;
                }
            }

            return _effects[0];
        }

        public void Update()
        {
            foreach (Effect effect in _effects)
            {
                if (effect.IsInUse)
                {
                    effect.Update();
                }
            }
        }

        public void Draw(bool isBackground)
        {
            foreach (Effect effect in _effects)
            {
                if (effect.IsInUse && effect.IsBackground == isBackground)
                {
                    effect.Draw();
                }
            }
        }
    }
}
