using System;
using System.Linq;
using CountdownGame.Core;
using CountdownGame.Unity;

namespace CountdownGame.UI
{
    public sealed class GameplayHudState
    {
        public string BeatWcText { get; private set; }
        public string ManaText { get; private set; }
        public string PassiveText { get; private set; }
        public string PendingText { get; private set; }
        public string RuntimeFeedbackText { get; private set; }
        public string WcText { get; private set; }
        public string PassiveSkillId { get; private set; }
        public string PendingSkillId { get; private set; }
        public int BeatNumber { get; private set; }
        public int CurrentWc { get; private set; }
        public int InitialWc { get; private set; }
        public int CurrentMana { get; private set; }
        public int MaxMana { get; private set; }
        public float ManaCounterFill { get; private set; }
        public float NoMoveCounterFill { get; private set; }
        public float BombCounterFill { get; private set; }
        public float BeatDurationSeconds { get; private set; }
        public bool ContextVisible { get; private set; }
        public bool ReplacementVisible { get; private set; }
        public bool PendingIsActiveSkill { get; private set; }
        public bool BeatTimerActive { get; private set; }
        public ActiveSkillSlotState[] ActiveSlots { get; private set; }

        public static GameplayHudState From(CountdownGameController controller)
        {
            if (controller == null)
            {
                return Waiting("HUD waiting for CountdownGameController.");
            }

            var simulation = controller.Simulation;
            if (simulation == null)
            {
                return Waiting("HUD waiting for simulation.");
            }

            var state = new GameplayHudState
            {
                BeatWcText = $"Beat {simulation.Run.BeatNumber}",
                ManaText = string.Empty,
                PassiveText = $"Passive: {FormatSkillId(simulation.Skills.PassiveSlot)}",
                RuntimeFeedbackText = string.Empty,
                WcText = $"WC {simulation.Run.Wc}/{Math.Max(simulation.Run.InitialWc, simulation.Run.Wc)}",
                PassiveSkillId = simulation.Skills.PassiveSlot,
                PendingSkillId = simulation.Skills.PendingSkillId,
                BeatNumber = simulation.Run.BeatNumber,
                CurrentWc = simulation.Run.Wc,
                InitialWc = simulation.Run.InitialWc,
                CurrentMana = simulation.Run.CurrentMana,
                MaxMana = simulation.Run.MaxMana,
                ManaCounterFill = CounterRatio(simulation.Run.CurrentMana, simulation.Run.MaxMana),
                NoMoveCounterFill = simulation.Player.PlayerMovedThisBeat ? 0f : 1f,
                BombCounterFill = BuildBombCounterFill(simulation),
                BeatDurationSeconds = BeatDurationForWc(simulation.Run.Wc),
                ReplacementVisible = simulation.Skills.HasPendingPickup,
                PendingIsActiveSkill = simulation.Skills.PendingCategory == SkillCategory.Active,
                BeatTimerActive =
                    simulation.Phase == BeatPhase.Player && !simulation.Skills.HasPendingPickup
            };

            state.ActiveSlots = BuildActiveSlots(simulation, controller.TargetingSkillSlot);
            state.PendingText = BuildPendingText(simulation, controller.TargetingSkillSlot, state.ActiveSlots);
            state.ContextVisible =
                state.ReplacementVisible || IsTargetingVisible(controller.TargetingSkillSlot, state.ActiveSlots);
            return state;
        }

        private static GameplayHudState Waiting(string message)
        {
            return new GameplayHudState
            {
                BeatWcText = message,
                ManaText = "Mana: --",
                PassiveText = "Passive: --",
                PendingText = message,
                RuntimeFeedbackText = string.Empty,
                WcText = "WC --",
                PassiveSkillId = null,
                PendingSkillId = null,
                BeatNumber = -1,
                CurrentWc = 0,
                InitialWc = 0,
                CurrentMana = 0,
                MaxMana = 0,
                ManaCounterFill = 0f,
                NoMoveCounterFill = 0f,
                BombCounterFill = 0f,
                BeatDurationSeconds = 0f,
                ContextVisible = true,
                ReplacementVisible = false,
                PendingIsActiveSkill = false,
                BeatTimerActive = false,
                ActiveSlots = new[]
                {
                    ActiveSkillSlotState.Empty(0, "No simulation"),
                    ActiveSkillSlotState.Empty(1, "No simulation"),
                    ActiveSkillSlotState.Empty(2, "No simulation")
                }
            };
        }

        private static ActiveSkillSlotState[] BuildActiveSlots(GameSimulation simulation, int targetingSkillSlot)
        {
            var count = simulation.Skills.ActiveSlots.Count;
            var slots = new ActiveSkillSlotState[count];
            for (var index = 0; index < count; index++)
            {
                slots[index] = BuildActiveSlot(simulation, index, targetingSkillSlot);
            }

            return slots;
        }

        private static ActiveSkillSlotState BuildActiveSlot(
            GameSimulation simulation,
            int slotIndex,
            int targetingSkillSlot)
        {
            var skillId = simulation.Skills.ActiveSlots[slotIndex];
            if (string.IsNullOrEmpty(skillId))
            {
                return ActiveSkillSlotState.Empty(slotIndex, "Empty slot");
            }

            var definition = StarterSkillCatalog.Get(skillId);
            if (definition == null)
            {
                return ActiveSkillSlotState.Disabled(
                    slotIndex,
                    $"{slotIndex + 1}. {skillId}\nUnknown skill",
                    "Unknown skill definition");
            }

            var disabledReason = GetDisabledReason(simulation, definition);
            var selected = targetingSkillSlot == slotIndex;
            var label = $"{slotIndex + 1}. {FormatSkillId(skillId)}";
            if (selected)
            {
                label += "\nTARGETING - click to cancel";
            }
            else if (!string.IsNullOrEmpty(disabledReason))
            {
                label += $"\n{disabledReason}";
            }

            return new ActiveSkillSlotState(
                slotIndex,
                skillId,
                label,
                string.IsNullOrEmpty(disabledReason),
                selected,
                disabledReason,
                definition.Targeting);
        }

