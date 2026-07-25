using CountdownGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CountdownGame.Unity
{
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        [SerializeField] private CountdownGameController controller;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private Tilemap boardTilemap;

        private Rect _endTurnRect;
        private bool _endTurnRequested;

        private void Awake()
        {
            if (boardCamera == null) boardCamera = Camera.main;
            if (boardTilemap == null) boardTilemap = FindAnyObjectByType<Tilemap>();
        }

        private void Update()
        {
            if (_endTurnRequested)
            {
                _endTurnRequested = false;
                if (controller != null && controller.Simulation != null &&
                    controller.Simulation.Phase == BeatPhase.Player)
                    controller.EndBeat();
                return;
            }

            if (controller == null || controller.Simulation == null ||
                controller.Simulation.Phase != BeatPhase.Player ||
                Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            var pointer = Mouse.current.position.ReadValue();
            var guiPointer = new Vector2(pointer.x, Screen.height - pointer.y);
            if (_endTurnRect.Contains(guiPointer)) return;
            TryMoveToScreenPoint(pointer);
        }

        private void TryMoveToScreenPoint(Vector2 screenPoint)
        {
            if (boardCamera == null || boardTilemap == null) return;

            var world = boardCamera.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, -boardCamera.transform.position.z));
            var clicked = boardTilemap.WorldToCell(world);
            var player = controller.Simulation.Player.Position;
            var destination = new Vector2Int(clicked.x, clicked.y);

            if (TryGetMoveDirection(
                    new Vector2Int(player.X, player.Y), destination, out var direction))
                controller.Move(direction);
        }

        private void OnGUI()
        {
            if (controller == null || controller.Simulation == null) return;

            const float width = 160f;
            const float height = 52f;
            _endTurnRect = new Rect(
                Screen.width - width - 20f,
                Screen.height - height - 20f,
                width,
                height);

            var simulation = controller.Simulation;
            var previousEnabled = GUI.enabled;
            GUI.enabled = simulation.Phase == BeatPhase.Player;
            if (GUI.Button(_endTurnRect, "END TURN"))
                _endTurnRequested = true;
            GUI.enabled = previousEnabled;

            GUI.Box(
                new Rect(_endTurnRect.x, _endTurnRect.y - 54f, width, 44f),
                $"Beat {simulation.Run.BeatNumber}   WC {simulation.Run.Wc}");
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
