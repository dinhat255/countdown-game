using System;

namespace CountdownGame.Core
{
    [Serializable]
    public sealed class ActorState
    {
        public int Id { get; }
        public int SpawnId { get; }
        public ActorKind Kind { get; }
        public GridCoord Position { get; internal set; }
        public GridDirection Facing { get; internal set; }
        public bool IsAlive { get; set; } = true;
        public int MaxHealth { get; private set; }
        public int Health { get; private set; }
        public bool SelfMovedThisBeat { get; internal set; }
        public bool PlayerMovedThisBeat => Kind == ActorKind.Player && SelfMovedThisBeat;

        public ActorState(
            int id,
            int spawnId,
            ActorKind kind,
            GridCoord position,
            GridDirection facing = GridDirection.Right)
        {
            Id = id;
            SpawnId = spawnId;
            Kind = kind;
            Position = position;
            Facing = facing;
            MaxHealth = kind == ActorKind.Player ? 0 : DefaultHealth(kind);
            Health = MaxHealth;
        }

        public void ResetForBeat() => SelfMovedThisBeat = false;

        public void SetMaximumHealth(int maximum, bool refill = true)
        {
            MaxHealth = Math.Max(0, maximum);
            Health = refill ? MaxHealth : Math.Min(Health, MaxHealth);
            IsAlive = Kind == ActorKind.Player || Health > 0;
        }

        public int ApplyDamage(int amount)
        {
            if (Kind == ActorKind.Player || !IsAlive || amount <= 0) return 0;
            var applied = Math.Min(Health, amount);
            Health -= applied;
            if (Health <= 0) IsAlive = false;
            return applied;
        }

        private static int DefaultHealth(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.Runner: return 3;
                case ActorKind.Jumper: return 4;
                case ActorKind.Thrower: return 5;
                default: return 0;
            }
        }
    }

    [Serializable]
    public sealed class RunState
    {
        public int BeatNumber { get; internal set; }
        public int Wc { get; private set; }
        public int InitialWc { get; }
        public int LowestWcReached { get; private set; }
        public int HighestWcReached { get; private set; }
        public int StandingStreak { get; internal set; }
        public int MovementPressure { get; internal set; }
        public int MaxMana { get; }
        public int CurrentMana { get; private set; }
        public BeatPhase Phase { get; internal set; } = BeatPhase.NotStarted;
        public bool Victory => Wc <= 0;

        public RunState(int initialWc, int initialMana = 3, int maxMana = 6)
        {
            if (maxMana < 0) throw new ArgumentOutOfRangeException(nameof(maxMana));
            InitialWc = initialWc;
            Wc = initialWc;
            LowestWcReached = initialWc;
            HighestWcReached = initialWc;
            MaxMana = maxMana;
            CurrentMana = Math.Max(0, Math.Min(maxMana, initialMana));
        }

        public bool TrySpendMana(int amount)
        {
            if (amount < 0 || CurrentMana < amount) return false;
            CurrentMana -= amount;
            return true;
        }

        public int RestoreMana(int amount)
        {
            var previous = CurrentMana;
            CurrentMana = Math.Min(MaxMana, CurrentMana + Math.Max(0, amount));
            return CurrentMana - previous;
        }

        public void ChangeWc(int delta)
        {
            Wc += delta;
            LowestWcReached = Math.Min(LowestWcReached, Wc);
            HighestWcReached = Math.Max(HighestWcReached, Wc);
        }

        public int ProgressPhase
        {
            get
            {
                if (InitialWc <= 0) return 3;
                var progress = (InitialWc - LowestWcReached) / (double)InitialWc;
                if (progress >= 2d / 3d) return 3;
                return progress >= 1d / 3d ? 2 : 1;
            }
        }
    }
}
