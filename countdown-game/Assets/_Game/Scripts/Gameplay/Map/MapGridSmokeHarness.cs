using Countdown.Gameplay.Map.Config;
using UnityEngine;

namespace Countdown.Gameplay.Map
{
    public static class MapGridSmokeHarness
    {
        public static bool Run(out string error)
        {
            error = string.Empty;

            CellTerrain[] terrainPrecedence =
            {
                CellTerrain.Obstacle,
                CellTerrain.Wall,
                CellTerrain.Ground
            };

            MapGridSettings defaultSettings = new MapGridSettings(
                new GridPosition(-1, -1),
                new GridPosition(1, 1),
                1f,
                Vector3.zero,
                8,
                MapCoordinateOrder.YThenX,
                terrainPrecedence);

            if (!CheckCoreInvariants(defaultSettings, out error))
            {
                return false;
            }

            MapGridSettings shiftedSettings = new MapGridSettings(
                new GridPosition(-2, -2),
                new GridPosition(2, 2),
                0.5f,
                new Vector3(10f, -3f, 0f),
                12,
                MapCoordinateOrder.XThenY,
                terrainPrecedence);

            return CheckCoreInvariants(shiftedSettings, out error);
        }

        private static bool CheckCoreInvariants(MapGridSettings settings, out string error)
        {
            error = string.Empty;
            MapGrid grid = new MapGrid(settings);

            GridPosition start = new GridPosition(0, 0);
            GridPosition destination = new GridPosition(1, 0);
            GridPosition wall = new GridPosition(0, 1);
            GridPosition obstacle = new GridPosition(-1, 0);

            grid.SetTerrain(start, CellTerrain.Ground);
            grid.SetTerrain(destination, CellTerrain.Ground);
            grid.SetTerrain(wall, CellTerrain.Wall);
            grid.SetTerrain(obstacle, CellTerrain.Obstacle);

            GridPosition roundTrip = grid.WorldToGrid(grid.GridToWorld(start));
            if (roundTrip != start)
            {
                error = $"World/grid round-trip failed. Expected {start}, got {roundTrip}.";
                return false;
            }

            if (!grid.IsWalkable(start) || grid.IsWalkable(wall) || grid.IsWalkable(obstacle))
            {
                error = "Walkability invariant failed.";
                return false;
            }

            object player = new object();
            object enemy = new object();
            object item = new object();
            object bomb = new object();

            if (!grid.TryPlaceOccupant(start, player, MapActorOccupantType.Player))
            {
                error = "Initial player placement failed.";
                return false;
            }

            if (grid.TryPlaceOccupant(start, enemy, MapActorOccupantType.Enemy))
            {
                error = "Second actor incorrectly entered an occupied cell.";
                return false;
            }

            if (!grid.TryPlaceInteractable(start, item, MapInteractableType.SkillItem)
                || !grid.TryPlaceHazard(start, bomb, MapHazardType.EnvironmentalBomb))
            {
                error = "Item/hazard should coexist with actor on the same cell.";
                return false;
            }

            if (grid.TryMoveOccupant(start, wall, player))
            {
                error = "Actor incorrectly moved into wall.";
                return false;
            }

            if (!ReferenceEquals(grid.GetOccupant(start), player))
            {
                error = "Failed move changed occupant state.";
                return false;
            }

            if (!grid.TryMoveOccupant(start, destination, player))
            {
                error = "Valid move failed.";
                return false;
            }

            if (grid.GetOccupant(start) != null || !ReferenceEquals(grid.GetOccupant(destination), player))
            {
                error = "Valid move produced incorrect occupant state.";
                return false;
            }

            GridPosition[] neighbors = new GridPosition[4];
            int neighborCount = grid.GetFourNeighbors(start, neighbors);
            if (neighborCount != 4)
            {
                error = $"Expected 4 neighbors around {start}, got {neighborCount}.";
                return false;
            }

            GridPosition[] range = new GridPosition[16];
            if (!grid.TryGetCellsInRange(start, 1, range, out int rangeCount) || rangeCount != 4)
            {
                error = $"Expected 4 range cells around {start}, got {rangeCount}.";
                return false;
            }

            return true;
        }
    }
}
