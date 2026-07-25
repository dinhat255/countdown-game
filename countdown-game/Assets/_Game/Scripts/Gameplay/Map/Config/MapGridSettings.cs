using System;
using Countdown.Gameplay.Map;
using UnityEngine;

namespace Countdown.Gameplay.Map.Config
{
    public enum MapCoordinateOrder
    {
        YThenX = 0,
        XThenY = 1
    }

    public sealed class MapGridSettings
    {
        private readonly CellTerrain[] terrainPrecedence;

        public MapGridSettings(
            GridPosition minPosition,
            GridPosition maxPosition,
            float cellSize,
            Vector3 worldOrigin,
            int rangeQueryCap,
            MapCoordinateOrder coordinateOrder,
            CellTerrain[] terrainPrecedence)
        {
            if (maxPosition.X < minPosition.X || maxPosition.Y < minPosition.Y)
            {
                throw new ArgumentException("MapGridSettings bounds are invalid.");
            }

            long width = (long)maxPosition.X - minPosition.X + 1L;
            long height = (long)maxPosition.Y - minPosition.Y + 1L;
            long cellCount = width * height;

            if (cellCount <= 0L || cellCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPosition), "MapGridSettings bounds produce an invalid cell count.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
            }

            if (rangeQueryCap < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeQueryCap), "Range query cap cannot be negative.");
            }

            ValidateTerrainPrecedence(terrainPrecedence);

            MinPosition = minPosition;
            MaxPosition = maxPosition;
            CellSize = cellSize;
            WorldOrigin = worldOrigin;
            RangeQueryCap = rangeQueryCap;
            CoordinateOrder = coordinateOrder;
            Width = (int)width;
            Height = (int)height;
            CellCount = (int)cellCount;
            this.terrainPrecedence = (CellTerrain[])terrainPrecedence.Clone();
            StableComparer = CreateComparer(coordinateOrder);
        }

        public GridPosition MinPosition { get; }
        public GridPosition MaxPosition { get; }
        public float CellSize { get; }
        public Vector3 WorldOrigin { get; }
        public int RangeQueryCap { get; }
        public MapCoordinateOrder CoordinateOrder { get; }
        public System.Collections.Generic.IComparer<GridPosition> StableComparer { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        public int TerrainPrecedenceCount => terrainPrecedence.Length;

        public CellTerrain GetTerrainByPrecedence(int index)
        {
            return terrainPrecedence[index];
        }

        private static System.Collections.Generic.IComparer<GridPosition> CreateComparer(MapCoordinateOrder coordinateOrder)
        {
            return coordinateOrder == MapCoordinateOrder.XThenY
                ? GridPositionXThenYComparer.Instance
                : GridPositionYThenXComparer.Instance;
        }

        private static void ValidateTerrainPrecedence(CellTerrain[] terrains)
        {
            if (terrains == null || terrains.Length == 0)
            {
                throw new ArgumentException("Terrain precedence cannot be empty.", nameof(terrains));
            }

            System.Collections.Generic.HashSet<CellTerrain> seen = new System.Collections.Generic.HashSet<CellTerrain>();

            for (int i = 0; i < terrains.Length; i++)
            {
                CellTerrain terrain = terrains[i];

                if (terrain == CellTerrain.Empty)
                {
                    throw new ArgumentException("Terrain precedence cannot contain Empty.", nameof(terrains));
                }

                if (!seen.Add(terrain))
                {
                    throw new ArgumentException($"Terrain precedence contains duplicate terrain {terrain}.", nameof(terrains));
                }
            }
        }

        private sealed class GridPositionYThenXComparer : System.Collections.Generic.IComparer<GridPosition>
        {
            public static readonly GridPositionYThenXComparer Instance = new GridPositionYThenXComparer();

            public int Compare(GridPosition left, GridPosition right)
            {
                int yCompare = left.Y.CompareTo(right.Y);
                return yCompare != 0 ? yCompare : left.X.CompareTo(right.X);
            }
        }

        private sealed class GridPositionXThenYComparer : System.Collections.Generic.IComparer<GridPosition>
        {
            public static readonly GridPositionXThenYComparer Instance = new GridPositionXThenYComparer();

            public int Compare(GridPosition left, GridPosition right)
            {
                int xCompare = left.X.CompareTo(right.X);
                return xCompare != 0 ? xCompare : left.Y.CompareTo(right.Y);
            }
        }
    }
}
