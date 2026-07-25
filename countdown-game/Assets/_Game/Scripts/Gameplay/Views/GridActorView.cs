using System;
using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    public sealed class GridActorView : MonoBehaviour
    {
        public int actorId;
        public int spawnId;
        public ActorKind actorKind;
        public Vector2Int initialCell;

        private Func<GridCoord, Vector3> _cellCenterResolver;

        public void Initialize(Func<GridCoord, Vector3> cellCenterResolver)
        {
            _cellCenterResolver = cellCenterResolver;
        }

        public void Present(GridCoord cell)
        {
            gameObject.SetActive(true);
            Vector3 position = _cellCenterResolver != null
                ? _cellCenterResolver(cell)
                : new Vector3(cell.X + 0.5f, cell.Y + 0.5f, transform.position.z);
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}
