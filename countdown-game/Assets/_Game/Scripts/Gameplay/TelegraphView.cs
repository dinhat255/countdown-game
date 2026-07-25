using UnityEngine;

namespace CountdownGame.Unity
{
    public sealed class TelegraphView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer marker;

        public void Show(Vector2Int cell, bool paused)
        {
            transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, -0.2f);
            if (marker != null)
            {
                marker.enabled = true;
                marker.color = paused
                    ? new Color(0.4f, 0.7f, 1f, 0.55f)
                    : new Color(1f, 0.8f, 0.1f, 0.65f);
            }
        }

        public void Hide()
        {
            if (marker != null) marker.enabled = false;
        }
    }
}
