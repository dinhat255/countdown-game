using UnityEngine;
using UnityEngine.Tilemaps;

namespace Countdown.Gameplay.Map.Config
{
    public sealed class MapSceneConfigurator : MonoBehaviour
    {
        [SerializeField] private MapSceneConfig config;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private TilemapRenderer groundRenderer;
        [SerializeField] private TilemapRenderer wallRenderer;
        [SerializeField] private TilemapRenderer obstacleRenderer;
        [SerializeField] private TilemapRenderer overlayRenderer;
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private Vector3Int[] overlayPreviewCells = System.Array.Empty<Vector3Int>();

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        public void Apply()
        {
            if (config == null)
            {
                return;
            }

            Camera cameraToConfigure = targetCamera != null ? targetCamera : (Application.isPlaying ? Camera.main : null);

            if (cameraToConfigure != null)
            {
                cameraToConfigure.orthographic = true;
                cameraToConfigure.orthographicSize = config.CameraOrthographicSize;

                Vector3 currentPosition = cameraToConfigure.transform.position;
                cameraToConfigure.transform.position = new Vector3(
                    config.CameraOffset.x,
                    config.CameraOffset.y,
                    currentPosition.z);
            }

            ApplySortingOrder(groundRenderer, config.GroundSortingOrder);
            ApplySortingOrder(wallRenderer, config.WallSortingOrder);
            ApplySortingOrder(obstacleRenderer, config.ObstacleSortingOrder);
            ApplySortingOrder(overlayRenderer, config.OverlaySortingOrder);
            ApplyOverlayPreview();
        }

        private static void ApplySortingOrder(TilemapRenderer renderer, int sortingOrder)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }
        }

        private void ApplyOverlayPreview()
        {
            if (overlayTilemap == null || config.OverlayTile == null)
            {
                return;
            }

            overlayTilemap.ClearAllTiles();
            for (int i = 0; i < overlayPreviewCells.Length; i++)
            {
                overlayTilemap.SetTile(overlayPreviewCells[i], config.OverlayTile);
            }
        }

        public void Configure(
            MapSceneConfig sceneConfig,
            Camera camera,
            TilemapRenderer groundTilemapRenderer,
            TilemapRenderer wallTilemapRenderer,
            TilemapRenderer obstacleTilemapRenderer,
            TilemapRenderer overlayTilemapRenderer,
            Tilemap previewTilemap,
            Vector3Int[] previewCells)
        {
            config = sceneConfig;
            targetCamera = camera;
            groundRenderer = groundTilemapRenderer;
            wallRenderer = wallTilemapRenderer;
            obstacleRenderer = obstacleTilemapRenderer;
            overlayRenderer = overlayTilemapRenderer;
            overlayTilemap = previewTilemap;
            overlayPreviewCells = previewCells ?? System.Array.Empty<Vector3Int>();
            Apply();
        }
    }
}
