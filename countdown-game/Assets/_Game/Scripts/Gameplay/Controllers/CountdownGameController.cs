using System.Collections.Generic;
using System.Linq;
using CountdownGame.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CountdownGame.Unity
{
    public sealed class CountdownGameController : MonoBehaviour, ISimulationEventSink
    {
        [Header("Model")]
        [SerializeField] private int runSeed = 12345;
        [SerializeField] private int initialWc = 12;
        [SerializeField] private Vector2Int gridSize = new Vector2Int(9, 7);
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private EnemyConfig enemyConfig;
        [SerializeField] private SpawnConfig spawnConfig;
        [SerializeField] private EnemyHealthConfig enemyHealthConfig;
        [SerializeField] private SkillCatalog skillCatalog;
        [SerializeField] private SkillDropConfig skillDropConfig;

        [Header("Scene")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private GridActorView[] actorViews;
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private TelegraphView telegraphView;
        [SerializeField] private GroundSkillItemView groundSkillItemPrefab;
        [SerializeField] private PlayerMoveHighlightView playerMoveHighlightView;

        private readonly Dictionary<int, GridActorView> _views = new Dictionary<int, GridActorView>();
        private readonly Dictionary<int, GroundSkillItemView> _groundItemViews =
            new Dictionary<int, GroundSkillItemView>();
        private GameSimulation _simulation;

        public GameSimulation Simulation => _simulation;
        public Tilemap TerrainTilemap => terrainTilemap;
        public int TargetingSkillSlot { get; private set; } = -1;

        private void Awake()
        {
            BuildSimulation();
        }

        public void BuildSimulation()
        {
            var grid = new GridState(gridSize.x, gridSize.y, terrainTilemap == null);
            if (terrainTilemap != null)
            {
                for (var y = 0; y < gridSize.y; y++)
                for (var x = 0; x < gridSize.x; x++)
                    grid.SetWalkable(new GridCoord(x, y), terrainTilemap.HasTile(new Vector3Int(x, y, 0)));
            }

            _views.Clear();
            foreach (var view in actorViews ?? FindObjectsByType<GridActorView>())
            {
                var actor = new ActorState(
                    view.actorId, view.spawnId, view.actorKind,
                    new GridCoord(view.initialCell.x, view.initialCell.y));
                grid.AddActor(actor);
                _views.Add(actor.Id, view);
                view.Present(actor.Position);
            }

            if (spawnPointsRoot != null)
            {
                foreach (Transform child in spawnPointsRoot)
                    grid.AddSpawnPoint(new GridCoord(
                        Mathf.FloorToInt(child.position.x), Mathf.FloorToInt(child.position.y)));
            }

            var player = grid.Actors.Single(a => a.Kind == ActorKind.Player);
            _simulation = new GameSimulation(
                grid,
                player,
                new RunState(initialWc),
                runSeed,
                movementConfig != null ? movementConfig.ToModel() : new MovementTuning(),
                enemyConfig != null ? enemyConfig.ToModel() : new EnemyTuning(),
                spawnConfig != null ? spawnConfig.ToModel() : new SpawnConfiguration(),
                this,
                skillDropConfiguration: skillDropConfig != null ? skillDropConfig.ToModel() : null,
                enemyHealthConfiguration: enemyHealthConfig != null ? enemyHealthConfig.ToModel() : null,
                skillCatalog: skillCatalog != null ? skillCatalog.ToModel() : null);
            EnsurePlayerMoveHighlightView();
            _simulation.StartBeat();
        }

        public MovementResult Move(GridDirection direction) => _simulation.TryPlayerMove(direction);
        public bool Attack(Vector2Int cell) =>
            _simulation.TryPlayerAttack(new GridCoord(cell.x, cell.y));
        public SkillUseResult Dash()
        {
            var slot = _simulation.Skills.ActiveSlots
                .Select((skillId, index) => new { skillId, index })
                .FirstOrDefault(value => value.skillId == SkillIds.Dash);
            return _simulation.TryUseSkill(slot != null ? slot.index : -1);
        }
        public SkillUseResult UseSkill(int slotIndex) =>
            _simulation.TryUseSkill(slotIndex);
        public SkillUseResult UseSkillAt(int slotIndex, Vector2Int cell) =>
            _simulation.TryUseSkill(slotIndex, new SkillTarget(new GridCoord(cell.x, cell.y)));
        public void BeginSkillTarget(int slotIndex) => TargetingSkillSlot = slotIndex;
        public void CancelSkillTarget() => TargetingSkillSlot = -1;
        public PickupResult ResolvePickup(PickupDecisionKind decision, int slotIndex = -1) =>
            _simulation.ResolvePendingPickup(new PickupDecision(decision, slotIndex));

        public void EndBeat(bool freeze = false)
        {
            Debug.Log($"[Countdown] End Turn requested for beat {_simulation.Run.BeatNumber}.");
            _simulation.EndPlayerPhase(freeze);
            Debug.Log($"[Countdown] End Turn resolved at phase {_simulation.Phase}.");
            if (_simulation.Phase == BeatPhase.NotStarted) _simulation.StartBeat();
        }

        public void MovementResolved(MovementResult result)
        {
            if (result.Succeeded && _views.TryGetValue(result.ActorId, out var view))
                view.Present(result.Landing);
            RefreshPlayerMoveHighlights();
        }

        public void Hit(int sourceId, int targetId, string cause) =>
            Debug.Log($"[Countdown] Hit {sourceId} -> {targetId} ({cause})");
        public void WcChanged(int previousValue, int currentValue, string cause) =>
            Debug.Log($"[Countdown] WC {previousValue} -> {currentValue} ({cause})");
        public void PressureCreated(int amount, MovementKind kind) =>
            Debug.Log($"[Countdown] {kind} pressure +{amount}");
        public void OverlayLanded(int actorId, GridCoord cell, OverlayKind kind) =>
            Debug.Log($"[Countdown] Actor {actorId} landed on {kind} at {cell}");
        public void EnemyDied(int enemyId)
        {
            if (_views.TryGetValue(enemyId, out var view))
                view.gameObject.SetActive(false);
            RefreshPlayerMoveHighlights();
        }
        public void EnemySpawned(ActorState enemy) =>
            Debug.Log($"[Countdown] Spawned {enemy.Kind} #{enemy.Id} at {enemy.Position}");

        public void TelegraphChanged(int enemyId, string kind, bool active, bool paused)
        {
            if (telegraphView == null) return;
            if (!active)
            {
                telegraphView.Hide();
                return;
            }
            GridCoord? landing = kind == "Jump"
                ? _simulation?.Jumper.GetLock(enemyId)?.Landing
                : _simulation?.Thrower.GetLock(enemyId)?.Landing;
            if (landing.HasValue)
                telegraphView.Show(new Vector2Int(landing.Value.X, landing.Value.Y), paused);
        }

        public void EnemyDecisionResolved(EnemyDecision decision) =>
            Debug.Log($"[Countdown] Enemy {decision.EnemyId}: {decision.Kind}");

        public void PhaseChanged(BeatPhase phase)
        {
            Debug.Log($"[Countdown] Phase: {phase}");
            RefreshPlayerMoveHighlights();
        }

        public void ManaChanged(int previousValue, int currentValue, string cause) =>
            Debug.Log($"[Countdown] Mana {previousValue} -> {currentValue} ({cause})");
        public void SkillUsed(int slotIndex, string skillId, int manaSpent)
        {
            Debug.Log($"[Countdown] Used {skillId} from slot {slotIndex} for {manaSpent} mana");
            RefreshPlayerMoveHighlights();
        }
        public void SkillRejected(int slotIndex, string skillId, SkillUseFailureReason reason) =>
            Debug.Log($"[Countdown] Rejected {skillId ?? "empty"} in slot {slotIndex}: {reason}");
        public void SkillSlotChanged(int slotIndex, string skillId, bool passive) =>
            Debug.Log($"[Countdown] {(passive ? "Passive" : "Active")} slot {slotIndex}: {skillId ?? "empty"}");
        public void SkillDropped(GroundSkillItem item) =>
            PresentGroundItem(item);
        public void SkillGroundItemRemoved(GroundSkillItem item)
        {
            if (_groundItemViews.TryGetValue(item.Id, out var view))
            {
                _groundItemViews.Remove(item.Id);
                Destroy(view.gameObject);
            }
            Debug.Log($"[Countdown] Removed ground item {item.SkillId} at {item.Cell}");
        }
        public void PickupPending(string skillId) =>
            Debug.Log($"[Countdown] Pickup pending: {skillId}");
        public void PickupResolved(string skillId, PickupDecisionKind decision) =>
            Debug.Log($"[Countdown] Pickup {skillId}: {decision}");
        public void DamageApplied(int sourceId, int targetId, int amount, string cause) =>
            Debug.Log($"[Countdown] Damage {sourceId} -> {targetId}: {amount} ({cause})");
        public void WardChanged(bool armed) =>
            Debug.Log($"[Countdown] Ward armed: {armed}");
        public void FreezeChanged(bool armed) =>
            Debug.Log($"[Countdown] Freeze armed: {armed}");

        private void EnsurePlayerMoveHighlightView()
        {
            if (playerMoveHighlightView == null)
                playerMoveHighlightView = GetComponent<PlayerMoveHighlightView>();
            if (playerMoveHighlightView == null)
                playerMoveHighlightView = gameObject.AddComponent<PlayerMoveHighlightView>();
            playerMoveHighlightView.Initialize(terrainTilemap);
        }

        private void RefreshPlayerMoveHighlights()
        {
            if (playerMoveHighlightView == null || _simulation == null) return;
            playerMoveHighlightView.Present(_simulation.GetAvailablePlayerMoveCells());
        }

        private void PresentGroundItem(GroundSkillItem item)
        {
            Debug.Log($"[Countdown] Dropped {item.SkillId} at {item.Cell}");
            if (groundSkillItemPrefab == null) return;
            var view = Instantiate(groundSkillItemPrefab, transform);
            var definition = skillCatalog != null ? skillCatalog.Find(item.SkillId) : null;
            var tileCell = new Vector3Int(item.Cell.X, item.Cell.Y, 0);
            var cellCenter = terrainTilemap != null
                ? terrainTilemap.GetCellCenterWorld(tileCell)
                : new Vector3(item.Cell.X + 0.5f, item.Cell.Y + 0.5f, 0f);
            view.Present(
                item.Id,
                item.SkillId,
                new Vector2Int(item.Cell.X, item.Cell.Y),
                cellCenter,
                definition != null ? definition.icon : null);
            _groundItemViews[item.Id] = view;
        }
    }
}
