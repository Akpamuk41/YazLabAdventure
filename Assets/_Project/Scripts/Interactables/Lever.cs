using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>
    /// Bağlı mekanizmaları (köprü vb.) tetikleyen şalter.
    /// Yalnızca izin verilen katmandaki aktörler (yoldaş) kullanabilir.
    /// </summary>
    public class Lever : MonoBehaviour, IInteractable, IResettable
    {
        [SerializeField] private string leverId = "BridgeLever";
        [SerializeField] private string displayName = "Şalter";
        [SerializeField] private Transform handle;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private LayerMask allowedActors = ~0;
        [SerializeField] private float offAngle = -35f;
        [SerializeField] private float onAngle = 35f;
        [SerializeField] private float pullDuration = 0.6f;

        [Tooltip("IActivatable uygulayan bileşenler.")]
        [SerializeField] private List<MonoBehaviour> targets = new();

        private Quaternion _handleBaseRotation;
        private Coroutine _pullRoutine;

        public string LeverId => leverId;
        public string DisplayName => displayName;
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (handle != null) _handleBaseRotation = handle.localRotation;
            SetHandleAngle(offAngle);
        }

        private void OnValidate()
        {
            targets.RemoveAll(t => t != null && t is not IActivatable);
        }

        public bool CanInteract(GameObject actor)
        {
            return !IsActivated && actor != null && (allowedActors.value & (1 << actor.layer)) != 0;
        }

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return;

            IsActivated = true;
            _pullRoutine = StartCoroutine(PullRoutine());
        }

        public void ResetState()
        {
            if (_pullRoutine != null) StopCoroutine(_pullRoutine);
            IsActivated = false;
            SetHandleAngle(offAngle);
        }

        private IEnumerator PullRoutine()
        {
            for (float t = 0f; t < pullDuration; t += Time.deltaTime)
            {
                SetHandleAngle(Mathf.Lerp(offAngle, onAngle, t / pullDuration));
                yield return null;
            }

            SetHandleAngle(onAngle);
            foreach (var target in targets)
            {
                if (target is IActivatable activatable) activatable.Activate();
            }

            EventBus.Publish(new LeverActivatedEvent(leverId));
            _pullRoutine = null;
        }

        private void SetHandleAngle(float angle)
        {
            if (handle != null) handle.localRotation = _handleBaseRotation * Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
