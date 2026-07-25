using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    public static class SkillIds
    {
        public const string Dash = "dash";
        public const string Snipe = "snipe";
        public const string Ward = "ward";
        public const string Bomb = "bomb";
        public const string Shockwave = "shockwave";
        public const string Freeze = "freeze";
        public const string WcDampener = "wc-dampener";
        public const string DamageUp = "damage-up";
        public const string Meditation = "meditation";
    }

    [Serializable]
    public sealed class SkillDefinitionModel
    {
        public string Id;
        public SkillCategory Category;
        public int Level;
        public int ManaCost;
        public SkillTargeting Targeting;
        public string Description;

        public SkillDefinitionModel(
            string id,
            SkillCategory category,
            int level,
            int manaCost,
            SkillTargeting targeting,
            string description)
        {
            Id = id;
            Category = category;
            Level = level;
            ManaCost = manaCost;
            Targeting = targeting;
            Description = description;
        }
    }

    public static class StarterSkillCatalog
    {
        public static readonly IReadOnlyList<SkillDefinitionModel> All =
            new[]
            {
                new SkillDefinitionModel(SkillIds.Dash, SkillCategory.Active, 1, 1,
                    SkillTargeting.Facing, "Dash three cells along facing."),
                new SkillDefinitionModel(SkillIds.Snipe, SkillCategory.Active, 1, 2,
                    SkillTargeting.Facing, "Deal 3 damage to the first enemy in four facing cells."),
                new SkillDefinitionModel(SkillIds.Ward, SkillCategory.Active, 1, 2,
                    SkillTargeting.None, "Negate the next hit-based WC increase this beat."),
                new SkillDefinitionModel(SkillIds.Bomb, SkillCategory.Active, 2, 2,
                    SkillTargeting.Cell, "Place a two-beat fuse bomb within range two."),
                new SkillDefinitionModel(SkillIds.Shockwave, SkillCategory.Active, 2, 3,
                    SkillTargeting.None, "Deal 2 damage to adjacent enemies."),
                new SkillDefinitionModel(SkillIds.Freeze, SkillCategory.Active, 3, 4,
                    SkillTargeting.None, "Skip the immediately following Enemy Phase."),
                new SkillDefinitionModel(SkillIds.WcDampener, SkillCategory.Passive, 1, 0,
                    SkillTargeting.None, "Reduce each hit-based WC penalty by one."),
                new SkillDefinitionModel(SkillIds.DamageUp, SkillCategory.Passive, 2, 0,
                    SkillTargeting.None, "Player attacks and offensive skills deal one more damage."),
                new SkillDefinitionModel(SkillIds.Meditation, SkillCategory.Passive, 3, 0,
                    SkillTargeting.None, "Restore three mana after a no-move beat.")
            };

        public static SkillDefinitionModel Get(string id) =>
            All.FirstOrDefault(skill => skill.Id == id);
    }

    [Serializable]
    public sealed class SkillDropConfiguration
    {
        public int IntervalBeats = 3;
        public int GroundItemCap = 2;
        public int[][] PhaseLevelWeights =
        {
            new[] { 60, 30, 10 },
            new[] { 30, 50, 20 },
            new[] { 20, 35, 45 }
        };
    }

    [Serializable]
    public sealed class EnemyHealthConfiguration
    {
        public int RunnerMaximumHealth = 3;
        public int JumperMaximumHealth = 4;
        public int ThrowerMaximumHealth = 5;

        public int MaximumFor(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.Runner: return RunnerMaximumHealth;
                case ActorKind.Jumper: return JumperMaximumHealth;
                case ActorKind.Thrower: return ThrowerMaximumHealth;
                default: return 0;
            }
        }
    }

    [Serializable]
    public sealed class GroundSkillItem
    {
        public int Id { get; }
        public string SkillId { get; }
        public GridCoord Cell { get; }

        public GroundSkillItem(int id, string skillId, GridCoord cell)
        {
            Id = id;
            SkillId = skillId;
            Cell = cell;
        }
    }

    [Serializable]
    public sealed class PlacedSkillBomb
    {
        public int PlacementId { get; }
        public GridCoord Cell { get; }
        public int FuseRemaining { get; internal set; }

        public PlacedSkillBomb(int placementId, GridCoord cell, int fuseRemaining)
        {
            PlacementId = placementId;
            Cell = cell;
            FuseRemaining = fuseRemaining;
        }
    }

    public sealed class SkillInventory
    {
        private readonly string[] _activeSlots = new string[3];

        public IReadOnlyList<string> ActiveSlots => _activeSlots;
        public string PassiveSlot { get; private set; }
        public string PendingSkillId { get; private set; }
        public SkillCategory PendingCategory { get; private set; }
        public bool HasPendingPickup => !string.IsNullOrEmpty(PendingSkillId);

        public string GetActive(int slotIndex) =>
            slotIndex >= 0 && slotIndex < _activeSlots.Length ? _activeSlots[slotIndex] : null;

        public bool SetActive(int slotIndex, string skillId)
        {
            if (slotIndex < 0 || slotIndex >= _activeSlots.Length) return false;
            _activeSlots[slotIndex] = skillId;
            return true;
        }

        public void SetPassive(string skillId) => PassiveSlot = skillId;

        public string ConsumeActive(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _activeSlots.Length) return null;
            var skill = _activeSlots[slotIndex];
            _activeSlots[slotIndex] = null;
            return skill;
        }

        public int FirstEmptyActiveSlot()
        {
            for (var i = 0; i < _activeSlots.Length; i++)
                if (string.IsNullOrEmpty(_activeSlots[i])) return i;
            return -1;
        }

        public bool TryEquipOrQueue(string skillId, SkillCategory? categoryOverride = null)
        {
            var definition = StarterSkillCatalog.Get(skillId);
            if (definition == null && !categoryOverride.HasValue) return false;
            var category = categoryOverride ?? definition.Category;
            if (category == SkillCategory.Active)
            {
                var empty = FirstEmptyActiveSlot();
                if (empty >= 0)
                {
                    _activeSlots[empty] = skillId;
                    return true;
                }
            }
            else if (string.IsNullOrEmpty(PassiveSlot))
            {
                PassiveSlot = skillId;
                return true;
            }

            PendingSkillId = skillId;
            PendingCategory = category;
            return false;
        }

        public string AutoFillPendingActive()
        {
            var empty = FirstEmptyActiveSlot();
            if (!HasPendingPickup || PendingCategory != SkillCategory.Active || empty < 0)
                return null;
            var skill = PendingSkillId;
            _activeSlots[empty] = skill;
            PendingSkillId = null;
            return skill;
        }

        public PickupResult ResolvePending(PickupDecision decision)
        {
            if (!HasPendingPickup)
                return PickupResult.Rejected(PickupFailureReason.NoPendingPickup);

            var skill = PendingSkillId;
            if (decision.Kind == PickupDecisionKind.Discard)
            {
                PendingSkillId = null;
                return PickupResult.Success(skill);
            }

            if (PendingCategory == SkillCategory.Active)
            {
                if (decision.Kind != PickupDecisionKind.ReplaceActive)
                    return PickupResult.Rejected(PickupFailureReason.InvalidDecision);
                if (decision.SlotIndex < 0 || decision.SlotIndex >= _activeSlots.Length)
                    return PickupResult.Rejected(PickupFailureReason.InvalidSlot);
                _activeSlots[decision.SlotIndex] = skill;
            }
            else
            {
                if (decision.Kind != PickupDecisionKind.ReplacePassive)
                    return PickupResult.Rejected(PickupFailureReason.InvalidDecision);
                PassiveSlot = skill;
            }

            PendingSkillId = null;
            return PickupResult.Success(skill);
        }
    }

    public sealed class SkillDropSystem
    {
        private const int DropRandomChannel = 19088743;
        private readonly GridState _grid;
        private readonly SkillDropConfiguration _configuration;
        private readonly IReadOnlyList<SkillDefinitionModel> _catalog;
        private readonly ISimulationEventSink _events;
        private readonly List<GroundSkillItem> _items = new List<GroundSkillItem>();
        private int _nextItemId;

        public IReadOnlyList<GroundSkillItem> Items => _items;

        public SkillDropSystem(
            GridState grid,
            SkillDropConfiguration configuration = null,
            IReadOnlyList<SkillDefinitionModel> catalog = null,
            ISimulationEventSink events = null,
            int nextItemId = 1)
        {
            _grid = grid;
            _configuration = configuration ?? new SkillDropConfiguration();
            _catalog = catalog ?? StarterSkillCatalog.All;
            _events = events ?? NullSimulationEventSink.Instance;
            _nextItemId = nextItemId;
        }

        public GroundSkillItem TryDrop(int runSeed, int completedBeat, int phase, bool victory)
        {
            if (victory || completedBeat <= 0 ||
                completedBeat % Math.Max(1, _configuration.IntervalBeats) != 0 ||
                _items.Count >= _configuration.GroundItemCap)
                return null;

            var validCells = ValidCells().ToArray();
            if (validCells.Length == 0) return null;

            var weights = _configuration.PhaseLevelWeights[
                Math.Max(0, Math.Min(_configuration.PhaseLevelWeights.Length - 1, phase - 1))];
            var total = weights.Sum();
            if (total <= 0) return null;
            var levelRoll = new SeededRandomContext(
                runSeed, completedBeat, DropRandomChannel, 0).Index(total);
            var level = 1;
            var accumulated = weights[0];
            while (level < weights.Length && levelRoll >= accumulated)
                accumulated += weights[level++];

            var skills = _catalog.Where(skill => skill.Level == level).OrderBy(skill => skill.Id).ToArray();
            if (skills.Length == 0) return null;
            var chosenSkill = skills[new SeededRandomContext(
                runSeed, completedBeat, DropRandomChannel, 1).Index(skills.Length)];
            var cell = validCells[new SeededRandomContext(
                runSeed, completedBeat, DropRandomChannel, 2).Index(validCells.Length)];
            var item = new GroundSkillItem(_nextItemId++, chosenSkill.Id, cell);
            _items.Add(item);
            _grid.AddOverlay(cell, OverlayKind.Item);
            _events.SkillDropped(item);
            return item;
        }

        public GroundSkillItem CollectAt(GridCoord cell)
        {
            var item = _items.FirstOrDefault(candidate => candidate.Cell == cell);
            if (item == null) return null;
            _items.Remove(item);
            _grid.RemoveOverlay(cell, OverlayKind.Item);
            _events.SkillGroundItemRemoved(item);
            return item;
        }

        private IEnumerable<GridCoord> ValidCells()
        {
            for (var y = 0; y < _grid.Height; y++)
            for (var x = 0; x < _grid.Width; x++)
            {
                var cell = new GridCoord(x, y);
                if (_grid.IsWalkable(cell) &&
                    !_grid.IsSpawnPoint(cell) &&
                    !_grid.IsActorOccupied(cell) &&
                    _grid.GetOverlays(cell).Count == 0 &&
                    _items.All(item => item.Cell != cell))
                    yield return cell;
            }
        }
    }
}
