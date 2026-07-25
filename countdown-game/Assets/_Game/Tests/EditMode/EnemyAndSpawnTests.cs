using System.Linq;
using CountdownGame.Core;
using NUnit.Framework;

namespace CountdownGame.Tests
{
    public sealed class EnemyAndSpawnTests
    {
        [Test]
        public void SeededContextIsRepeatableAndSeedsCanVaryEquivalentChoice()
        {
            var first = new SeededRandomContext(12, 3, 8, 1).Index(4);
            var again = new SeededRandomContext(12, 3, 8, 1).Index(4);
            Assert.That(again, Is.EqualTo(first));
            Assert.That(Enumerable.Range(13, 100)
                .Select(seed => new SeededRandomContext(seed, 3, 8, 1).Index(4))
                .Any(index => index != first), Is.True);
        }

        [Test]
        public void RunnerHoldsAndAttacksWhenAdjacent()
        {
            var grid = new GridState(5, 3);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(2, 1));
            var runner = new ActorState(2, 2, ActorKind.Runner, new GridCoord(1, 1));
            grid.AddActor(player);
            grid.AddActor(runner);
            var events = new RecordingEventSink();
            var context = new EnemyContext(
                grid, new MovementResolver(grid, events), player, 4, 1, events);

            var decision = new RunnerBrain().Act(runner, context);

            Assert.That(decision.Kind, Is.EqualTo(EnemyDecisionKind.Attack));
            Assert.That(runner.Position, Is.EqualTo(new GridCoord(1, 1)));
            Assert.That(runner.SelfMovedThisBeat, Is.False);
        }

        [Test]
        public void RunnerShortestPathRoutesAroundWallAndAttacksAfterMove()
        {
            var grid = new GridState(5, 4);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(4, 1));
            var runner = new ActorState(2, 2, ActorKind.Runner, new GridCoord(1, 1));
            grid.AddActor(player);
            grid.AddActor(runner);
            grid.SetBlocker(new GridCoord(2, 1), true);
            var context = new EnemyContext(
                grid, new MovementResolver(grid), player, 4, 1, NullSimulationEventSink.Instance);

            new RunnerBrain().Act(runner, context);

