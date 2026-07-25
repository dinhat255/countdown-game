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

        [Header("Scene")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private GridActorView[] actorViews;
        [SerializeField] private Transform spawnPointsRoot;
        [SerializeField] private TelegraphView telegraphView;

        private readonly Dictionary<int, GridActorView> _views = new Dictionary<int, GridActorView>();
        private GameSimulation _simulation;

        public GameSimulation Simulation => _simulation;

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
                this);
            _simulation.StartBeat();
        }

        public MovementResult Move(GridDirection direction) => _simulation.TryPlayerMove(direction);
        public MovementResult Dash() => _simulation.TryPlayerDash();

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
        }

        public void Hit(int sourceId, int targetId, string cause) =>
            Debug.Log($"[Countdown] Hit {sourceId} -> {targetId} ({cause})");
        public void WcChanged(int previousValue, int currentValue, string cause) =>
            Debug.Log($"[Countdown] WC {previousValue} -> {currentValue} ({cause})");
        public void PressureCreated(int amount, MovementKind kind) =>
            Debug.Log($"[Countdown] {kind} pressure +{amount}");
        public void OverlayLanded(int actorId, GridCoord cell, OverlayKind kind) =>
            Debug.Log($"[Countdown] Actor {actorId} landed on {kind} at {cell}");
        public void EnemyDied(int enemyId) { }
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

        public void PhaseChanged(BeatPhase phase) =>
            Debug.Log($"[Countdown] Phase: {phase}");
    }
}
