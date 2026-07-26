using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CountdownGame.Core;
using CountdownGame.Unity;
using NUnit.Framework;
using UnityEngine;
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
                Is.EqualTo(MovementFailureReason.ActionAlreadyUsed));
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

        [UnityTest]
        public IEnumerator SpawnedEnemyCreatesAVisibleActorViewAtItsOccupiedCell()
        {
            var controllerObject = new GameObject("Spawn View Test Controller");
            controllerObject.SetActive(false);
            var controller = controllerObject.AddComponent<CountdownGameController>();
            var templateObject = new GameObject("Jumper View Template");
            var renderer = templateObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.magenta);
            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            renderer.sprite = sprite;
            var template = templateObject.AddComponent<GridActorView>();
            template.actorId = 3;
            template.spawnId = 3;
            template.actorKind = ActorKind.Jumper;

            var viewsField = typeof(CountdownGameController).GetField(
                "_views",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(viewsField, Is.Not.Null);
            var views = (Dictionary<int, GridActorView>)viewsField.GetValue(controller);
            views.Add(template.actorId, template);
            var spawned = new ActorState(
                1000,
                1000,
                ActorKind.Jumper,
                new GridCoord(4, 5));

            controller.EnemySpawned(spawned);

            Assert.That(views.TryGetValue(spawned.Id, out var spawnedView), Is.True);
            Assert.That(spawnedView, Is.Not.SameAs(template));
            Assert.That(spawnedView.gameObject.activeSelf, Is.True);
            Assert.That(spawnedView.actorKind, Is.EqualTo(ActorKind.Jumper));
            Assert.That(spawnedView.initialCell, Is.EqualTo(new Vector2Int(4, 5)));
            Assert.That(spawnedView.GetComponent<SpriteRenderer>().sprite, Is.SameAs(sprite));

            Object.Destroy(spawnedView.gameObject);
            Object.Destroy(templateObject);
            Object.Destroy(controllerObject);
            Object.Destroy(sprite);
            Object.Destroy(texture);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DropPickupCastRemovalAndNoMoveRegenerationFlow()
        {
            var grid = new GridState(10, 6);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(4, 3));
            grid.AddActor(player);
            for (var y = 0; y < grid.Height; y++)
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new GridCoord(x, y);
                if (!HasPickupDirection(grid, cell))
                    grid.AddOverlay(cell, OverlayKind.EnvironmentalBomb);
            }
            var dashOnly = new List<SkillDefinitionModel>
            {
                new SkillDefinitionModel(
                    SkillIds.Dash, SkillCategory.Active, 1, 1,
                    SkillTargeting.Facing, "Dash")
            };
            var simulation = new GameSimulation(
                grid,
                player,
                new RunState(30),
                42,
                skillDropConfiguration: new SkillDropConfiguration
                {
                    IntervalBeats = 3,
                    PhaseLevelWeights = new[]
                    {
                        new[] { 100, 0, 0 },
                        new[] { 100, 0, 0 },
                        new[] { 100, 0, 0 }
                    }
                },
                skillCatalog: dashOnly);

            for (var beat = 0; beat < 3; beat++)
            {
                simulation.StartBeat();
                simulation.EndPlayerPhase(true);
            }

            var item = simulation.SkillDrops.Items.Single();
            var direction = FindPickupDirection(grid, item.Cell);
            var origin = item.Cell.Step(Opposite(direction));
            grid.CommitPosition(player, origin);
            player.Facing = direction;
            simulation.StartBeat();
            Assert.That(simulation.TryPlayerMove(direction).Succeeded, Is.True);
            Assert.That(simulation.SkillDrops.Items, Is.Empty);
            Assert.That(simulation.Skills.ActiveSlots[0], Is.EqualTo(SkillIds.Dash));
            simulation.EndPlayerPhase(true);

            simulation.StartBeat();
            var manaBeforeCast = simulation.Run.CurrentMana;
            Assert.That(simulation.TryUseSkill(0).Succeeded, Is.True);
            Assert.That(simulation.Skills.ActiveSlots[0], Is.Null);
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(manaBeforeCast - 1));
            simulation.EndPlayerPhase(true);

            simulation.StartBeat();
            var manaBeforeRestore = simulation.Run.CurrentMana;
            simulation.EndPlayerPhase(true);
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(
                System.Math.Min(simulation.Run.MaxMana, manaBeforeRestore + 2)));
            yield return null;
        }

        private static GridDirection FindPickupDirection(GridState grid, GridCoord item)
        {
            foreach (var direction in GridDirections.Cardinal)
            {
                var origin = item.Step(Opposite(direction));
                var dashLanding = item.Step(direction, 3);
                if (grid.IsWalkable(origin) && grid.IsWalkable(dashLanding))
                    return direction;
            }
            Assert.Fail("Test map did not provide a pickup direction with a later dash path.");
            return GridDirection.Right;
        }

        private static bool HasPickupDirection(GridState grid, GridCoord item)
        {
            foreach (var direction in GridDirections.Cardinal)
                if (grid.IsWalkable(item.Step(Opposite(direction))) &&
                    grid.IsWalkable(item.Step(direction, 3)))
                    return true;
            return false;
        }

        private static GridDirection Opposite(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Up: return GridDirection.Down;
                case GridDirection.Right: return GridDirection.Left;
                case GridDirection.Down: return GridDirection.Up;
                default: return GridDirection.Right;
            }
        }
    }
}
