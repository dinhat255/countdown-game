using UnityEngine;
using UnityEngine.Tilemaps;

namespace Countdown.Gameplay.Map.Config
{
    [ExecuteAlways]
    public sealed class MapSceneTestPainter : MonoBehaviour
    {
        [SerializeField] private MapSceneConfig config;
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] private Tilemap overlayTilemap;
        [SerializeField] private bool paintOnValidate = true;

        private void Awake()
        {
            Paint();
        }

        private void OnValidate()
        {
            if (paintOnValidate)
            {
                Paint();
            }
        }

        [ContextMenu("Paint Phase 1 Test Map")]
        public void Paint()
        {
            if (config == null)
            {
                return;
            }

            PaintGround();
            PaintWalls();
            PaintObstacles();
            PaintOverlay();
        }

        private void PaintGround()
        {
            if (groundTilemap == null || config.GroundTile == null)
            {
                return;
            }

            groundTilemap.ClearAllTiles();
            for (int x = -3; x <= 3; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), config.GroundTile);
                }
            }
        }

        private void PaintWalls()
        {
            if (wallTilemap == null || config.WallTile == null)
            {
                return;
            }

            wallTilemap.ClearAllTiles();
            for (int x = -4; x <= 4; x++)
            {
                wallTilemap.SetTile(new Vector3Int(x, -3, 0), config.WallTile);
                wallTilemap.SetTile(new Vector3Int(x, 3, 0), config.WallTile);
            }

            for (int y = -2; y <= 2; y++)
            {
                wallTilemap.SetTile(new Vector3Int(-4, y, 0), config.WallTile);
                wallTilemap.SetTile(new Vector3Int(4, y, 0), config.WallTile);
            }
        }

        private void PaintObstacles()
        {
            if (obstacleTilemap == null || config.ObstacleTile == null)
            {
                return;
            }

            obstacleTilemap.ClearAllTiles();
            obstacleTilemap.SetTile(new Vector3Int(-1, 0, 0), config.ObstacleTile);
            obstacleTilemap.SetTile(new Vector3Int(0, 0, 0), config.ObstacleTile);
            obstacleTilemap.SetTile(new Vector3Int(1, 1, 0), config.ObstacleTile);
        }

        private void PaintOverlay()
        {
            if (overlayTilemap == null || config.OverlayTile == null)
            {
                return;
            }

            overlayTilemap.ClearAllTiles();
            overlayTilemap.SetTile(new Vector3Int(-3, -2, 0), config.OverlayTile);
            overlayTilemap.SetTile(new Vector3Int(-2, -2, 0), config.OverlayTile);
            overlayTilemap.SetTile(new Vector3Int(-1, -2, 0), config.OverlayTile);
        }
    }
}
