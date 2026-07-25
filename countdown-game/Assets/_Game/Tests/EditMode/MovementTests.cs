using System.Linq;
using CountdownGame.Core;
using CountdownGame.Unity;
using NUnit.Framework;
using UnityEngine;

namespace CountdownGame.Tests
{
    public sealed class MovementTests
    {
        [Test]
        public void MoveRejectsBoundsWallsAndOccupancyWithoutMutation()
        {
            var grid = new GridState(4, 3);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            var blocker = new ActorState(2, 2, ActorKind.Runner, new GridCoord(1, 0));
            grid.AddActor(player);
            grid.AddActor(blocker);
            grid.SetBlocker(new GridCoord(0, 1), true);
            var resolver = new MovementResolver(grid);

            var occupied = resolver.TryResolve(
                new MovementRequest(player.Id, MovementKind.Move, GridDirection.Right));
            var wall = resolver.TryResolve(
                new MovementRequest(player.Id, MovementKind.Move, GridDirection.Up));
            var bounds = resolver.TryResolve(
                new MovementRequest(player.Id, MovementKind.Move, GridDirection.Left));

            Assert.That(occupied.FailureReason, Is.EqualTo(MovementFailureReason.OccupiedLanding));
            Assert.That(wall.FailureReason, Is.EqualTo(MovementFailureReason.BlockedTerrain));
            Assert.That(bounds.FailureReason, Is.EqualTo(MovementFailureReason.OutOfBounds));
            Assert.That(player.Position, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(player.PlayerMovedThisBeat, Is.False);
            Assert.That(player.Facing, Is.EqualTo(GridDirection.Right));
        }

        [Test]
        public void MoveAndDashShareOneSelfMovementCap()
        {
            var grid = new GridState(6, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            var resolver = new MovementResolver(grid);

            Assert.That(resolver.TryResolve(
                new MovementRequest(1, MovementKind.Move, GridDirection.Right)).Succeeded, Is.True);
            var dash = resolver.TryResolve(
                new MovementRequest(1, MovementKind.Dash, GridDirection.Right, 3));

            Assert.That(dash.FailureReason, Is.EqualTo(MovementFailureReason.AlreadySelfMoved));
            Assert.That(player.Position, Is.EqualTo(new GridCoord(1, 0)));
        }

        [Test]
        public void DashValidatesEntireTerrainButCanCrossEnemy()
        {
            var grid = new GridState(6, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddActor(new ActorState(2, 2, ActorKind.Runner, new GridCoord(1, 0)));
            var resolver = new MovementResolver(grid);

            var success = resolver.TryResolve(
                new MovementRequest(1, MovementKind.Dash, GridDirection.Right, 3));
            Assert.That(success.Succeeded, Is.True);
            Assert.That(player.Position, Is.EqualTo(new GridCoord(3, 0)));

            player.ResetForBeat();
            grid.SetBlocker(new GridCoord(4, 0), true);
            var failed = resolver.TryResolve(
                new MovementRequest(1, MovementKind.Dash, GridDirection.Right, 2));
            Assert.That(failed.FailureReason, Is.EqualTo(MovementFailureReason.BlockedTerrain));
            Assert.That(player.Position, Is.EqualTo(new GridCoord(3, 0)));
            Assert.That(player.SelfMovedThisBeat, Is.False);
        }

        [Test]
        public void DashOnlyEmitsLandingOverlayAndFailedDashHasNoWcOrPressureSideEffects()
        {
            var grid = new GridState(5, 1);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddOverlay(new GridCoord(1, 0), OverlayKind.Item);
            grid.AddOverlay(new GridCoord(3, 0), OverlayKind.EnvironmentalBomb);
            var events = new RecordingEventSink();
            var simulation = new GameSimulation(
                grid, player, new RunState(10), 7, events: events);
            simulation.StartBeat();

            var dash = simulation.TryPlayerDash();
            Assert.That(dash.Succeeded, Is.True);
            Assert.That(events.Events.Any(e => e.Contains("(1,0)")), Is.False);
            Assert.That(events.Events.Any(e => e == "Overlay:1:(3,0):EnvironmentalBomb"), Is.True);
            Assert.That(simulation.Run.Wc, Is.EqualTo(12));
            Assert.That(simulation.Run.MovementPressure, Is.EqualTo(2));

            var failedGrid = new GridState(5, 1);
            var failedPlayer = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            failedGrid.AddActor(failedPlayer);
            failedGrid.SetBlocker(new GridCoord(2, 0), true);
            var failedSimulation = new GameSimulation(
                failedGrid, failedPlayer, new RunState(10), 7);
            failedSimulation.StartBeat();
            Assert.That(failedSimulation.TryPlayerDash().Succeeded, Is.False);
            Assert.That(failedSimulation.Run.Wc, Is.EqualTo(10));
            Assert.That(failedSimulation.Run.MovementPressure, Is.Zero);
            Assert.That(failedPlayer.PlayerMovedThisBeat, Is.False);
        }

        [Test]
        public void RelocationPreservesMovementFlagAndDoesNotInteractWithIntermediateCells()
        {
            var grid = new GridState(5, 2);
            var target = new ActorState(2, 2, ActorKind.Runner, new GridCoord(1, 0));
            grid.AddActor(target);
            target.SelfMovedThisBeat = true;
            var resolver = new MovementResolver(grid);

            var result = resolver.TryResolve(new MovementRequest(
                target.Id, MovementKind.Relocation, GridDirection.Up, 1, new GridCoord(4, 1)));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(target.Position, Is.EqualTo(new GridCoord(4, 1)));
            Assert.That(target.SelfMovedThisBeat, Is.True);
        }

        [Test]
        public void GroundSkillItemOverlayDoesNotBlockPlayerMovement()
        {
            var grid = new GridState(3, 1);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddOverlay(new GridCoord(1, 0), OverlayKind.Item);

            var result = new MovementResolver(grid).TryResolve(
                new MovementRequest(player.Id, MovementKind.Move, GridDirection.Right));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(player.Position, Is.EqualTo(new GridCoord(1, 0)));
        }

        [Test]
        public void AvailablePlayerMoveCellsOnlyReturnsLegalDestinationsBeforeMoving()
        {
            var grid = new GridState(3, 3);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(1, 1));
            grid.AddActor(player);
            grid.AddActor(new ActorState(2, 2, ActorKind.Runner, new GridCoord(2, 1)));
            grid.SetBlocker(new GridCoord(1, 2), true);
            var simulation = new GameSimulation(
                grid, player, new RunState(10), 7);

            Assert.That(simulation.GetAvailablePlayerMoveCells(), Is.Empty);

            simulation.StartBeat();

            Assert.That(simulation.GetAvailablePlayerMoveCells(), Is.EqualTo(new[]
            {
                new GridCoord(1, 0),
                new GridCoord(0, 1)
            }));

            Assert.That(simulation.TryPlayerMove(GridDirection.Down).Succeeded, Is.True);
            Assert.That(simulation.GetAvailablePlayerMoveCells(), Is.Empty);
        }

        [Test]
        public void ClickingDistantGroundSkillItemChoosesOneLegalApproachStep()
        {
            var grid = new GridState(5, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddOverlay(new GridCoord(4, 0), OverlayKind.Item);

            var found = PlayerInputAdapter.TryGetGroundItemApproachDirection(
                grid, player.Position, new GridCoord(4, 0), out var direction);
            var result = found
                ? new MovementResolver(grid).TryResolve(
                    new MovementRequest(player.Id, MovementKind.Move, direction))
                : MovementResult.Rejected(
                    player.Id, MovementFailureReason.InvalidDistance, player.Position, player.Position);

            Assert.That(found, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(player.Position.ManhattanDistance(new GridCoord(0, 0)), Is.EqualTo(1));
            Assert.That(player.Position.ManhattanDistance(new GridCoord(4, 0)), Is.EqualTo(3));
        }

        [Test]
        public void ClickMovementOnlyAcceptsAnAdjacentCardinalDestination()
        {
            Assert.That(PlayerInputAdapter.TryGetMoveDirection(
                new Vector2Int(2, 2), new Vector2Int(2, 3), out var up), Is.True);
            Assert.That(up, Is.EqualTo(GridDirection.Up));
            Assert.That(PlayerInputAdapter.TryGetMoveDirection(
                new Vector2Int(2, 2), new Vector2Int(3, 3), out _), Is.False);
            Assert.That(PlayerInputAdapter.TryGetMoveDirection(
                new Vector2Int(2, 2), new Vector2Int(2, 4), out _), Is.False);
        }
    }
}
