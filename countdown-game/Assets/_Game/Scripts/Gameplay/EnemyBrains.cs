using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    [Serializable]
    public sealed class JumperTelegraphLock
    {
        public GridCoord Origin;
        public GridCoord Landing;
        public int PreparedBeat;
    }

    [Serializable]
    public sealed class ThrowTelegraphLock
    {
        public GridCoord ThrowerOrigin;
        public int TargetId;
        public GridCoord TargetOrigin;
        public GridCoord Landing;
        public GridCoord[] Trajectory;
        public GridCoord[] ImpactArea;
        public int PreparedBeat;
    }

    public sealed class EnemyContext
    {
        public GridState Grid { get; }
        public IMovementResolver Movement { get; }
        public ActorState Player { get; }
        public int RunSeed { get; }
        public int BeatNumber { get; }
        public ISimulationEventSink Events { get; }
        public int DecisionOrdinal { get; set; }

        public EnemyContext(
            GridState grid,
            IMovementResolver movement,
            ActorState player,
            int runSeed,
            int beatNumber,
            ISimulationEventSink events)
        {
            Grid = grid;
            Movement = movement;
            Player = player;
            RunSeed = runSeed;
            BeatNumber = beatNumber;
            Events = events ?? NullSimulationEventSink.Instance;
        }

        public SeededRandomContext RandomFor(int actorId) =>
            new SeededRandomContext(RunSeed, BeatNumber, actorId, DecisionOrdinal++);

        public void HitPlayer(int sourceId, string cause)
        {
            Events.Hit(sourceId, Player.Id, cause);
        }
    }

    public sealed class RunnerBrain : IEnemyBrain
    {
        public EnemyDecision Act(ActorState enemy, EnemyContext context)
        {
            if (enemy.Position.ManhattanDistance(context.Player.Position) == 1)
            {
                context.HitPlayer(enemy.Id, "RunnerAttack");
                return Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Attack));
            }

            var next = GridPathfinding.NextStep(
                context.Grid, enemy.Position, context.Player.Position, enemy.Id, context.RandomFor(enemy.Id));
            if (next.HasValue)
            {
                var direction = DirectionBetween(enemy.Position, next.Value);
                var result = context.Movement.TryResolve(
                    new MovementRequest(enemy.Id, MovementKind.Move, direction));
                if (result.Succeeded && enemy.Position.ManhattanDistance(context.Player.Position) == 1)
                {
                    Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Move, enemy.Position));
                    context.HitPlayer(enemy.Id, "RunnerAttack");
                    return Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Attack));
                }
                if (result.Succeeded)
                    return Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Move, enemy.Position));
            }
            return Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Hold));
        }

        internal static GridDirection DirectionBetween(GridCoord from, GridCoord to)
        {
            if (to.X > from.X) return GridDirection.Right;
            if (to.X < from.X) return GridDirection.Left;
            return to.Y > from.Y ? GridDirection.Up : GridDirection.Down;
        }

        internal static EnemyDecision Emit(EnemyContext context, EnemyDecision decision)
        {
            context.Events.EnemyDecisionResolved(decision);
            return decision;
        }
    }

    public sealed class JumperBrain : IEnemyBrain
    {
        private readonly int _jumpDistance;
        private readonly int _shockwaveRadius;
        private readonly Dictionary<int, JumperTelegraphLock> _locks =
            new Dictionary<int, JumperTelegraphLock>();

        public JumperBrain(int jumpDistance = 2, int shockwaveRadius = 1)
        {
            _jumpDistance = jumpDistance;
            _shockwaveRadius = shockwaveRadius;
        }

        public JumperTelegraphLock GetLock(int enemyId) =>
            _locks.TryGetValue(enemyId, out var value) ? value : null;

        public EnemyDecision Act(ActorState enemy, EnemyContext context)
        {
            if (_locks.TryGetValue(enemy.Id, out var locked))
            {
                _locks.Remove(enemy.Id);
                context.Events.TelegraphChanged(enemy.Id, "Jump", false, false);
                if (enemy.Position != locked.Origin || !context.Grid.IsWalkable(locked.Landing) ||
                    context.Grid.IsActorOccupied(locked.Landing, enemy.Id))
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.CancelJump, locked.Landing));

                var direction = RunnerBrain.DirectionBetween(enemy.Position, locked.Landing);
                var result = context.Movement.TryResolve(new MovementRequest(
                    enemy.Id, MovementKind.Jump, direction, _jumpDistance, locked.Landing));
                if (!result.Succeeded)
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.CancelJump, locked.Landing));

                if (locked.Landing.ManhattanDistance(context.Player.Position) <= _shockwaveRadius)
                    context.HitPlayer(enemy.Id, "JumperShockwave");
                return RunnerBrain.Emit(context,
                    new EnemyDecision(enemy.Id, EnemyDecisionKind.ResolveJump, locked.Landing));
            }

            var candidates = GridDirections.Cardinal
                .Select(direction => enemy.Position.Step(direction, _jumpDistance))
                .Where(cell => context.Grid.IsWalkable(cell) &&
                               !context.Grid.IsActorOccupied(cell, enemy.Id))
                .OrderBy(cell => cell.ManhattanDistance(context.Player.Position))
                .ThenBy(cell => cell)
                .ToArray();

            if (candidates.Length > 0)
            {
                var bestDistance = candidates[0].ManhattanDistance(context.Player.Position);
                var equivalent = candidates
                    .Where(c => c.ManhattanDistance(context.Player.Position) == bestDistance)
                    .ToArray();
                var landing = equivalent[context.RandomFor(enemy.Id).Index(equivalent.Length)];
                _locks[enemy.Id] = new JumperTelegraphLock
                {
                    Origin = enemy.Position,
                    Landing = landing,
                    PreparedBeat = context.BeatNumber
                };
                context.Events.TelegraphChanged(enemy.Id, "Jump", true, false);
                return RunnerBrain.Emit(context,
                    new EnemyDecision(enemy.Id, EnemyDecisionKind.PrepareJump, landing));
            }

            var step = GridPathfinding.NextStep(
                context.Grid, enemy.Position, context.Player.Position, enemy.Id, context.RandomFor(enemy.Id));
            if (step.HasValue)
            {
                var move = context.Movement.TryResolve(new MovementRequest(
                    enemy.Id, MovementKind.Move, RunnerBrain.DirectionBetween(enemy.Position, step.Value)));
                if (move.Succeeded)
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.Move, move.Landing));
            }
            return RunnerBrain.Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Hold));
        }

        public void SetPaused(int enemyId, ISimulationEventSink events)
        {
            if (_locks.ContainsKey(enemyId))
                events.TelegraphChanged(enemyId, "Jump", true, true);
        }
    }

    public sealed class ThrowerBrain : IEnemyBrain
    {
        private readonly int _pickupRange;
        private readonly int _throwRange;
        private readonly int _impactRadius;
        private readonly Dictionary<int, ThrowTelegraphLock> _locks =
            new Dictionary<int, ThrowTelegraphLock>();

        public ThrowerBrain(int pickupRange = 2, int throwRange = 4, int impactRadius = 1)
        {
            _pickupRange = pickupRange;
            _throwRange = throwRange;
            _impactRadius = impactRadius;
        }

        public ThrowTelegraphLock GetLock(int enemyId) =>
            _locks.TryGetValue(enemyId, out var value) ? value : null;

        public EnemyDecision Act(ActorState enemy, EnemyContext context)
        {
            if (_locks.TryGetValue(enemy.Id, out var locked))
            {
                _locks.Remove(enemy.Id);
                context.Events.TelegraphChanged(enemy.Id, "Throw", false, false);
                var target = context.Grid.GetActor(locked.TargetId);
                if (!IsLockValid(enemy, target, locked, context.Grid))
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.CancelThrow, locked.Landing, locked.TargetId));

                var preservedFlag = target.SelfMovedThisBeat;
                var result = context.Movement.TryResolve(new MovementRequest(
                    target.Id, MovementKind.Relocation, GridDirection.Up, 1, locked.Landing));
                target.SelfMovedThisBeat = preservedFlag;
                if (!result.Succeeded)
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.CancelThrow, locked.Landing, locked.TargetId));

                if (locked.ImpactArea.Contains(context.Player.Position))
                    context.HitPlayer(enemy.Id, "ThrowImpact");
                return RunnerBrain.Emit(context,
                    new EnemyDecision(enemy.Id, EnemyDecisionKind.ResolveThrow, locked.Landing, locked.TargetId));
            }

            var prepared = TryPrepare(enemy, context);
            if (prepared.HasValue) return prepared.Value;

            GridCoord? targetGoal = context.Grid.Actors
                .Where(IsThrowable)
                .OrderBy(a => enemy.Position.ManhattanDistance(a.Position))
                .ThenBy(a => a.SpawnId)
                .Select(a => (GridCoord?)a.Position)
                .FirstOrDefault();
            targetGoal = targetGoal ?? context.Player.Position;
            var next = GridPathfinding.NextStep(
                context.Grid, enemy.Position, targetGoal.Value, enemy.Id, context.RandomFor(enemy.Id));
            if (!next.HasValue && targetGoal.Value != context.Player.Position)
                next = GridPathfinding.NextStep(
                    context.Grid, enemy.Position, context.Player.Position, enemy.Id, context.RandomFor(enemy.Id));
            if (next.HasValue)
            {
                var result = context.Movement.TryResolve(new MovementRequest(
                    enemy.Id, MovementKind.Move, RunnerBrain.DirectionBetween(enemy.Position, next.Value)));
                if (result.Succeeded)
                    return RunnerBrain.Emit(context,
                        new EnemyDecision(enemy.Id, EnemyDecisionKind.Move, result.Landing));
            }
            return RunnerBrain.Emit(context, new EnemyDecision(enemy.Id, EnemyDecisionKind.Hold));
        }

        private EnemyDecision? TryPrepare(ActorState enemy, EnemyContext context)
        {
            var targets = context.Grid.Actors
                .Where(IsThrowable)
                .Where(a => enemy.Position.ManhattanDistance(a.Position) <= _pickupRange)
                .OrderBy(a => a.SpawnId)
                .ToArray();
            if (targets.Length == 0) return null;

            var options = new List<(ActorState target, GridCoord landing, GridCoord[] trajectory)>();
            foreach (var target in targets)
            {
                for (var y = 0; y < context.Grid.Height; y++)
                for (var x = 0; x < context.Grid.Width; x++)
                {
                    var landing = new GridCoord(x, y);
                    if (enemy.Position.ManhattanDistance(landing) > _throwRange ||
                        landing == target.Position ||
                        !context.Grid.IsWalkable(landing) ||
                        context.Grid.IsActorOccupied(landing, target.Id))
                        continue;
                    var trajectory = GridPathfinding.SupercoverLine(enemy.Position, landing).ToArray();
                    if (trajectory.Skip(1).Any(cell => !context.Grid.IsWalkable(cell))) continue;
                    options.Add((target, landing, trajectory));
                }
            }

            if (options.Count == 0) return null;
            var minDistance = options.Min(o => o.landing.ManhattanDistance(context.Player.Position));
            var best = options
                .Where(o => o.landing.ManhattanDistance(context.Player.Position) == minDistance)
                .OrderBy(o => o.target.SpawnId)
                .ThenBy(o => o.landing)
                .ToArray();
            var selected = best[context.RandomFor(enemy.Id).Index(best.Length)];
            var impact = Radius(selected.landing, _impactRadius).ToArray();
            _locks[enemy.Id] = new ThrowTelegraphLock
            {
                ThrowerOrigin = enemy.Position,
                TargetId = selected.target.Id,
                TargetOrigin = selected.target.Position,
                Landing = selected.landing,
                Trajectory = selected.trajectory,
                ImpactArea = impact,
                PreparedBeat = context.BeatNumber
            };
            context.Events.TelegraphChanged(enemy.Id, "Throw", true, false);
            return RunnerBrain.Emit(context, new EnemyDecision(
                enemy.Id, EnemyDecisionKind.PrepareThrow, selected.landing, selected.target.Id));
        }

        private bool IsLockValid(
            ActorState thrower, ActorState target, ThrowTelegraphLock locked, IGridQuery grid)
        {
            return thrower.Position == locked.ThrowerOrigin &&
                   target != null && target.IsAlive && IsThrowable(target) &&
                   target.Position == locked.TargetOrigin &&
                   thrower.Position.ManhattanDistance(target.Position) <= _pickupRange &&
                   thrower.Position.ManhattanDistance(locked.Landing) <= _throwRange &&
                   grid.IsWalkable(locked.Landing) &&
                   !grid.IsActorOccupied(locked.Landing, target.Id) &&
                   locked.Trajectory.Skip(1).All(grid.IsWalkable);
        }

        private static bool IsThrowable(ActorState actor) =>
            actor.IsAlive && (actor.Kind == ActorKind.Runner || actor.Kind == ActorKind.Jumper);

        private static IEnumerable<GridCoord> Radius(GridCoord center, int radius)
        {
            for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
                if (Math.Abs(x) + Math.Abs(y) <= radius)
                    yield return new GridCoord(center.X + x, center.Y + y);
        }

        public void SetPaused(int enemyId, ISimulationEventSink events)
        {
            if (_locks.ContainsKey(enemyId))
                events.TelegraphChanged(enemyId, "Throw", true, true);
        }
    }
}
