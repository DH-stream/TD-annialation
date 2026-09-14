namespace TDAnnihilation
{
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

        public void SelectSoloStages()
        {
            Wave = 0;
            Phase = TDGamePhase.Build;
        }

        public void StartWave()
        {
            if (Phase != TDGamePhase.Build) return;
            Wave++;
            Phase = TDGamePhase.Wave;
        }

        public void CompleteWave()
        {
            if (Phase != TDGamePhase.Wave) return;
            Phase = Wave >= maxWaves ? TDGamePhase.Victory : TDGamePhase.Build;
        }

        public void Lose() => Phase = TDGamePhase.Defeat;

        public void ReturnToMenu()
        {
            Wave = 0;
            Phase = TDGamePhase.MainMenu;
        }
    }
}
