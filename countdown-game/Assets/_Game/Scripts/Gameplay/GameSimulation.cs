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
        private readonly SkillDropSystem _skillDrops;
        private readonly EnemyHealthConfiguration _enemyHealth;
        private readonly IReadOnlyDictionary<string, SkillDefinitionModel> _skillCatalog;
        private readonly List<PlacedSkillBomb> _bombs = new List<PlacedSkillBomb>();
        private int _nextBombPlacementId = 1;
        private bool _wardArmed;
        private bool _freezeArmed;
        private bool _freezeUsedThisBeat;

        public GridState Grid { get; }
        public RunState Run { get; }
        public ActorState Player { get; }
        public BeatPhase Phase => Run.Phase;
        public JumperBrain Jumper => _jumper;
        public ThrowerBrain Thrower => _thrower;
        public SpawnSystem Spawns => _spawns;
        public SkillDropSystem SkillDrops => _skillDrops;
        public SkillInventory Skills { get; } = new SkillInventory();
        public IReadOnlyList<PlacedSkillBomb> Bombs => _bombs;
        public bool WardArmed => _wardArmed;
        public bool FreezeArmed => _freezeArmed;
        public int PredictedNoMoveManaRestoration =>
            Skills.PassiveSlot == SkillIds.Meditation ? 3 : 2;

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
            int nextSpawnId = 1000,
            SkillDropConfiguration skillDropConfiguration = null,
            EnemyHealthConfiguration enemyHealthConfiguration = null,
            IReadOnlyList<SkillDefinitionModel> skillCatalog = null)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Run = run ?? throw new ArgumentNullException(nameof(run));
            _runSeed = runSeed;
            _events = events ?? NullSimulationEventSink.Instance;
            _enemyHealth = enemyHealthConfiguration ?? new EnemyHealthConfiguration();
            _skillCatalog = (skillCatalog ?? StarterSkillCatalog.All)
                .GroupBy(skill => skill.Id)
                .ToDictionary(group => group.Key, group => group.First());
            _movementTuning = movementTuning ?? new MovementTuning();
            enemyTuning = enemyTuning ?? new EnemyTuning();
            _movement = new MovementResolver(grid, _events);
            _jumper = new JumperBrain(enemyTuning.JumperDistance, enemyTuning.ShockwaveRadius);
            _thrower = new ThrowerBrain(
                enemyTuning.ThrowerPickupRange, enemyTuning.ThrowerRange, enemyTuning.ThrowImpactRadius);
            _spawns = new SpawnSystem(
                grid, spawnConfiguration ?? new SpawnConfiguration(), nextActorId, nextSpawnId, _events);
            _skillDrops = new SkillDropSystem(
                grid, skillDropConfiguration, skillCatalog, _events);
            foreach (var enemy in Grid.Actors.Where(IsEnemy))
                enemy.SetMaximumHealth(_enemyHealth.MaximumFor(enemy.Kind));
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
            _freezeUsedThisBeat = false;
            if (_wardArmed)
            {
                _wardArmed = false;
                _events.WardChanged(false);
            }
            foreach (var actor in Grid.Actors) actor.ResetForBeat();
            SetPhase(BeatPhase.Player);
        }

        public IReadOnlyList<GridCoord> GetAvailablePlayerMoveCells()
        {
            if (Phase != BeatPhase.Player || !Player.IsAlive || Player.SelfMovedThisBeat)
                return Array.Empty<GridCoord>();

            return GridDirections.Cardinal
                .Select(direction => Player.Position.Step(direction))
                .Where(cell =>
                    Grid.IsWalkable(cell) &&
                    !Grid.IsActorOccupied(cell, Player.Id))
                .ToArray();
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
                CollectGroundItem(result.Landing);
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
                CollectGroundItem(result.Landing);
            }
            return result;
        }

        public bool EquipActiveSkill(int slotIndex, string skillId)
        {
            var definition = FindSkill(skillId);
            if (definition == null || definition.Category != SkillCategory.Active ||
                !Skills.SetActive(slotIndex, skillId))
                return false;
            _events.SkillSlotChanged(slotIndex, skillId, false);
            return true;
        }

        public bool EquipPassiveSkill(string skillId)
        {
            var definition = FindSkill(skillId);
            if (definition == null || definition.Category != SkillCategory.Passive) return false;
            Skills.SetPassive(skillId);
            _events.SkillSlotChanged(0, skillId, true);
            return true;
        }

        public SkillUseResult TryUseSkill(int slotIndex, SkillTarget target = default)
        {
            var skillId = Skills.GetActive(slotIndex);
            if (Phase != BeatPhase.Player)
                return RejectSkill(slotIndex, skillId, SkillUseFailureReason.WrongPhase);
            if (slotIndex < 0 || slotIndex >= Skills.ActiveSlots.Count)
                return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InvalidSlot);
            if (string.IsNullOrEmpty(skillId))
                return RejectSkill(slotIndex, null, SkillUseFailureReason.EmptySlot);

            var definition = FindSkill(skillId);
            if (definition == null || definition.Category != SkillCategory.Active)
                return RejectSkill(slotIndex, skillId, SkillUseFailureReason.PassiveSkill);
            if (Run.CurrentMana < definition.ManaCost)
                return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InsufficientMana);

            ActorState singleTarget = null;
            ActorState[] areaTargets = null;
            switch (skillId)
            {
                case SkillIds.Dash:
                    var dashFailure = ValidateDash();
                    if (dashFailure != MovementFailureReason.None)
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.MovementRejected);
                    break;
                case SkillIds.Snipe:
                    singleTarget = FindSnipeTarget();
                    if (singleTarget == null)
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InvalidTarget);
                    break;
                case SkillIds.Ward:
                    if (_wardArmed)
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.EffectAlreadyActive);
                    break;
                case SkillIds.Bomb:
                    if (!target.Cell.HasValue || !IsValidBombCell(target.Cell.Value))
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InvalidTarget);
                    break;
                case SkillIds.Shockwave:
                    areaTargets = AdjacentEnemies().ToArray();
                    if (areaTargets.Length == 0)
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InvalidTarget);
                    break;
                case SkillIds.Freeze:
                    if (_freezeUsedThisBeat || _freezeArmed)
                        return RejectSkill(slotIndex, skillId, SkillUseFailureReason.EffectAlreadyActive);
                    break;
                default:
                    return RejectSkill(slotIndex, skillId, SkillUseFailureReason.InvalidTarget);
            }

            var previousMana = Run.CurrentMana;
            Run.TrySpendMana(definition.ManaCost);
            _events.ManaChanged(previousMana, Run.CurrentMana, $"Skill:{skillId}");

            switch (skillId)
            {
                case SkillIds.Dash:
                    TryPlayerDash();
                    break;
                case SkillIds.Snipe:
                    DamageEnemy(Player.Id, singleTarget, PlayerDamage(3), "Snipe");
                    break;
                case SkillIds.Ward:
                    _wardArmed = true;
                    _events.WardChanged(true);
                    break;
                case SkillIds.Bomb:
                    var bomb = new PlacedSkillBomb(_nextBombPlacementId++, target.Cell.Value, 2);
                    _bombs.Add(bomb);
                    Grid.AddOverlay(bomb.Cell, OverlayKind.Hazard);
                    break;
                case SkillIds.Shockwave:
                    foreach (var enemy in areaTargets.OrderBy(actor => actor.SpawnId))
                        DamageEnemy(Player.Id, enemy, PlayerDamage(2), "Shockwave");
                    break;
                case SkillIds.Freeze:
                    _freezeArmed = true;
                    _freezeUsedThisBeat = true;
                    _events.FreezeChanged(true);
                    break;
            }

            Skills.ConsumeActive(slotIndex);
            _events.SkillSlotChanged(slotIndex, null, false);
            _events.SkillUsed(slotIndex, skillId, definition.ManaCost);
            var autoFillSlot = Skills.FirstEmptyActiveSlot();
            var autoFilled = Skills.AutoFillPendingActive();
            if (autoFilled != null)
            {
                _events.SkillSlotChanged(autoFillSlot, autoFilled, false);
                _events.PickupResolved(autoFilled, PickupDecisionKind.ReplaceActive);
            }
            return SkillUseResult.Success(slotIndex, skillId, definition.ManaCost);
        }

        public PickupResult ResolvePendingPickup(PickupDecision decision)
        {
            var skillId = Skills.PendingSkillId;
            var result = Skills.ResolvePending(decision);
            if (result.Succeeded)
            {
                _events.PickupResolved(skillId, decision.Kind);
                if (decision.Kind == PickupDecisionKind.ReplaceActive)
                    _events.SkillSlotChanged(decision.SlotIndex, skillId, false);
                else if (decision.Kind == PickupDecisionKind.ReplacePassive)
                    _events.SkillSlotChanged(0, skillId, true);
            }
            return result;
        }

        public int ResolvePlayerHit(int sourceId, string cause, int wcPenalty = 1)
        {
            _events.Hit(sourceId, Player.Id, cause);
            var applied = Math.Max(0, wcPenalty);
            if (_wardArmed)
            {
                applied = 0;
                _wardArmed = false;
                _events.WardChanged(false);
            }
            else if (Skills.PassiveSlot == SkillIds.WcDampener)
            {
                applied = Math.Max(0, applied - 1);
            }

            if (applied > 0)
            {
                var previous = Run.Wc;
                Run.ChangeWc(applied);
                _events.WcChanged(previous, Run.Wc, cause);
            }
            return applied;
        }

        public void EndPlayerPhase(bool freezeEnemyPhase = false)
        {
            if (Phase != BeatPhase.Player) throw new InvalidOperationException("Not in Player Phase.");

            SetPhase(BeatPhase.Enemy);
            var skipEnemyPhase = freezeEnemyPhase || _freezeArmed;
            if (skipEnemyPhase)
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
                        Grid, _movement, Player, _runSeed, Run.BeatNumber, _events,
                        (sourceId, cause) => ResolvePlayerHit(sourceId, cause));
                    BrainFor(enemy.Kind).Act(enemy, context);
                }
            }
            if (_freezeArmed)
            {
                _freezeArmed = false;
                _events.FreezeChanged(false);
            }

            SetPhase(BeatPhase.EndOfBeat);
            if (!Player.PlayerMovedThisBeat)
            {
                Run.StandingStreak++;
                var previous = Run.Wc;
                Run.ChangeWc(-1);
                _events.WcChanged(previous, Run.Wc, "NoMove");
                var previousMana = Run.CurrentMana;
                Run.RestoreMana(PredictedNoMoveManaRestoration);
                if (previousMana != Run.CurrentMana)
                    _events.ManaChanged(previousMana, Run.CurrentMana, "NoMove");
            }
            else
            {
                Run.StandingStreak = 0;
            }

            TickSkillBombs();

            if (Run.Victory)
            {
                SetPhase(BeatPhase.Victory);
                return;
            }

            var spawned = _spawns.Tick(
                Player, Run.ProgressPhase, Run.MovementPressure, _runSeed, Run.BeatNumber, false);
            if (spawned != null)
                spawned.SetMaximumHealth(_enemyHealth.MaximumFor(spawned.Kind));
            _skillDrops.TryDrop(_runSeed, Run.BeatNumber, Run.ProgressPhase, false);
            SetPhase(BeatPhase.NotStarted);
        }

        private SkillUseResult RejectSkill(
            int slotIndex, string skillId, SkillUseFailureReason reason)
        {
            _events.SkillRejected(slotIndex, skillId, reason);
            return SkillUseResult.Rejected(slotIndex, skillId, reason);
        }

        private SkillDefinitionModel FindSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            return _skillCatalog.TryGetValue(skillId, out var definition) ? definition : null;
        }

        private MovementFailureReason ValidateDash()
        {
            if (Player.SelfMovedThisBeat) return MovementFailureReason.AlreadySelfMoved;
            for (var distance = 1; distance <= _movementTuning.DashDistance; distance++)
            {
                var cell = Player.Position.Step(Player.Facing, distance);
                if (!Grid.IsInBounds(cell)) return MovementFailureReason.OutOfBounds;
                if (!Grid.IsWalkable(cell)) return MovementFailureReason.BlockedTerrain;
                if (distance == _movementTuning.DashDistance &&
                    Grid.IsActorOccupied(cell, Player.Id))
                    return MovementFailureReason.OccupiedLanding;
            }
            return MovementFailureReason.None;
        }

        private ActorState FindSnipeTarget()
        {
            for (var distance = 1; distance <= 4; distance++)
            {
                var cell = Player.Position.Step(Player.Facing, distance);
                if (!Grid.IsInBounds(cell) || !Grid.IsWalkable(cell)) return null;
                var actor = Grid.GetActorAt(cell);
                if (actor != null && IsEnemy(actor)) return actor;
            }
            return null;
        }

        private IEnumerable<ActorState> AdjacentEnemies() =>
            Grid.Actors.Where(actor =>
                IsEnemy(actor) &&
                Math.Abs(actor.Position.X - Player.Position.X) <= 1 &&
                Math.Abs(actor.Position.Y - Player.Position.Y) <= 1);

        private bool IsValidBombCell(GridCoord cell) =>
            Player.Position.ManhattanDistance(cell) <= 2 &&
            Grid.IsWalkable(cell) &&
            !Grid.IsActorOccupied(cell) &&
            Grid.GetOverlays(cell).Count == 0;

        private int PlayerDamage(int baseDamage) =>
            baseDamage + (Skills.PassiveSlot == SkillIds.DamageUp ? 1 : 0);

        public int DamageEnemy(int sourceId, ActorState enemy, int damage, string cause)
        {
            if (enemy == null || !IsEnemy(enemy) || damage <= 0) return 0;
            var applied = enemy.ApplyDamage(damage);
            if (applied <= 0) return 0;
            _events.DamageApplied(sourceId, enemy.Id, applied, cause);
            if (!enemy.IsAlive) _events.EnemyDied(enemy.Id);
            return applied;
        }

        private void TickSkillBombs()
        {
            var due = new List<PlacedSkillBomb>();
            foreach (var bomb in _bombs.OrderBy(value => value.PlacementId))
            {
                bomb.FuseRemaining--;
                if (bomb.FuseRemaining <= 0) due.Add(bomb);
            }

            foreach (var bomb in due)
            {
                foreach (var enemy in Grid.Actors
                             .Where(IsEnemy)
                             .Where(actor =>
                                 Math.Abs(actor.Position.X - bomb.Cell.X) <= 1 &&
                                 Math.Abs(actor.Position.Y - bomb.Cell.Y) <= 1)
                             .OrderBy(actor => actor.SpawnId)
                             .ToArray())
                    DamageEnemy(Player.Id, enemy, PlayerDamage(2), "Bomb");
                Grid.RemoveOverlay(bomb.Cell, OverlayKind.Hazard);
                _bombs.Remove(bomb);
            }
        }

        private void CollectGroundItem(GridCoord landing)
        {
            var item = _skillDrops.CollectAt(landing);
            if (item == null) return;
            var definition = FindSkill(item.SkillId);
            var emptyActiveSlot = Skills.FirstEmptyActiveSlot();
            var equipped = Skills.TryEquipOrQueue(item.SkillId, definition.Category);
            if (equipped)
            {
                var slot = definition.Category == SkillCategory.Active ? emptyActiveSlot : 0;
                _events.SkillSlotChanged(slot, item.SkillId, definition.Category == SkillCategory.Passive);
            }
            else
            {
                _events.PickupPending(item.SkillId);
            }
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