        private static string GetDisabledReason(GameSimulation simulation, SkillDefinitionModel definition)
        {
            if (simulation.Phase != BeatPhase.Player)
                return "Wrong phase";
            if (simulation.Run.CurrentMana < definition.ManaCost)
                return "Not enough Mana";

            switch (definition.Id)
            {
                case SkillIds.Dash:
                    return simulation.Player.SelfMovedThisBeat ? "Already moved" : null;
                case SkillIds.Snipe:
                    return FindSnipeTarget(simulation) == null ? "No target ahead" : null;
                case SkillIds.Ward:
                    return simulation.WardArmed ? "Ward already on" : null;
                case SkillIds.Shockwave:
                    return AdjacentEnemyCount(simulation) == 0 ? "No adjacent enemy" : null;
                case SkillIds.Freeze:
                    return simulation.FreezeArmed ? "Freeze already on" : null;
                default:
                    return null;
            }
        }

        private static string BuildPendingText(
            GameSimulation simulation,
            int targetingSkillSlot,
            ActiveSkillSlotState[] activeSlots)
        {
            if (simulation.Skills.HasPendingPickup)
            {
                var pending = StarterSkillCatalog.Get(simulation.Skills.PendingSkillId);
                var category = pending != null
                    ? pending.Category.ToString()
                    : simulation.Skills.PendingCategory.ToString();
                var cost = pending != null && pending.Category == SkillCategory.Active
                    ? $" | Cost {pending.ManaCost}"
                    : string.Empty;
                var description = !string.IsNullOrEmpty(pending?.Description)
                    ? $"\n{pending.Description}"
                    : string.Empty;
                return $"New skill: {FormatSkillId(simulation.Skills.PendingSkillId)}\n{category}{cost}{description}";
            }

            if (targetingSkillSlot >= 0 && targetingSkillSlot < activeSlots.Length)
            {
                var slot = activeSlots[targetingSkillSlot];
                return $"Targeting {slot.DisplayName}\nChoose a valid cell on the map, or click the skill again to cancel.";
            }

            return "No pending pickup.";
        }

        private static float BuildBombCounterFill(GameSimulation simulation)
        {
            if (simulation.Bombs.Count == 0) return 0f;
            var nearestFuse = simulation.Bombs.Min(bomb => bomb.FuseRemaining);
            return CounterRatio(nearestFuse, 2);
        }

        internal static float BeatDurationForWc(int wc)
        {
            if (wc > 10) return 2.4f;
            if (wc > 5) return 1.8f;
            return 1.6f;
        }

        private static float CounterRatio(int value, int maximum)
        {
            if (maximum <= 0) return 0f;
            return Math.Max(0f, Math.Min(1f, value / (float)maximum));
        }

        private static bool IsTargetingVisible(int targetingSkillSlot, ActiveSkillSlotState[] activeSlots) =>
            targetingSkillSlot >= 0 && targetingSkillSlot < activeSlots.Length;

        private static ActorState FindSnipeTarget(GameSimulation simulation)
        {
            for (var distance = 1; distance <= 4; distance++)
            {
                var cell = simulation.Player.Position.Step(simulation.Player.Facing, distance);
                if (!simulation.Grid.IsInBounds(cell) || !simulation.Grid.IsWalkable(cell)) return null;
                var actor = simulation.Grid.GetActorAt(cell);
                if (actor != null && actor.Kind != ActorKind.Player && actor.IsAlive) return actor;
            }

            return null;
        }

        private static int AdjacentEnemyCount(GameSimulation simulation) =>
            simulation.Grid.Actors.Count(actor =>
                actor.Kind != ActorKind.Player &&
                actor.IsAlive &&
                Math.Abs(actor.Position.X - simulation.Player.Position.X) <= 1 &&
                Math.Abs(actor.Position.Y - simulation.Player.Position.Y) <= 1);

        private static string FormatSkillId(string skillId) =>
            string.IsNullOrEmpty(skillId) ? "Empty" : skillId;

    }

    public readonly struct ActiveSkillSlotState
    {
        public readonly int SlotIndex;
        public readonly string SkillId;
        public readonly string Label;
        public readonly bool Interactable;
        public readonly bool Selected;
        public readonly string DisabledReason;
        public readonly SkillTargeting Targeting;

        public string DisplayName => string.IsNullOrEmpty(SkillId) ? $"Slot {SlotIndex + 1}" : SkillId;

        public ActiveSkillSlotState(
            int slotIndex,
            string skillId,
            string label,
            bool interactable,
            bool selected,
            string disabledReason,
            SkillTargeting targeting)
        {
            SlotIndex = slotIndex;
            SkillId = skillId;
            Label = label;
            Interactable = interactable;
            Selected = selected;
            DisabledReason = disabledReason;
            Targeting = targeting;
        }

        public static ActiveSkillSlotState Empty(int slotIndex, string reason) =>
            new ActiveSkillSlotState(
                slotIndex,
                null,
                $"{slotIndex + 1}. Empty",
                false,
                false,
                reason,
                SkillTargeting.None);

        public static ActiveSkillSlotState Disabled(int slotIndex, string label, string reason) =>
            new ActiveSkillSlotState(
                slotIndex,
                null,
                label,
                false,
                false,
                reason,
                SkillTargeting.None);
    }
}
