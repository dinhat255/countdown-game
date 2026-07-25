using System.Linq;
using CountdownGame.Core;
using NUnit.Framework;

namespace CountdownGame.Tests
{
    public sealed class SkillAndManaTests
    {
        [Test]
        public void ManaStartsAtThreeClampsAtSixAndMeditationRestoresThree()
        {
            var simulation = CreateSimulation();
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(3));
            Assert.That(simulation.Run.MaxMana, Is.EqualTo(6));

            simulation.EquipPassiveSkill(SkillIds.Meditation);
            simulation.StartBeat();
            simulation.EndPlayerPhase();

            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(6));
            Assert.That(simulation.PredictedNoMoveManaRestoration, Is.EqualTo(3));
        }

        [Test]
        public void ValidMovementPreventsManaRestoreButRejectedMovementKeepsEligibility()
        {
            var moved = CreateSimulation(new GridCoord(0, 0), 5, 2);
            moved.StartBeat();
            Assert.That(moved.TryPlayerMove(GridDirection.Right).Succeeded, Is.True);
            moved.EndPlayerPhase();
            Assert.That(moved.Run.CurrentMana, Is.EqualTo(3));

            var rejected = CreateSimulation(new GridCoord(0, 0), 1, 1);
            rejected.StartBeat();
            Assert.That(rejected.TryPlayerMove(GridDirection.Left).Succeeded, Is.False);
            rejected.EndPlayerPhase();
            Assert.That(rejected.Run.CurrentMana, Is.EqualTo(5));
        }

        [Test]
        public void SuccessfulDashSpendsManaConsumesOnlyItsSlotAndUsesMovementRules()
        {
            var simulation = CreateSimulation(new GridCoord(0, 0), 6, 1);
            simulation.EquipActiveSkill(0, SkillIds.Dash);
            simulation.EquipActiveSkill(1, SkillIds.Ward);
            simulation.StartBeat();

            var result = simulation.TryUseSkill(0);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ManaSpent, Is.EqualTo(1));
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(2));
            Assert.That(simulation.Skills.ActiveSlots[0], Is.Null);
            Assert.That(simulation.Skills.ActiveSlots[1], Is.EqualTo(SkillIds.Ward));
            Assert.That(simulation.Player.Position, Is.EqualTo(new GridCoord(3, 0)));
            Assert.That(simulation.Player.PlayerMovedThisBeat, Is.True);
            Assert.That(simulation.Run.Wc, Is.EqualTo(22));
            Assert.That(simulation.Run.MovementPressure, Is.EqualTo(2));
            simulation.EndPlayerPhase();
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(2));
        }

        [Test]
        public void InvalidSkillCallsDoNotSpendManaOrRemoveSkill()
        {
            var simulation = CreateSimulation(new GridCoord(0, 0), 5, 1);
            simulation.Grid.SetBlocker(new GridCoord(2, 0), true);
            simulation.EquipActiveSkill(0, SkillIds.Dash);
            simulation.StartBeat();
            var blocked = simulation.TryUseSkill(0);
            Assert.That(blocked.FailureReason, Is.EqualTo(SkillUseFailureReason.MovementRejected));
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(3));
            Assert.That(simulation.Skills.ActiveSlots[0], Is.EqualTo(SkillIds.Dash));

            simulation.EquipActiveSkill(1, SkillIds.Freeze);
            var expensive = simulation.TryUseSkill(1);
            Assert.That(expensive.FailureReason, Is.EqualTo(SkillUseFailureReason.InsufficientMana));
            Assert.That(simulation.Skills.ActiveSlots[1], Is.EqualTo(SkillIds.Freeze));
            simulation.EndPlayerPhase();
            var wrongPhase = simulation.TryUseSkill(0);
            Assert.That(wrongPhase.FailureReason, Is.EqualTo(SkillUseFailureReason.WrongPhase));
        }

        [Test]
        public void InvalidSnipeAndBombTargetsDoNotSpendOrConsume()
        {
            var simulation = CreateSimulation(initialMana: 6);
            simulation.EquipActiveSkill(0, SkillIds.Snipe);
            simulation.EquipActiveSkill(1, SkillIds.Bomb);
            simulation.Grid.SetBlocker(simulation.Player.Position.Step(GridDirection.Right), true);
            simulation.StartBeat();

            Assert.That(simulation.TryUseSkill(0).FailureReason,
                Is.EqualTo(SkillUseFailureReason.InvalidTarget));
            Assert.That(simulation.TryUseSkill(
                1, new SkillTarget(new GridCoord(7, 2))).FailureReason,
                Is.EqualTo(SkillUseFailureReason.InvalidTarget));
            Assert.That(simulation.Run.CurrentMana, Is.EqualTo(6));
            Assert.That(simulation.Skills.ActiveSlots[0], Is.EqualTo(SkillIds.Snipe));
            Assert.That(simulation.Skills.ActiveSlots[1], Is.EqualTo(SkillIds.Bomb));
        }

        [Test]
        public void SnipeAndShockwaveDealDamageAndDamageUpModifiesPlayerDamage()
        {
            var grid = new GridState(7, 3);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 1));
            var snipeTarget = new ActorState(2, 2, ActorKind.Thrower, new GridCoord(3, 1));
            var adjacent = new ActorState(3, 3, ActorKind.Jumper, new GridCoord(1, 2));
            grid.AddActor(player);
            grid.AddActor(snipeTarget);
            grid.AddActor(adjacent);
            var simulation = new GameSimulation(grid, player, new RunState(20, 6), 4);
            simulation.EquipPassiveSkill(SkillIds.DamageUp);
            simulation.EquipActiveSkill(0, SkillIds.Snipe);
            simulation.EquipActiveSkill(1, SkillIds.Shockwave);
            simulation.StartBeat();

            Assert.That(simulation.TryUseSkill(0).Succeeded, Is.True);
            Assert.That(snipeTarget.Health, Is.EqualTo(1));
            Assert.That(simulation.TryUseSkill(1).Succeeded, Is.True);
            Assert.That(adjacent.Health, Is.EqualTo(1));
            Assert.That(player.PlayerMovedThisBeat, Is.False);
        }

        [Test]
        public void WardNegatesWcOnlyOnceAndDampenerReducesHitPenalty()
        {
            var simulation = CreateSimulation(initialMana: 6);
            simulation.EquipActiveSkill(0, SkillIds.Ward);
            simulation.StartBeat();
            Assert.That(simulation.TryUseSkill(0).Succeeded, Is.True);
            Assert.That(simulation.ResolvePlayerHit(9, "Enemy", 3), Is.Zero);
            Assert.That(simulation.ResolvePlayerHit(9, "Enemy", 3), Is.EqualTo(3));
            Assert.That(simulation.Run.Wc, Is.EqualTo(23));

            simulation.EquipPassiveSkill(SkillIds.WcDampener);
            Assert.That(simulation.ResolvePlayerHit(9, "Enemy", 3), Is.EqualTo(2));
            Assert.That(simulation.Run.Wc, Is.EqualTo(25));
        }

        [Test]
        public void DuplicateWardAndFreezeEffectsAreRejectedWithoutConsumption()
        {
            var simulation = CreateSimulation(initialMana: 12, maxMana: 12);
            simulation.EquipActiveSkill(0, SkillIds.Ward);
            simulation.EquipActiveSkill(1, SkillIds.Ward);
            simulation.EquipActiveSkill(2, SkillIds.Freeze);
            simulation.StartBeat();
            Assert.That(simulation.TryUseSkill(0).Succeeded, Is.True);
            Assert.That(simulation.TryUseSkill(1).FailureReason,
                Is.EqualTo(SkillUseFailureReason.EffectAlreadyActive));
            Assert.That(simulation.Skills.ActiveSlots[1], Is.EqualTo(SkillIds.Ward));
            Assert.That(simulation.TryUseSkill(2).Succeeded, Is.True);
            simulation.EquipActiveSkill(0, SkillIds.Freeze);
            Assert.That(simulation.TryUseSkill(0).FailureReason,
                Is.EqualTo(SkillUseFailureReason.EffectAlreadyActive));
            Assert.That(simulation.Skills.ActiveSlots[0], Is.EqualTo(SkillIds.Freeze));
        }

        [Test]
        public void BombValidatesCellTicksTwoBeatsAndPreservesMovementFlags()
        {
            var grid = new GridState(6, 4);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(1, 1));
            var enemy = new ActorState(2, 2, ActorKind.Jumper, new GridCoord(3, 2));
            grid.AddActor(player);
            grid.AddActor(enemy);
            var simulation = new GameSimulation(grid, player, new RunState(20, 6), 2);
            simulation.EquipActiveSkill(0, SkillIds.Bomb);
            simulation.StartBeat();
            Assert.That(simulation.TryUseSkill(
                0, new SkillTarget(new GridCoord(2, 1))).Succeeded, Is.True);
            Assert.That(simulation.Bombs.Single().FuseRemaining, Is.EqualTo(2));
            simulation.EndPlayerPhase(true);
            Assert.That(simulation.Bombs.Single().FuseRemaining, Is.EqualTo(1));

            simulation.StartBeat();
            simulation.EndPlayerPhase(true);
            Assert.That(simulation.Bombs, Is.Empty);
            Assert.That(enemy.Health, Is.EqualTo(2));
            Assert.That(enemy.SelfMovedThisBeat, Is.False);
            Assert.That(player.PlayerMovedThisBeat, Is.False);
        }

        [Test]
        public void InventorySupportsDuplicatesPendingReplacementDiscardAndAutoFill()
        {
            var inventory = new SkillInventory();
            Assert.That(inventory.TryEquipOrQueue(SkillIds.Dash), Is.True);
            Assert.That(inventory.TryEquipOrQueue(SkillIds.Dash), Is.True);
            Assert.That(inventory.TryEquipOrQueue(SkillIds.Ward), Is.True);
            Assert.That(inventory.TryEquipOrQueue(SkillIds.Freeze), Is.False);
            Assert.That(inventory.PendingSkillId, Is.EqualTo(SkillIds.Freeze));
            inventory.ConsumeActive(1);
            Assert.That(inventory.AutoFillPendingActive(), Is.EqualTo(SkillIds.Freeze));
            Assert.That(inventory.ActiveSlots[1], Is.EqualTo(SkillIds.Freeze));

            Assert.That(inventory.TryEquipOrQueue(SkillIds.Meditation), Is.True);
            Assert.That(inventory.TryEquipOrQueue(SkillIds.DamageUp), Is.False);
            Assert.That(inventory.ResolvePending(
                new PickupDecision(PickupDecisionKind.ReplacePassive)).Succeeded, Is.True);
            Assert.That(inventory.PassiveSlot, Is.EqualTo(SkillIds.DamageUp));
            Assert.That(inventory.TryEquipOrQueue(SkillIds.WcDampener), Is.False);
            Assert.That(inventory.ResolvePending(
                new PickupDecision(PickupDecisionKind.Discard)).Succeeded, Is.True);
        }

        [Test]
        public void DropOccursEveryThirdBeatUsesValidCellsAndHonorsCapAndVictory()
        {
            var grid = new GridState(4, 2);
            var player = new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0));
            grid.AddActor(player);
            grid.AddSpawnPoint(new GridCoord(3, 1));
            grid.AddOverlay(new GridCoord(2, 1), OverlayKind.EnvironmentalBomb);
            var config = new SkillDropConfiguration { GroundItemCap = 1 };
            var first = new SkillDropSystem(grid, config);

            Assert.That(first.TryDrop(12, 1, 1, false), Is.Null);
            var dropped = first.TryDrop(12, 3, 1, false);
            Assert.That(dropped, Is.Not.Null);
            Assert.That(dropped.Cell, Is.Not.EqualTo(player.Position));
            Assert.That(dropped.Cell, Is.Not.EqualTo(new GridCoord(3, 1)));
            Assert.That(dropped.Cell, Is.Not.EqualTo(new GridCoord(2, 1)));
            Assert.That(first.TryDrop(12, 6, 1, false), Is.Null);

            var repeatGrid = new GridState(4, 2);
            repeatGrid.AddActor(new ActorState(1, 1, ActorKind.Player, new GridCoord(0, 0)));
            repeatGrid.AddSpawnPoint(new GridCoord(3, 1));
            repeatGrid.AddOverlay(new GridCoord(2, 1), OverlayKind.EnvironmentalBomb);
            var repeated = new SkillDropSystem(repeatGrid, config).TryDrop(12, 3, 1, false);
            Assert.That(repeated.SkillId, Is.EqualTo(dropped.SkillId));
            Assert.That(repeated.Cell, Is.EqualTo(dropped.Cell));
            Assert.That(new SkillDropSystem(new GridState(2, 2), config)
                .TryDrop(12, 3, 1, true), Is.Null);
        }

        [Test]
        public void DropPhaseWeightsCanForceTheSelectedSkillLevel()
        {
            var config = new SkillDropConfiguration
            {
                PhaseLevelWeights = new[]
                {
                    new[] { 0, 100, 0 },
                    new[] { 0, 0, 100 },
                    new[] { 100, 0, 0 }
                }
            };

            var phaseOne = new SkillDropSystem(new GridState(3, 3), config)
                .TryDrop(3, 3, 1, false);
            var phaseTwo = new SkillDropSystem(new GridState(3, 3), config)
                .TryDrop(3, 3, 2, false);
            Assert.That(StarterSkillCatalog.Get(phaseOne.SkillId).Level, Is.EqualTo(2));
            Assert.That(StarterSkillCatalog.Get(phaseTwo.SkillId).Level, Is.EqualTo(3));
        }

        private static GameSimulation CreateSimulation(
            GridCoord? playerCell = null,
            int width = 8,
            int height = 3,
            int initialMana = 3,
            int maxMana = 6)
        {
            var grid = new GridState(width, height);
            var player = new ActorState(
                1, 1, ActorKind.Player, playerCell ?? new GridCoord(1, 1));
            grid.AddActor(player);
            return new GameSimulation(
                grid, player, new RunState(20, initialMana, maxMana), 7);
        }
    }
}
