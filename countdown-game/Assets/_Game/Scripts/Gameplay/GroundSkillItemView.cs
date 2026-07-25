using UnityEngine;

namespace CountdownGame.Unity
{
    public sealed class GroundSkillItemView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer iconRenderer;
        public int ItemId { get; private set; }
        public Vector2Int Cell { get; private set; }

        private void Awake()
        {
            var clickTarget = GetComponent<Collider2D>();
            if (clickTarget == null)
                clickTarget = gameObject.AddComponent<BoxCollider2D>();
            clickTarget.isTrigger = true;
        }

        public void Present(
            int itemId,
            string skillId,
            Vector2Int cell,
            Vector3 cellCenterWorld,
            Sprite icon)
        {
            ItemId = itemId;
            Cell = cell;
            gameObject.name = $"Skill Drop Placeholder - {skillId}";
            transform.position = new Vector3(
                cellCenterWorld.x,
                cellCenterWorld.y,
                transform.position.z);
            if (iconRenderer != null && icon != null) iconRenderer.sprite = icon;
        }
    }
}
