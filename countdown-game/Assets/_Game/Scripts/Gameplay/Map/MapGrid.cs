using Countdown.Gameplay.Map.Config;
using UnityEngine;

namespace Countdown.Gameplay.Map
{
    public sealed class MapGrid
    {
        private static readonly GridPosition[] FourNeighborOffsets =
        {
            new GridPosition(0, 1),
            new GridPosition(1, 0),
            new GridPosition(0, -1),
            new GridPosition(-1, 0)
        };

        private readonly MapCell[] cells;

        public MapGrid(MapGridSettings settings)
        {
            Settings = settings;
            cells = new MapCell[settings.CellCount];

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new MapCell(CellTerrain.Empty);
            }
        }

        public MapGridSettings Settings { get; }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            Vector3 local = worldPosition - Settings.WorldOrigin;
            int x = Mathf.FloorToInt(local.x / Settings.CellSize);
            int y = Mathf.FloorToInt(local.y / Settings.CellSize);
            return new GridPosition(x, y);
        }

        public Vector3 GridToWorld(GridPosition position)
        {
            return Settings.WorldOrigin + new Vector3(
                (position.X + 0.5f) * Settings.CellSize,
                (position.Y + 0.5f) * Settings.CellSize,
                0f);
        }

        public bool IsInsideMap(GridPosition position)
        {
            return position.X >= Settings.MinPosition.X
                && position.X <= Settings.MaxPosition.X
                && position.Y >= Settings.MinPosition.Y
                && position.Y <= Settings.MaxPosition.Y;
        }

        public bool IsWalkable(GridPosition position)
        {
            return IsInsideMap(position) && cells[GetIndex(position)].Terrain == CellTerrain.Ground;
        }

        public bool IsOccupied(GridPosition position)
        {
            return IsInsideMap(position) && cells[GetIndex(position)].HasOccupant;
        }

        public bool CanEnter(GridPosition position)
        {
            return IsWalkable(position) && !IsOccupied(position);
        }

        public MapCellFacts GetCellFacts(GridPosition position)
        {
            if (!IsInsideMap(position))
            {
                return MapCellFacts.Outside(position);
            }

            MapCell cell = cells[GetIndex(position)];
            return new MapCellFacts(
                position,
                true,
                cell.Terrain,
                cell.HasOccupant,
                cell.HasInteractable,
                cell.HasHazard);
        }

        public object GetOccupant(GridPosition position)
        {
            return IsInsideMap(position) ? cells[GetIndex(position)].Occupant : null;
        }

        public object GetInteractable(GridPosition position)
        {
            return IsInsideMap(position) ? cells[GetIndex(position)].Interactable : null;
        }

        public object GetHazard(GridPosition position)
        {
            return IsInsideMap(position) ? cells[GetIndex(position)].Hazard : null;
        }

        public int GetFourNeighbors(GridPosition position, GridPosition[] results, bool insideOnly = true)
        {
            if (results == null || results.Length < FourNeighborOffsets.Length)
            {
                return 0;
            }

            int count = 0;

            for (int i = 0; i < FourNeighborOffsets.Length; i++)
            {
                GridPosition candidate = new GridPosition(
                    position.X + FourNeighborOffsets[i].X,
                    position.Y + FourNeighborOffsets[i].Y);

                if (insideOnly && !IsInsideMap(candidate))
                {
                    continue;
                }

                results[count] = candidate;
                count++;
            }

            return count;
        }

        public bool TryGetCellsInRange(GridPosition center, int range, GridPosition[] results, out int count, bool includeCenter = false, bool insideOnly = true)
        {
            count = 0;

            if (range < 0 || range > Settings.RangeQueryCap || results == null)
            {
                return false;
            }

            for (int y = center.Y - range; y <= center.Y + range; y++)
            {
                for (int x = center.X - range; x <= center.X + range; x++)
                {
                    GridPosition candidate = new GridPosition(x, y);

                    if (!includeCenter && candidate == center)
                    {
                        continue;
                    }

                    if (insideOnly && !IsInsideMap(candidate))
                    {
                        continue;
                    }

                    if (center.ManhattanDistanceTo(candidate) > range)
                    {
                        continue;
                    }

                    if (count >= results.Length)
                    {
                        count = 0;
                        return false;
                    }

                    results[count] = candidate;
                    count++;
                }
            }

            System.Array.Sort(results, 0, count, Settings.StableComparer);
            return true;
        }

        public bool TryPlaceOccupant(GridPosition position, object occupant, MapActorOccupantType occupantType)
        {
            if (!CanEnter(position) || occupant == null || occupantType == MapActorOccupantType.Empty)
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool placed = cell.TryPlaceOccupant(occupant, occupantType);
            cells[index] = cell;
            return placed;
        }

        public bool TryMoveOccupant(GridPosition from, GridPosition to, object expectedOccupant = null)
        {
            if (!IsInsideMap(from) || !CanEnter(to))
            {
                return false;
            }

            int fromIndex = GetIndex(from);
            MapCell fromCell = cells[fromIndex];

            if (!fromCell.HasOccupant)
            {
                return false;
            }

            if (expectedOccupant != null && !ReferenceEquals(fromCell.Occupant, expectedOccupant))
            {
                return false;
            }

            int toIndex = GetIndex(to);
            MapCell toCell = cells[toIndex];
            object occupant = fromCell.Occupant;
            MapActorOccupantType occupantType = fromCell.OccupantType;

            if (!toCell.TryPlaceOccupant(occupant, occupantType))
            {
                return false;
            }

            fromCell.RemoveOccupant(occupant);
            cells[fromIndex] = fromCell;
            cells[toIndex] = toCell;
            return true;
        }

        public bool RemoveOccupant(GridPosition position, object expectedOccupant = null)
        {
            if (!IsInsideMap(position))
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool removed = cell.RemoveOccupant(expectedOccupant);
            cells[index] = cell;
            return removed;
        }

        public bool TryPlaceInteractable(GridPosition position, object interactable, MapInteractableType interactableType)
        {
            if (!IsInsideMap(position) || interactable == null || interactableType == MapInteractableType.Empty)
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool placed = cell.TryPlaceInteractable(interactable, interactableType);
            cells[index] = cell;
            return placed;
        }

        public bool RemoveInteractable(GridPosition position, object expectedInteractable = null)
        {
            if (!IsInsideMap(position))
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool removed = cell.RemoveInteractable(expectedInteractable);
            cells[index] = cell;
            return removed;
        }

        public bool TryPlaceHazard(GridPosition position, object hazard, MapHazardType hazardType)
        {
            if (!IsInsideMap(position) || hazard == null || hazardType == MapHazardType.Empty)
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool placed = cell.TryPlaceHazard(hazard, hazardType);
            cells[index] = cell;
            return placed;
        }

        public bool RemoveHazard(GridPosition position, object expectedHazard = null)
        {
            if (!IsInsideMap(position))
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            bool removed = cell.RemoveHazard(expectedHazard);
            cells[index] = cell;
            return removed;
        }

        internal bool SetTerrain(GridPosition position, CellTerrain terrain)
        {
            if (!IsInsideMap(position))
            {
                return false;
            }

            int index = GetIndex(position);
            MapCell cell = cells[index];
            cell.SetTerrain(terrain);
            cells[index] = cell;
            return true;
        }

        private int GetIndex(GridPosition position)
        {
            int x = position.X - Settings.MinPosition.X;
            int y = position.Y - Settings.MinPosition.Y;
            return y * Settings.Width + x;
        }
    }
}
