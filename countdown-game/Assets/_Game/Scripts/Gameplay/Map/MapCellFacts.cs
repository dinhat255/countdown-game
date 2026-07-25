namespace Countdown.Gameplay.Map
{
    public readonly struct MapCellFacts
    {
        public MapCellFacts(GridPosition position, bool isInsideMap, CellTerrain terrain, bool hasOccupant, bool hasInteractable, bool hasHazard)
        {
            Position = position;
            IsInsideMap = isInsideMap;
            Terrain = terrain;
            HasOccupant = hasOccupant;
            HasInteractable = hasInteractable;
            HasHazard = hasHazard;
        }

        public GridPosition Position { get; }
        public bool IsInsideMap { get; }
        public CellTerrain Terrain { get; }
        public bool HasOccupant { get; }
        public bool HasInteractable { get; }
        public bool HasHazard { get; }
        public bool IsWalkable => IsInsideMap && Terrain == CellTerrain.Ground;
        public bool CanEnter => IsWalkable && !HasOccupant;

        public static MapCellFacts Outside(GridPosition position)
        {
            return new MapCellFacts(position, false, CellTerrain.Empty, false, false, false);
        }
    }
}
