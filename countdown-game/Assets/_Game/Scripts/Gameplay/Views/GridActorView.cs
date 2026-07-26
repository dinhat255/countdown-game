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
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

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

        public void PlayAnimation(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return;
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator == null || !_animator.isActiveAndEnabled) return;

            int stateHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (!_animator.HasState(0, stateHash))
            {
                Debug.LogWarning(
                    $"[Countdown] {name} has no animation state named '{stateName}'.",
                    this);
                return;
            }

            _animator.Play(stateHash, 0, 0f);
        }

        public void FaceMovement(GridCoord origin, GridCoord landing)
        {
            if (origin.X == landing.X) return;
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) return;

            _spriteRenderer.flipX = landing.X > origin.X;
        }
    }
}
