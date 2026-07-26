using System.Collections.Generic;
using System;
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
        [SerializeField] private Sprite cellSprite;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private Tilemap _terrainTilemap;
        private Func<GridCoord, Vector3> _cellCenterResolver;

        public void Initialize(
            Tilemap terrainTilemap,
            Func<GridCoord, Vector3> cellCenterResolver = null,
            Sprite overrideCellSprite = null)
        {
            _terrainTilemap = terrainTilemap;
            _cellCenterResolver = cellCenterResolver;
            if (overrideCellSprite != null)
                cellSprite = overrideCellSprite;
            ApplySorting();
        }

        public void Present(IReadOnlyList<GridCoord> cells)
        {
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
            if (_cellCenterResolver != null)
                return _cellCenterResolver(cell);
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
                renderer.sprite = cellSprite;
                renderer.color = availableColor;
                _renderers.Add(renderer);
            }
            ApplySorting();
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

    }
}
