namespace Mario2
{
    public class TimerCountdownObject(ICutscene cutscene, Action onEnd)
    {
        private readonly ICutscene _cutscene = cutscene;
        private readonly Action _onEnd = onEnd;

        private const float TimerSoundInterval = 0.0355f;
        private const float TimerCountdownInterval = 0.015f;
        private const float TimerCountdownIntervalFaster = 0.005f;
        private float _timerCountdownTimer;
        private float _timerSoundIntervalTimer;

        public void Update()
        {
            _timerSoundIntervalTimer += Time.DeltaTime;
            _timerCountdownTimer += Time.DeltaTime;

            if (_timerCountdownTimer >= (SoundManager.IsCurrentMusicFinished()
                ? TimerCountdownIntervalFaster : TimerCountdownInterval))
            {
                _timerCountdownTimer = 0f;
                if (_cutscene.Player.World.TimeLeft > 0)
                {
                    _cutscene.Player.World.TimeLeft--;
                    SessionManager.GetScoreWithoutText(ScoreRewards.Time);
                }
            }

            if (_timerSoundIntervalTimer >= TimerSoundInterval)
            {
                _timerSoundIntervalTimer = 0f;

                if (_cutscene.Player.World.TimeLeft > 0)
                {
                    SoundManager.PlaySound(Sounds.Score);
                }
                else
                {
                    SoundManager.PlaySound(Sounds.ScoreEnd);
                    _onEnd.Invoke();
                }
            }
        }
    }
}
