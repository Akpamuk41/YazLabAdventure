using System.Collections;
using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>
    /// Menteşesi uçurumun karşı kenarında olan asma köprü. Başlangıçta uçuruma sarkıktır,
    /// etkinleştirildiğinde yatay konuma inerek iki kenarı birleştirir ve NavMesh'i güncelletir.
    /// </summary>
    public class DrawBridge : MonoBehaviour, IActivatable, IResettable
    {
        [Tooltip("Köprü gövdesini taşıyan menteşe (dönme noktası).")]
        [SerializeField] private Transform hinge;
        [SerializeField] private float retractedAngle = -90f;
        [SerializeField] private float extendedAngle = 0f;
        [SerializeField] private float extendDuration = 2.5f;
        [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Coroutine _routine;

        public bool IsActive { get; private set; }

        private void Awake() => SetAngle(retractedAngle);

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            Animate(retractedAngle, extendedAngle);
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            Animate(extendedAngle, retractedAngle);
        }

        public void ResetState()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;

            bool wasActive = IsActive;
            IsActive = false;
            SetAngle(retractedAngle);

            if (!wasActive) return;
            EventBus.Publish(new NavMeshDirtyEvent());
            EventBus.Publish(new BridgeStateChangedEvent(false));
        }

        private void Animate(float from, float to)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AnimateRoutine(from, to));
        }

        private IEnumerator AnimateRoutine(float from, float to)
        {
            for (float t = 0f; t < extendDuration; t += Time.deltaTime)
            {
                SetAngle(Mathf.LerpUnclamped(from, to, easing.Evaluate(t / extendDuration)));
                yield return null;
            }

            SetAngle(to);
            _routine = null;

            // Fizik motorunun yeni collider konumunu görmesi için bir fizik adımı bekle.
            yield return new WaitForFixedUpdate();
            EventBus.Publish(new NavMeshDirtyEvent());
            EventBus.Publish(new BridgeStateChangedEvent(IsActive));
            if (IsActive) EventBus.Publish(new HudMessageEvent("Köprü indi!", new Color(0.5f, 1f, 0.5f)));
        }

        private void SetAngle(float angle)
        {
            var target = hinge != null ? hinge : transform;
            target.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
