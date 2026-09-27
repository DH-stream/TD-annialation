namespace TDAnnihilation
{
    public enum TDRunMode
    {
        Normal,
        Endless
    }

    public enum TDGamePhase
    {
        MainMenu,
        Build,
        Wave,
        Victory,
        Defeat
    }

    public sealed class TDGameFlow
    {
        private readonly int maxWaves;

        public TDGameFlow(int maxWaves)
        {
            this.maxWaves = maxWaves;
            Phase = TDGamePhase.MainMenu;
        }

        public TDGamePhase Phase { get; private set; }
        public int Wave { get; private set; }
        public TDRunMode RunMode { get; private set; }

        public void SelectSoloStages(TDRunMode mode = TDRunMode.Normal)
        {
            Wave = 0;
            RunMode = mode;
            Phase = TDGamePhase.Build;
        }

        public void ResumeBuildPhase(int completedWaves, TDRunMode mode)
        {
            Wave = completedWaves < 0 ? 0 : completedWaves;
            RunMode = mode;
            Phase = TDGamePhase.Build;
        }

        public void StartWave()
        {
            if (Phase != TDGamePhase.Build) return;
            Wave++;
            Phase = TDGamePhase.Wave;
        }

        public bool CompleteWave()
        {
            if (Phase != TDGamePhase.Wave) return false;
            Phase = RunMode == TDRunMode.Normal && Wave >= maxWaves
                ? TDGamePhase.Victory
                : TDGamePhase.Build;
            return true;
        }

        public void Lose()
        {
            if (Phase == TDGamePhase.Wave || Phase == TDGamePhase.Build)
                Phase = TDGamePhase.Defeat;
        }

        public void ReturnToMenu()
        {
            Wave = 0;
            RunMode = TDRunMode.Normal;
            Phase = TDGamePhase.MainMenu;
        }
    }
}
