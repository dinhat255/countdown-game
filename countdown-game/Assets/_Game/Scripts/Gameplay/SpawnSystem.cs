using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    public sealed class SpawnConfiguration
    {
        public int CooldownBeats = 4;
        public int LivingEnemyCap = 8;
        public int MinimumPlayerDistance = 4;
        public int[][] PhaseWeights =
        {
            new[] { 70, 20, 10 },
            new[] { 30, 50, 20 },
            new[] { 20, 30, 50 }
        };
    }

    public sealed class SpawnSystem
    {
        private readonly GridState _grid;
        private readonly SpawnConfiguration _config;
        private readonly ISimulationEventSink _events;
        private int _nextActorId;
        private int _nextSpawnId;

        public int CooldownRemaining { get; private set; }

        public SpawnSystem(
            GridState grid,
            SpawnConfiguration config,
            int nextActorId,
            int nextSpawnId,
            ISimulationEventSink events = null)
        {
            _grid = grid;
            _config = config;
            _nextActorId = nextActorId;
            _nextSpawnId = nextSpawnId;
            _events = events ?? NullSimulationEventSink.Instance;
            CooldownRemaining = config.CooldownBeats;
        }

        public ActorState Tick(
            ActorState player,
            int phase,
            int pressure,
            int runSeed,
            int beatNumber,
            bool victory)
        {
            if (victory) return null;
            CooldownRemaining -= 1 + Math.Max(0, pressure);
            if (CooldownRemaining > 0) return null;

            CooldownRemaining = _config.CooldownBeats;
            var living = _grid.Actors.Count(a => a.IsAlive && a.Kind != ActorKind.Player);
            if (living >= _config.LivingEnemyCap) return null;

            var points = _grid.SpawnPoints
                .Where(_grid.IsWalkable)
                .Where(p => !_grid.IsActorOccupied(p))
                .Where(p => p.ManhattanDistance(player.Position) >= _config.MinimumPlayerDistance)
                .OrderBy(p => p)
                .ToArray();
            if (points.Length == 0) return null;

            var pointRandom = new SeededRandomContext(runSeed, beatNumber, 0, 0);
            var point = points[pointRandom.Index(points.Length)];
            var weights = _config.PhaseWeights[Math.Max(0, Math.Min(2, phase - 1))];
            var total = weights.Sum();
            var roll = new SeededRandomContext(runSeed, beatNumber, _nextActorId, 1).Index(total);
            var kind = roll < weights[0]
                ? ActorKind.Runner
                : roll < weights[0] + weights[1] ? ActorKind.Jumper : ActorKind.Thrower;
            var actor = new ActorState(_nextActorId++, _nextSpawnId++, kind, point);
            _grid.AddActor(actor);
            _events.EnemySpawned(actor);
            return actor;
        }
    }
}
