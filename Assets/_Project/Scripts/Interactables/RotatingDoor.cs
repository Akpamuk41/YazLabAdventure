using System.Collections;
using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>
    /// Menteşe etrafında dönerek açılan kapı. NPC bekçisinin koruduğu kilitli kapı olarak kullanılır;
    /// LLM kararı "open_door" olduğunda Activate() çağrılacaktır.
    /// </summary>
    public class RotatingDoor : MonoBehaviour, IActivatable, IResettable
    {
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float duration = 1.2f;

        private Quaternion _closedRotation;
        private Coroutine _routine;

        public bool IsActive { get; private set; }

        private Transform Leaf => doorLeaf != null ? doorLeaf : transform;

        private void Awake() => _closedRotation = Leaf.localRotation;

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            RotateTo(_closedRotation * Quaternion.Euler(0f, openAngle, 0f));
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            RotateTo(_closedRotation);
        }

        public void ResetState()
        {
            if (_routine != null) StopCoroutine(_routine);
            IsActive = false;
            Leaf.localRotation = _closedRotation;
        }

        private void RotateTo(Quaternion target)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RotateRoutine(target));
        }

        private IEnumerator RotateRoutine(Quaternion target)
        {
            var start = Leaf.localRotation;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                Leaf.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }

            Leaf.localRotation = target;
            _routine = null;
            yield return new WaitForFixedUpdate();
            EventBus.Publish(new NavMeshDirtyEvent());
        }
    }
}
