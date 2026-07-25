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
        }

        public void ResetForBeat() => SelfMovedThisBeat = false;
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
        public BeatPhase Phase { get; internal set; } = BeatPhase.NotStarted;
        public bool Victory => Wc <= 0;

        public RunState(int initialWc)
        {
            InitialWc = initialWc;
            Wc = initialWc;
            LowestWcReached = initialWc;
            HighestWcReached = initialWc;
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
