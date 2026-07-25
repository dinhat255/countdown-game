using UnityEngine;
using UnityEngine.Tilemaps;

namespace Countdown.Gameplay.Map.Config
{
    public sealed class MapSceneConfig : ScriptableObject
    {
        [Header("Camera")]
        [SerializeField, Min(0.1f)] private float cameraOrthographicSize = 5.5f;
        [SerializeField] private Vector2 cameraOffset;

        [Header("Sorting Orders")]
        [SerializeField] private int groundSortingOrder;
        [SerializeField] private int wallSortingOrder = 10;
        [SerializeField] private int obstacleSortingOrder = 20;
        [SerializeField] private int actorSortingOrder = 30;
        [SerializeField] private int itemSortingOrder = 40;
        [SerializeField] private int hazardSortingOrder = 50;
        [SerializeField] private int overlaySortingOrder = 100;

        [Header("Placeholder Tiles")]
        [SerializeField] private TileBase groundTile;
        [SerializeField] private TileBase wallTile;
        [SerializeField] private TileBase obstacleTile;
        [SerializeField] private TileBase overlayTile;

        public float CameraOrthographicSize => cameraOrthographicSize;
        public Vector2 CameraOffset => cameraOffset;
        public int GroundSortingOrder => groundSortingOrder;
        public int WallSortingOrder => wallSortingOrder;
        public int ObstacleSortingOrder => obstacleSortingOrder;
        public int ActorSortingOrder => actorSortingOrder;
        public int ItemSortingOrder => itemSortingOrder;
        public int HazardSortingOrder => hazardSortingOrder;
        public int OverlaySortingOrder => overlaySortingOrder;
        public TileBase GroundTile => groundTile;
        public TileBase WallTile => wallTile;
        public TileBase ObstacleTile => obstacleTile;
        public TileBase OverlayTile => overlayTile;
    }
}
