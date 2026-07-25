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

        public void Present(GridCoord cell)
        {
            transform.position = new Vector3(cell.X + 0.5f, cell.Y + 0.5f, transform.position.z);
        }
    }
}
