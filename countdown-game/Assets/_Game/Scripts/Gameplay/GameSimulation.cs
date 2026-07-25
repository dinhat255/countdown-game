using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    [Serializable]
    public sealed class MovementTuning
    {
        public int DashDistance = 3;
        public int MovePressure = 1;
        public int DashPressure = 2;
        public int DashWcIncrease = 2;
    }

    public sealed class EnemyTuning
    {
        public int JumperDistance = 2;
        public int ShockwaveRadius = 1;
        public int ThrowerPickupRange = 2;
        public int ThrowerRange = 4;
        public int ThrowImpactRadius = 1;
    }

    public sealed class GameSimulation : IBeatController
    {
        private readonly int _runSeed;
        private readonly ISimulationEventSink _events;
        private readonly MovementTuning _movementTuning;
        private readonly MovementResolver _movement;
        private readonly RunnerBrain _runner = new RunnerBrain();
        private readonly JumperBrain _jumper;
        private readonly ThrowerBrain _thrower;
        private readonly SpawnSystem _spawns;

        public GridState Grid { get; }
        public RunState Run { get; }
        public ActorState Player { get; }
        public BeatPhase Phase => Run.Phase;
        public JumperBrain Jumper => _jumper;
        public ThrowerBrain Thrower => _thrower;
        public SpawnSystem Spawns => _spawns;

        public GameSimulation(
            GridState grid,
            ActorState player,
            RunState run,
            int runSeed,
            MovementTuning movementTuning = null,
            EnemyTuning enemyTuning = null,
            SpawnConfiguration spawnConfiguration = null,
            ISimulationEventSink events = null,
            int nextActorId = 1000,
            int nextSpawnId = 1000)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Run = run ?? throw new ArgumentNullException(nameof(run));
            _runSeed = runSeed;
            _events = events ?? NullSimulationEventSink.Instance;
            _movementTuning = movementTuning ?? new MovementTuning();
            enemyTuning = enemyTuning ?? new EnemyTuning();
            _movement = new MovementResolver(grid, _events);
            _jumper = new JumperBrain(enemyTuning.JumperDistance, enemyTuning.ShockwaveRadius);
            _thrower = new ThrowerBrain(
                enemyTuning.ThrowerPickupRange, enemyTuning.ThrowerRange, enemyTuning.ThrowImpactRadius);
            _spawns = new SpawnSystem(
                grid, spawnConfiguration ?? new SpawnConfiguration(), nextActorId, nextSpawnId, _events);
        }

        public void StartBeat()
        {
            if (Run.Victory)
            {
                SetPhase(BeatPhase.Victory);
                return;
            }
            Run.BeatNumber++;
            Run.MovementPressure = 0;
            foreach (var actor in Grid.Actors) actor.ResetForBeat();
            SetPhase(BeatPhase.Player);
        }

        public MovementResult TryPlayerMove(GridDirection direction)
        {
            if (Phase != BeatPhase.Player)
                return MovementResult.Rejected(
                    Player.Id, MovementFailureReason.ActorDead, Player.Position, Player.Position);
            var result = _movement.TryResolve(
                new MovementRequest(Player.Id, MovementKind.Move, direction));
            if (result.Succeeded)
            {
                Run.MovementPressure = _movementTuning.MovePressure;
                _events.PressureCreated(_movementTuning.MovePressure, MovementKind.Move);
            }
            return result;
        }

        public MovementResult TryPlayerDash()
        {
            if (Phase != BeatPhase.Player)
                return MovementResult.Rejected(
                    Player.Id, MovementFailureReason.ActorDead, Player.Position, Player.Position);
            var result = _movement.TryResolve(new MovementRequest(
                Player.Id, MovementKind.Dash, Player.Facing, _movementTuning.DashDistance));
            if (result.Succeeded)
            {
                var previous = Run.Wc;
                Run.ChangeWc(_movementTuning.DashWcIncrease);
                _events.WcChanged(previous, Run.Wc, "Dash");
                Run.MovementPressure = _movementTuning.DashPressure;
                _events.PressureCreated(_movementTuning.DashPressure, MovementKind.Dash);
            }
            return result;
        }

        public void EndPlayerPhase(bool freezeEnemyPhase = false)
        {
            if (Phase != BeatPhase.Player) throw new InvalidOperationException("Not in Player Phase.");

            SetPhase(BeatPhase.Enemy);
            if (freezeEnemyPhase)
            {
                foreach (var enemy in Grid.Actors.Where(IsEnemy))
                {
                    _jumper.SetPaused(enemy.Id, _events);
                    _thrower.SetPaused(enemy.Id, _events);
                }
            }
            else
            {
                var initiative = Grid.Actors
                    .Where(IsEnemy)
                    .OrderBy(a => a.SpawnId)
                    .Select(a => a.Id)
                    .ToArray();
                foreach (var id in initiative)
                {
                    var enemy = Grid.GetActor(id);
                    if (enemy == null || !enemy.IsAlive) continue;
                    var context = new EnemyContext(
                        Grid, _movement, Player, _runSeed, Run.BeatNumber, _events);
                    BrainFor(enemy.Kind).Act(enemy, context);
                }
            }

            SetPhase(BeatPhase.EndOfBeat);
            if (!Player.PlayerMovedThisBeat)
            {
                Run.StandingStreak++;
                var previous = Run.Wc;
                Run.ChangeWc(-1);
                _events.WcChanged(previous, Run.Wc, "NoMove");
            }
            else
            {
                Run.StandingStreak = 0;
            }

            if (Run.Victory)
            {
                SetPhase(BeatPhase.Victory);
                return;
            }

            _spawns.Tick(
                Player, Run.ProgressPhase, Run.MovementPressure, _runSeed, Run.BeatNumber, false);
            SetPhase(BeatPhase.NotStarted);
        }

        private IEnemyBrain BrainFor(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.Runner: return _runner;
                case ActorKind.Jumper: return _jumper;
                case ActorKind.Thrower: return _thrower;
                default: throw new InvalidOperationException("Player has no enemy brain.");
            }
        }

        private static bool IsEnemy(ActorState actor) =>
            actor.IsAlive && actor.Kind != ActorKind.Player;

        private void SetPhase(BeatPhase phase)
        {
            Run.Phase = phase;
            _events.PhaseChanged(phase);
        }
    }
}