            Assert.That(runner.Position, Is.EqualTo(new GridCoord(1, 2))
                .Or.EqualTo(new GridCoord(1, 0)));
            Assert.That(runner.SelfMovedThisBeat, Is.True);
        }

        [Test]
        public void JumperTelegraphsThenResolvesLockedLandingAndHitsRadiusOne()
        {
            var grid = new GridState(6, 4);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(4, 1));
            var jumper = new ActorState(2, 2, ActorKind.Jumper, new GridCoord(2, 1));
            grid.AddActor(player);
            grid.AddActor(jumper);
            var events = new RecordingEventSink();
            var brain = new JumperBrain();
            var movement = new MovementResolver(grid, events);

            var prepared = brain.Act(jumper, new EnemyContext(grid, movement, player, 2, 1, events));
            var locked = brain.GetLock(jumper.Id);
            Assert.That(prepared.Kind, Is.EqualTo(EnemyDecisionKind.PrepareJump));
            Assert.That(locked, Is.Not.Null);

            jumper.ResetForBeat();
            var resolved = brain.Act(jumper, new EnemyContext(grid, movement, player, 2, 2, events));
            Assert.That(resolved.Kind, Is.EqualTo(EnemyDecisionKind.ResolveJump));
            Assert.That(jumper.Position, Is.EqualTo(locked.Landing));
            Assert.That(events.Events.Any(e => e.Contains("JumperShockwave")),
                Is.EqualTo(locked.Landing.ManhattanDistance(player.Position) <= 1));
        }

        [Test]
        public void JumperCancelsBlockedLockWithoutRetargeting()
        {
            var grid = new GridState(7, 5);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(6, 2));
            var jumper = new ActorState(2, 2, ActorKind.Jumper, new GridCoord(2, 2));
            grid.AddActor(player);
            grid.AddActor(jumper);
            var brain = new JumperBrain();
            var movement = new MovementResolver(grid);
            brain.Act(jumper, new EnemyContext(
                grid, movement, player, 2, 1, NullSimulationEventSink.Instance));
            var landing = brain.GetLock(jumper.Id).Landing;
            grid.SetBlocker(landing, true);
            jumper.ResetForBeat();

            var cancelled = brain.Act(jumper, new EnemyContext(
                grid, movement, player, 2, 2, NullSimulationEventSink.Instance));

            Assert.That(cancelled.Kind, Is.EqualTo(EnemyDecisionKind.CancelJump));
            Assert.That(jumper.Position, Is.EqualTo(new GridCoord(2, 2)));
            Assert.That(brain.GetLock(jumper.Id), Is.Null);
        }

        [Test]
        public void JumperFallsBackToMoveWhenNoTwoCellLandingExists()
        {
            var grid = new GridState(3, 1);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(2, 0));
            var jumper = new ActorState(2, 2, ActorKind.Jumper, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddActor(jumper);

            var decision = new JumperBrain().Act(jumper, new EnemyContext(
                grid, new MovementResolver(grid), player, 1, 1, NullSimulationEventSink.Instance));

            Assert.That(decision.Kind, Is.EqualTo(EnemyDecisionKind.Move));
            Assert.That(jumper.Position, Is.EqualTo(new GridCoord(1, 0)));
        }

        [Test]
        public void FreezePausesExistingTelegraphForAnEntireEnemyPhase()
        {
            var grid = new GridState(7, 5);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(6, 2));
            var jumper = new ActorState(2, 2, ActorKind.Jumper, new GridCoord(2, 2));
            grid.AddActor(player);
            grid.AddActor(jumper);
            var events = new RecordingEventSink();
            var simulation = new GameSimulation(grid, player, new RunState(20), 3, events: events);
            simulation.StartBeat();
            simulation.EndPlayerPhase();
            var lockBefore = simulation.Jumper.GetLock(jumper.Id);
            simulation.StartBeat();
            simulation.EndPlayerPhase(true);

            Assert.That(simulation.Jumper.GetLock(jumper.Id), Is.SameAs(lockBefore));
            Assert.That(events.Events.Any(e => e == $"Telegraph:{jumper.Id}:Jump:True:True"), Is.True);
        }

        [Test]
        public void ThrowerLocksRelocatesAndPreservesTargetMovementFlag()
        {
            var grid = new GridState(8, 5);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(6, 2));
            var target = new ActorState(2, 2, ActorKind.Runner, new GridCoord(5, 2));
            var thrower = new ActorState(3, 3, ActorKind.Thrower, new GridCoord(4, 2));
            grid.AddActor(player);
            grid.AddActor(target);
            grid.AddActor(thrower);
            var events = new RecordingEventSink();
            var brain = new ThrowerBrain();
            var movement = new MovementResolver(grid, events);
            var prepared = brain.Act(thrower, new EnemyContext(grid, movement, player, 5, 1, events));
            var locked = brain.GetLock(thrower.Id);
            Assert.That(prepared.Kind, Is.EqualTo(EnemyDecisionKind.PrepareThrow));
            target.SelfMovedThisBeat = true;
            thrower.ResetForBeat();

            var resolved = brain.Act(thrower, new EnemyContext(grid, movement, player, 5, 2, events));

            Assert.That(resolved.Kind, Is.EqualTo(EnemyDecisionKind.ResolveThrow));
            Assert.That(target.Position, Is.EqualTo(locked.Landing));
            Assert.That(target.SelfMovedThisBeat, Is.True);
        }

        [Test]
        public void ThrowerCancelsBlockedLockedTrajectoryWithoutRetargeting()
        {
            var grid = new GridState(8, 5);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(7, 2));
            var target = new ActorState(2, 2, ActorKind.Runner, new GridCoord(3, 2));
            var thrower = new ActorState(3, 3, ActorKind.Thrower, new GridCoord(2, 2));
            grid.AddActor(player);
            grid.AddActor(target);
            grid.AddActor(thrower);
            var brain = new ThrowerBrain();
            var movement = new MovementResolver(grid);
            brain.Act(thrower, new EnemyContext(
                grid, movement, player, 5, 1, NullSimulationEventSink.Instance));
            var locked = brain.GetLock(thrower.Id);
            var blockingCell = locked.Trajectory.Skip(1).First(c => c != locked.Landing);
            grid.SetBlocker(blockingCell, true);
            thrower.ResetForBeat();

            var cancelled = brain.Act(thrower, new EnemyContext(
                grid, movement, player, 5, 2, NullSimulationEventSink.Instance));

            Assert.That(cancelled.Kind, Is.EqualTo(EnemyDecisionKind.CancelThrow));
            Assert.That(target.Position, Is.EqualTo(locked.TargetOrigin));
            Assert.That(brain.GetLock(thrower.Id), Is.Null);
        }

        [Test]
        public void ThrowInitiativeLetsLaterTargetActAndNeverReplaysEarlierTarget()
        {
            var laterEvents = RunThrowInitiativeScenario(throwerSpawnId: 1, targetSpawnId: 2);
            var laterResolve = laterEvents.FindIndex(d => d.Kind == EnemyDecisionKind.ResolveThrow);
            var laterTargetAction = laterEvents.FindIndex(
                laterResolve + 1, d => d.EnemyId == 2 && d.Kind == EnemyDecisionKind.Attack);
            Assert.That(laterResolve, Is.GreaterThanOrEqualTo(0));
            Assert.That(laterTargetAction, Is.GreaterThan(laterResolve));

            var earlierEvents = RunThrowInitiativeScenario(throwerSpawnId: 2, targetSpawnId: 1);
            var earlierResolve = earlierEvents.FindIndex(d => d.Kind == EnemyDecisionKind.ResolveThrow);
            Assert.That(earlierResolve, Is.GreaterThanOrEqualTo(0));
            Assert.That(earlierEvents.Skip(earlierResolve + 1).Any(d => d.EnemyId == 2), Is.False);
        }

        [Test]
        public void SpawningHonorsCooldownCapDistanceOccupiedPointsAndVictory()
        {
            var grid = new GridState(10, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddSpawnPoint(new GridCoord(1, 0));
            grid.AddSpawnPoint(new GridCoord(8, 0));
            var config = new SpawnConfiguration
            {
                CooldownBeats = 1,
                LivingEnemyCap = 1,
                MinimumPlayerDistance = 4,
                PhaseWeights = new[]
                {
                    new[] { 100, 0, 0 },
                    new[] { 0, 100, 0 },
                    new[] { 0, 0, 100 }
                }
            };
            var spawns = new SpawnSystem(grid, config, 10, 10);

            var spawned = spawns.Tick(player, 1, 0, 1, 1, false);
            Assert.That(spawned, Is.Not.Null);
            Assert.That(spawned.Kind, Is.EqualTo(ActorKind.Runner));
            Assert.That(spawned.Position, Is.EqualTo(new GridCoord(8, 0)));
            Assert.That(spawns.Tick(player, 2, 0, 1, 2, false), Is.Null);
            spawned.IsAlive = false;
            Assert.That(spawns.Tick(player, 3, 0, 1, 3, true), Is.Null);
        }

        [Test]
        public void VictoryShortCircuitsSpawn()
        {
            var grid = new GridState(8, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddSpawnPoint(new GridCoord(7, 0));
            var simulation = new GameSimulation(
                grid,
                player,
                new RunState(1),
                9,
                spawnConfiguration: new SpawnConfiguration { CooldownBeats = 1 });
            simulation.StartBeat();
            simulation.EndPlayerPhase();

            Assert.That(simulation.Phase, Is.EqualTo(BeatPhase.Victory));
            Assert.That(grid.Actors.Count, Is.EqualTo(1));
        }

        [TestCase(GridDirection.Up)]
        [TestCase(GridDirection.Right)]
        [TestCase(GridDirection.Down)]
        [TestCase(GridDirection.Left)]
        [Timeout(1000)]
        public void VerticalSliceLayoutCompletesEnemyPhaseAfterPlayerMove(GridDirection direction)
        {
            var grid = new GridState(9, 7);
            for (var y = 1; y <= 3; y++)
                grid.SetBlocker(new GridCoord(4, y), true);
            grid.SetBlocker(new GridCoord(2, 4), true);

            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(1, 2));
            grid.AddActor(player);
            grid.AddActor(new ActorState(2, 2, ActorKind.Runner, new GridCoord(7, 2)));
            grid.AddActor(new ActorState(3, 3, ActorKind.Jumper, new GridCoord(6, 5)));
            grid.AddActor(new ActorState(4, 4, ActorKind.Thrower, new GridCoord(7, 5)));
            grid.AddSpawnPoint(new GridCoord(8, 0));
            grid.AddSpawnPoint(new GridCoord(8, 6));
            grid.AddSpawnPoint(new GridCoord(0, 6));
            var events = new RecordingEventSink();
            var simulation = new GameSimulation(grid, player, new RunState(12), 12345, events: events);

            simulation.StartBeat();
            Assert.That(simulation.TryPlayerMove(direction).Succeeded, Is.True);
            simulation.EndPlayerPhase();

            Assert.That(simulation.Phase, Is.EqualTo(BeatPhase.NotStarted));
            Assert.That(events.Decisions.Select(d => d.EnemyId), Does.Contain(2));
            Assert.That(events.Decisions.Select(d => d.EnemyId), Does.Contain(3));
            Assert.That(events.Decisions.Select(d => d.EnemyId), Does.Contain(4));
        }

        private static System.Collections.Generic.List<EnemyDecision> RunThrowInitiativeScenario(
            int throwerSpawnId, int targetSpawnId)
        {
            var grid = new GridState(9, 5);
            var player = new ActorState(1, 0, ActorKind.Player, new GridCoord(6, 2));
            var target = new ActorState(2, targetSpawnId, ActorKind.Runner, new GridCoord(5, 2));
            var thrower = new ActorState(3, throwerSpawnId, ActorKind.Thrower, new GridCoord(4, 2));
            grid.AddActor(player);
            grid.AddActor(target);
            grid.AddActor(thrower);
            var events = new RecordingEventSink();
            var simulation = new GameSimulation(grid, player, new RunState(20), 17, events: events);
            simulation.StartBeat();
            simulation.EndPlayerPhase();
            events.Decisions.Clear();
            simulation.StartBeat();
            simulation.EndPlayerPhase();
            return events.Decisions;
        }
    }
}
