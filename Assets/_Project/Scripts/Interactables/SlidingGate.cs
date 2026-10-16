using System.Collections;
using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>
    /// Etkinleştirilince zemine gömülerek yolu açan geçit. Hareket bitince NavMesh'i güncelletir,
    /// böylece yoldaş da açılan yoldan geçebilir.
    /// </summary>
    public class SlidingGate : MonoBehaviour, IActivatable, IResettable
    {
        [SerializeField] private float travel = 2.6f;
        [SerializeField] private float duration = 1.5f;

        private Vector3 _closedPosition;
        private Coroutine _routine;

        public bool IsActive { get; private set; }

        private Vector3 OpenPosition => _closedPosition + Vector3.down * travel;

        private void Awake() => _closedPosition = transform.localPosition;

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            MoveTo(OpenPosition);
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            MoveTo(_closedPosition);
        }

        public void ResetState()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;

            bool wasActive = IsActive;
            IsActive = false;
            transform.localPosition = _closedPosition;
            if (wasActive) EventBus.Publish(new NavMeshDirtyEvent());
        }

        private void MoveTo(Vector3 target)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(MoveRoutine(target));
        }

        private IEnumerator MoveRoutine(Vector3 target)
        {
            var start = transform.localPosition;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }

            transform.localPosition = target;
            _routine = null;

            yield return new WaitForFixedUpdate();
            EventBus.Publish(new NavMeshDirtyEvent());
        }
    }
}
