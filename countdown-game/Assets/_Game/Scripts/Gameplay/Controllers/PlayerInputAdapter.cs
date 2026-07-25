using CountdownGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CountdownGame.Unity
{
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        [SerializeField] private CountdownGameController controller;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private Tilemap boardTilemap;

        private void Awake()
        {
            if (boardCamera == null) boardCamera = Camera.main;
            if (boardTilemap == null && controller != null)
                boardTilemap = controller.TerrainTilemap;
            if (boardTilemap == null)
                boardTilemap = FindAnyObjectByType<Tilemap>();
        }

        private void Update()
        {
            if (controller == null || controller.Simulation == null ||
                controller.Simulation.Phase != BeatPhase.Player ||
                Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var pointer = Mouse.current.position.ReadValue();
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            TryMoveToScreenPoint(pointer);
        }

        private void TryMoveToScreenPoint(Vector2 screenPoint)
        {
            if (boardCamera == null) return;

            var world = boardCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, -boardCamera.transform.position.z));
            if (!controller.TryWorldToBoardCell(world, out Vector2Int clickedCell))
            {
                if (boardTilemap == null) return;
                Vector3Int tileCell = boardTilemap.WorldToCell(world);
                clickedCell = new Vector2Int(tileCell.x, tileCell.y);
            }

            var clickedItem = FindGroundItemAt(world);
            if (clickedItem != null)
                clickedCell = clickedItem.Cell;
            if (controller.TargetingSkillSlot >= 0)
            {
                var result = controller.UseSkillAt(
                    controller.TargetingSkillSlot, clickedCell);
                if (result.Succeeded) controller.CancelSkillTarget();
                return;
            }
            var player = controller.Simulation.Player.Position;
            var destination = clickedCell;

            if (TryGetMoveDirection(
                    new Vector2Int(player.X, player.Y), destination, out var direction))
                controller.Move(direction);
            else if (clickedItem != null && TryGetGroundItemApproachDirection(
                         controller.Simulation.Grid,
                         player,
                         new GridCoord(clickedItem.Cell.x, clickedItem.Cell.y),
                         out direction))
                controller.Move(direction);
        }

        private static GroundSkillItemView FindGroundItemAt(Vector3 worldPoint)
        {
            foreach (var hit in Physics2D.OverlapPointAll(worldPoint))
            {
                var item = hit.GetComponentInParent<GroundSkillItemView>();
                if (item != null) return item;
            }
            return null;
        }

        public static bool TryGetGroundItemApproachDirection(
            GridState grid,
            GridCoord player,
            GridCoord item,
            out GridDirection direction)
        {
            direction = GridDirection.Up;
            if (grid == null || !grid.HasOverlay(item, OverlayKind.Item)) return false;
            var actor = grid.GetActorAt(player);
            if (actor == null || actor.Kind != ActorKind.Player) return false;
            var next = GridPathfinding.NextStep(
                grid,
                player,
                item,
                actor.Id,
                new SeededRandomContext(0, 0, actor.Id, 0));
            return next.HasValue && TryGetMoveDirection(
                new Vector2Int(player.X, player.Y),
                new Vector2Int(next.Value.X, next.Value.Y),
                out direction);
        }

        public static bool TryGetMoveDirection(
            Vector2Int origin,
            Vector2Int destination,
            out GridDirection direction)
        {
            var delta = destination - origin;
            if (delta == Vector2Int.up)
            {
                direction = GridDirection.Up;
                return true;
            }
            if (delta == Vector2Int.right)
            {
                direction = GridDirection.Right;
                return true;
            }
            if (delta == Vector2Int.down)
            {
                direction = GridDirection.Down;
                return true;
            }
            if (delta == Vector2Int.left)
            {
                direction = GridDirection.Left;
                return true;
            }

            direction = default;
            return false;
        }
    }
}
