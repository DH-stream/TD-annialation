namespace TDAnnihilation
{
    public readonly struct TDEnemyArchetype
    {
        public static readonly TDEnemyArchetype Raider = new TDEnemyArchetype(0.72f, 1f, false);
        public static readonly TDEnemyArchetype Elite = new TDEnemyArchetype(1.18f, 3.2f, true);

        private TDEnemyArchetype(float scale, float healthMultiplier, bool isElite)
        {
            Scale = scale;
            HealthMultiplier = healthMultiplier;
            IsElite = isElite;
        }

        public float Scale { get; }
        public float HealthMultiplier { get; }
        public bool IsElite { get; }
    }
}
