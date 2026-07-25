using System.Collections.Generic;
using CountdownGame.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CountdownGame.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerMoveHighlightView : MonoBehaviour
    {
        [SerializeField] private Color availableColor = new Color(0.15f, 0.9f, 1f, 0.42f);
        [SerializeField, Range(0.5f, 1f)] private float cellScale = 0.86f;
        [SerializeField] private int sortingOrderOffset = 1;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private Tilemap _terrainTilemap;
        private Sprite _cellSprite;

        public void Initialize(Tilemap terrainTilemap)
        {
            _terrainTilemap = terrainTilemap;
            EnsureSprite();
            ApplySorting();
        }

        public void Present(IReadOnlyList<GridCoord> cells)
        {
            EnsureSprite();
            var visibleCount = cells?.Count ?? 0;
            EnsureRendererCount(visibleCount);

            for (var index = 0; index < _renderers.Count; index++)
            {
                var renderer = _renderers[index];
                var visible = index < visibleCount;
                renderer.gameObject.SetActive(visible);
                if (!visible) continue;

                var cell = cells[index];
                renderer.transform.position = CellCenter(cell);
                renderer.transform.localScale = new Vector3(cellScale, cellScale, 1f);
                renderer.color = availableColor;
            }
        }

        private Vector3 CellCenter(GridCoord cell)
        {
            if (_terrainTilemap != null)
                return _terrainTilemap.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));
            return new Vector3(cell.X + 0.5f, cell.Y + 0.5f, 0f);
        }

        private void EnsureRendererCount(int count)
        {
            while (_renderers.Count < count)
            {
                var cellObject = new GameObject($"Available Move {_renderers.Count + 1}");
                cellObject.transform.SetParent(transform, false);
                var renderer = cellObject.AddComponent<SpriteRenderer>();
                renderer.sprite = _cellSprite;
                renderer.color = availableColor;
                _renderers.Add(renderer);
            }
            ApplySorting();
        }

        private void EnsureSprite()
        {
            if (_cellSprite != null) return;
            var texture = Texture2D.whiteTexture;
            _cellSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            _cellSprite.name = "Runtime Player Move Highlight";
        }

        private void ApplySorting()
        {
            var terrainRenderer = _terrainTilemap != null
                ? _terrainTilemap.GetComponent<TilemapRenderer>()
                : null;
            foreach (var renderer in _renderers)
            {
                if (terrainRenderer != null)
                {
                    renderer.sortingLayerID = terrainRenderer.sortingLayerID;
                    renderer.sortingOrder = terrainRenderer.sortingOrder + sortingOrderOffset;
                }
                else
                {
                    renderer.sortingOrder = sortingOrderOffset;
                }
            }
        }

        private void OnDestroy()
        {
            if (_cellSprite == null) return;
            if (Application.isPlaying) Destroy(_cellSprite);
            else DestroyImmediate(_cellSprite);
        }
    }
}
