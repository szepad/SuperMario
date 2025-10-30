namespace Mario2
{
    public abstract class Scene
    {
        private bool _isLoaded;

        public void Load()
        {
            if (_isLoaded) return;
            _isLoaded = true;

            OnLoad();
        }

        public void Unload()
        {
            if (!_isLoaded) return;
            _isLoaded = false;

            OnUnload();
        }

        public abstract void Update();
        
        public virtual void PreDraw() { }
        public abstract void Draw();

        protected abstract void OnLoad();
        protected abstract void OnUnload();
    }
}
