using System.Collections.Generic;
using Countdown.Gameplay.Map;
using UnityEngine;

namespace Countdown.Gameplay.Map.Config
{
    public sealed class MapGridConfig : ScriptableObject
    {
        [Header("Bounds")]
        [SerializeField] private Vector2Int minCell = new Vector2Int(-4, -3);
        [SerializeField] private Vector2Int maxCell = new Vector2Int(4, 3);
        [SerializeField, Min(1)] private int maxCellCount = 512;

        [Header("Coordinates")]
        [SerializeField, Min(0.001f)] private float cellSize = 1f;
        [SerializeField] private Vector3 worldOrigin;
        [SerializeField] private MapCoordinateOrder stableCoordinateOrder = MapCoordinateOrder.YThenX;

        [Header("Queries")]
        [SerializeField, Min(0)] private int rangeQueryCap = 64;

        [Header("Terrain")]
        [SerializeField] private CellTerrain[] terrainPrecedence =
        {
            CellTerrain.Obstacle,
            CellTerrain.Wall,
            CellTerrain.Ground
        };

        public bool TryCreateSettings(out MapGridSettings settings, out string error)
        {
            settings = null;
            error = string.Empty;

            GridPosition minPosition = new GridPosition(minCell.x, minCell.y);
            GridPosition maxPosition = new GridPosition(maxCell.x, maxCell.y);

            if (maxPosition.X < minPosition.X || maxPosition.Y < minPosition.Y)
            {
                error = $"Invalid map bounds: min {minPosition}, max {maxPosition}.";
                return false;
            }

            long width = (long)maxPosition.X - minPosition.X + 1L;
            long height = (long)maxPosition.Y - minPosition.Y + 1L;
            long cellCount = width * height;

            if (cellCount <= 0L || cellCount > maxCellCount)
            {
                error = $"Map cell count {cellCount} must be between 1 and maxCellCount {maxCellCount}.";
                return false;
            }

            if (cellSize <= 0f)
            {
                error = "Cell size must be positive.";
                return false;
            }

            if (rangeQueryCap < 0)
            {
                error = "Range query cap cannot be negative.";
                return false;
            }

            if (!ValidateTerrainPrecedence(out error))
            {
                return false;
            }

            settings = new MapGridSettings(
                minPosition,
                maxPosition,
                cellSize,
                worldOrigin,
                rangeQueryCap,
                stableCoordinateOrder,
                terrainPrecedence);
            return true;
        }

        private bool ValidateTerrainPrecedence(out string error)
        {
            error = string.Empty;

            if (terrainPrecedence == null || terrainPrecedence.Length == 0)
            {
                error = "Terrain precedence cannot be empty.";
                return false;
            }

            HashSet<CellTerrain> seen = new HashSet<CellTerrain>();

            for (int i = 0; i < terrainPrecedence.Length; i++)
            {
                CellTerrain terrain = terrainPrecedence[i];

                if (terrain == CellTerrain.Empty)
                {
                    error = "Terrain precedence cannot contain Empty.";
                    return false;
                }

                if (!seen.Add(terrain))
                {
                    error = $"Terrain precedence contains duplicate terrain {terrain}.";
                    return false;
                }
            }

            if (!seen.Contains(CellTerrain.Ground) || !seen.Contains(CellTerrain.Wall) || !seen.Contains(CellTerrain.Obstacle))
            {
                error = "Terrain precedence must include Ground, Wall and Obstacle.";
                return false;
            }

            return true;
        }
    }
}
