namespace Countdown.Gameplay.Map
{
    public enum MapActorOccupantType
    {
        Empty = 0,
        Player = 1,
        Enemy = 2
    }

    public enum MapInteractableType
    {
        Empty = 0,
        SkillItem = 1
    }

    public enum MapHazardType
    {
        Empty = 0,
        EnvironmentalBomb = 1,
        BombSkill = 2
    }

    public struct MapCell
    {
        public MapCell(CellTerrain terrain)
        {
            Terrain = terrain;
            OccupantType = MapActorOccupantType.Empty;
            Occupant = null;
            InteractableType = MapInteractableType.Empty;
            Interactable = null;
            HazardType = MapHazardType.Empty;
            Hazard = null;
        }

        public CellTerrain Terrain { get; private set; }
        public MapActorOccupantType OccupantType { get; private set; }
        public object Occupant { get; private set; }
        public MapInteractableType InteractableType { get; private set; }
        public object Interactable { get; private set; }
        public MapHazardType HazardType { get; private set; }
        public object Hazard { get; private set; }

        public bool HasOccupant => OccupantType != MapActorOccupantType.Empty;
        public bool HasInteractable => InteractableType != MapInteractableType.Empty;
        public bool HasHazard => HazardType != MapHazardType.Empty;

        public void SetTerrain(CellTerrain terrain)
        {
            Terrain = terrain;
        }

        public bool TryPlaceOccupant(object occupant, MapActorOccupantType occupantType)
        {
            if (occupant == null || occupantType == MapActorOccupantType.Empty || HasOccupant)
            {
                return false;
            }

            Occupant = occupant;
            OccupantType = occupantType;
            return true;
        }

        public bool RemoveOccupant(object expectedOccupant = null)
        {
            if (!HasOccupant)
            {
                return false;
            }

            if (expectedOccupant != null && !ReferenceEquals(Occupant, expectedOccupant))
            {
                return false;
            }

            Occupant = null;
            OccupantType = MapActorOccupantType.Empty;
            return true;
        }

        public bool TryPlaceInteractable(object interactable, MapInteractableType interactableType)
        {
            if (interactable == null || interactableType == MapInteractableType.Empty || HasInteractable)
            {
                return false;
            }

            Interactable = interactable;
            InteractableType = interactableType;
            return true;
        }

        public bool RemoveInteractable(object expectedInteractable = null)
        {
            if (!HasInteractable)
            {
                return false;
            }

            if (expectedInteractable != null && !ReferenceEquals(Interactable, expectedInteractable))
            {
                return false;
            }

            Interactable = null;
            InteractableType = MapInteractableType.Empty;
            return true;
        }

        public bool TryPlaceHazard(object hazard, MapHazardType hazardType)
        {
            if (hazard == null || hazardType == MapHazardType.Empty || HasHazard)
            {
                return false;
            }

            Hazard = hazard;
            HazardType = hazardType;
            return true;
        }

        public bool RemoveHazard(object expectedHazard = null)
        {
            if (!HasHazard)
            {
                return false;
            }

            if (expectedHazard != null && !ReferenceEquals(Hazard, expectedHazard))
            {
                return false;
            }

            Hazard = null;
            HazardType = MapHazardType.Empty;
            return true;
        }
    }
}
