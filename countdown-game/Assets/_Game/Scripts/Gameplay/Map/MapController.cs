using Countdown.Gameplay.Map.Config;
using CountdownGame.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Countdown.Gameplay.Map
{
    public sealed class MapController : MonoBehaviour
    {
        [SerializeField] private MapGridConfig gridConfig;
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool runSmokeHarnessOnStart = true;
        [SerializeField] private bool drawDebugGizmos = true;

        private readonly GridPosition[] neighborScratch = new GridPosition[4];

        public MapGrid Grid { get; private set; }
        public bool IsReady => Grid != null;

        public bool TryBuildCoreGridState(out GridState gridState, out string error)
        {
            gridState = null;
            error = string.Empty;

            if (Grid == null && !BuildGrid())
            {
                error = $"{nameof(MapController)} could not build {nameof(MapGrid)}.";
                return false;
            }

            MapGridSettings settings = Grid.Settings;
            gridState = new GridState(settings.Width, settings.Height, false);

            for (int y = settings.MinPosition.Y; y <= settings.MaxPosition.Y; y++)
            {
                for (int x = settings.MinPosition.X; x <= settings.MaxPosition.X; x++)
                {
                    GridPosition mapPosition = new GridPosition(x, y);
                    gridState.SetWalkable(MapToCoreCoord(mapPosition), Grid.IsWalkable(mapPosition));
                }
            }

            return true;
        }

        public bool TryWorldToCoreCoord(Vector3 worldPosition, out GridCoord coord)
        {
            if (Grid == null && !BuildGrid())
            {
                coord = default;
                return false;
            }

            GridPosition mapPosition = Grid.WorldToGrid(worldPosition);

            if (!Grid.IsInsideMap(mapPosition))
            {
                coord = default;
                return false;
            }

            coord = MapToCoreCoord(mapPosition);
            return true;
        }

        public bool TryCoreCoordToWorld(GridCoord coord, out Vector3 worldPosition)
        {
            if (Grid == null && !BuildGrid())
            {
                worldPosition = default;
                return false;
            }

            GridPosition mapPosition = CoreToMapPosition(coord);
            if (!Grid.IsInsideMap(mapPosition))
            {
                worldPosition = default;
                return false;
            }

            worldPosition = Grid.GridToWorld(mapPosition);
            return true;
        }

        public Vector3 CoreCoordToWorld(GridCoord coord)
        {
            if (TryCoreCoordToWorld(coord, out Vector3 worldPosition))
            {
                return worldPosition;
            }

            throw new System.InvalidOperationException($"{nameof(MapController)} could not resolve {coord} to a valid world position.");
        }

        private void Start()
        {
            if (runSmokeHarnessOnStart)
            {
                RunSmokeHarness();
            }

            if (buildOnStart)
            {
                BuildGrid();
            }
        }

        public bool BuildGrid()
        {
            if (gridConfig == null)
            {
                Debug.LogError($"{nameof(MapController)} missing {nameof(gridConfig)}.", this);
                Grid = null;
                return false;
            }

            if (!gridConfig.TryCreateSettings(out MapGridSettings settings, out string error))
            {
                Debug.LogError($"{nameof(MapGridConfig)} is invalid: {error}", this);
                Grid = null;
                return false;
            }

            Grid = new MapGrid(settings);

            for (int y = settings.MinPosition.Y; y <= settings.MaxPosition.Y; y++)
            {
                for (int x = settings.MinPosition.X; x <= settings.MaxPosition.X; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    Grid.SetTerrain(position, ReadTerrain(position, settings));
                }
            }

            return true;
        }

        public bool RunSmokeHarness()
        {
            if (MapGridSmokeHarness.Run(out string error))
            {
                Debug.Log($"{nameof(MapGridSmokeHarness)} passed.", this);
                return true;
            }

            Debug.LogError($"{nameof(MapGridSmokeHarness)} failed: {error}", this);
            return false;
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            EnsureGrid();
            return Grid.WorldToGrid(worldPosition);
        }

        public Vector3 GridToWorld(GridPosition position)
        {
            EnsureGrid();
            return Grid.GridToWorld(position);
        }

        public int GetDebugFourNeighbors(GridPosition position, GridPosition[] results)
        {
            EnsureGrid();
            return Grid.GetFourNeighbors(position, results);
        }

        private GridCoord MapToCoreCoord(GridPosition position)
        {
            MapGridSettings settings = Grid.Settings;
            return new GridCoord(
                position.X - settings.MinPosition.X,
                position.Y - settings.MinPosition.Y);
        }

        private GridPosition CoreToMapPosition(GridCoord coord)
        {
            MapGridSettings settings = Grid.Settings;
            return new GridPosition(
                coord.X + settings.MinPosition.X,
                coord.Y + settings.MinPosition.Y);
        }

        private CellTerrain ReadTerrain(GridPosition position, MapGridSettings settings)
        {
            for (int i = 0; i < settings.TerrainPrecedenceCount; i++)
            {
                CellTerrain terrain = settings.GetTerrainByPrecedence(i);

                if (HasTile(terrain, position))
                {
                    return terrain;
                }
            }

            return CellTerrain.Empty;
        }

        private bool HasTile(CellTerrain terrain, GridPosition position)
        {
            Tilemap tilemap = GetTilemap(terrain);
            return tilemap != null && tilemap.HasTile(position.ToVector3Int());
        }

        private Tilemap GetTilemap(CellTerrain terrain)
        {
            switch (terrain)
            {
                case CellTerrain.Ground:
                    return groundTilemap;
                case CellTerrain.Wall:
                    return wallTilemap;
                case CellTerrain.Obstacle:
                    return obstacleTilemap;
                default:
                    return null;
            }
        }

        private void EnsureGrid()
        {
            if (Grid == null && !BuildGrid())
            {
                throw new System.InvalidOperationException($"{nameof(MapController)} could not build a valid grid.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            if (Grid == null && !Application.isPlaying)
            {
                BuildGrid();
            }

            if (Grid == null)
            {
                return;
            }

            for (int y = Grid.Settings.MinPosition.Y; y <= Grid.Settings.MaxPosition.Y; y++)
            {
                for (int x = Grid.Settings.MinPosition.X; x <= Grid.Settings.MaxPosition.X; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    MapCellFacts facts = Grid.GetCellFacts(position);
                    Gizmos.color = GetGizmoColor(facts);
                    Gizmos.DrawWireCube(Grid.GridToWorld(position), Vector3.one * (Grid.Settings.CellSize * 0.9f));
                }
            }

            int neighborCount = Grid.GetFourNeighbors(new GridPosition(0, 0), neighborScratch);
            Gizmos.color = Color.cyan;

            for (int i = 0; i < neighborCount; i++)
            {
                Gizmos.DrawCube(Grid.GridToWorld(neighborScratch[i]), Vector3.one * (Grid.Settings.CellSize * 0.25f));
            }
        }

        private static Color GetGizmoColor(MapCellFacts facts)
        {
            if (!facts.IsInsideMap)
            {
                return Color.gray;
            }

            switch (facts.Terrain)
            {
                case CellTerrain.Ground:
                    return facts.HasOccupant ? Color.yellow : Color.green;
                case CellTerrain.Wall:
                    return Color.red;
                case CellTerrain.Obstacle:
                    return new Color(1f, 0.5f, 0f);
                default:
                    return Color.gray;
            }
        }
    }
}
