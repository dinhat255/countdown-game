using System.Collections;
using System.Linq;
using CountdownGame.Core;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CountdownGame.Tests
{
    public sealed class BeatLoopSmokeTests
    {
        [UnityTest]
        public IEnumerator CompleteBeatLoopSupportsMoveDashInitiativeTelegraphsFreezeAndSpawn()
        {
            var grid = new GridState(10, 6);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(1, 2));
            grid.AddActor(player);
            grid.AddActor(new ActorState(2, 2, ActorKind.Runner, new GridCoord(8, 2)));
            grid.AddActor(new ActorState(3, 3, ActorKind.Jumper, new GridCoord(6, 4)));
            grid.AddActor(new ActorState(4, 4, ActorKind.Thrower, new GridCoord(7, 4)));
            grid.AddSpawnPoint(new GridCoord(9, 0));
            var events = new RecordingEventSink();
            var simulation = new GameSimulation(
                grid,
                player,
                new RunState(30),
                123,
                spawnConfiguration: new SpawnConfiguration { CooldownBeats = 1 },
                events: events);

            simulation.StartBeat();
            Assert.That(simulation.TryPlayerMove(GridDirection.Right).Succeeded, Is.True);
            Assert.That(simulation.TryPlayerDash().FailureReason,
                Is.EqualTo(MovementFailureReason.AlreadySelfMoved));
            simulation.EndPlayerPhase();
            Assert.That(grid.Actors.Where(a => a.Kind != ActorKind.Player)
                .OrderBy(a => a.SpawnId).Select(a => a.SpawnId), Is.Ordered);
            Assert.That(events.Events.Any(e => e.Contains("Telegraph")), Is.True);

            simulation.StartBeat();
            var beforeFreeze = grid.Actors.ToDictionary(a => a.Id, a => a.Position);
            simulation.EndPlayerPhase(true);
            Assert.That(beforeFreeze.All(pair => grid.GetActor(pair.Key).Position == pair.Value), Is.True);

            simulation.StartBeat();
            player.Facing = GridDirection.Right;
            var dash = simulation.TryPlayerDash();
            Assert.That(dash.Succeeded, Is.True);
            simulation.EndPlayerPhase();

            Assert.That(events.Events.Any(e => e.StartsWith("Spawn:")), Is.True);
            Assert.That(simulation.Run.BeatNumber, Is.EqualTo(3));
            yield return null;
        }
    }
}
