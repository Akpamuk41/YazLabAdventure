using UnityEngine;
using UnityEngine.InputSystem;
using YazLab.Player;

namespace YazLab.CameraSystem
{
    /// <summary>Hedef etrafında fareyle dönen, duvarlara çarpınca yaklaşan üçüncü şahıs kamerası.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("IPlayerInput uygulayan bileşen.")]
        [SerializeField] private MonoBehaviour inputBehaviour;
        [SerializeField] private Vector3 pivotOffset = new(0f, 1.5f, 0f);
        [SerializeField] private float distance = 7f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float followSharpness = 15f;

        private IPlayerInput _input;
        private float _yaw;
        private float _pitch = 25f;
        private float _currentDistance;

        private void Awake()
        {
            _input = inputBehaviour as IPlayerInput;
            _currentDistance = distance;
            if (target != null) _yaw = target.eulerAngles.y;
        }

        private void Start() => LockCursor(true);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && _input is { Enabled: true }) LockCursor(true);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var look = _input?.Look ?? Vector2.zero;
            _yaw += look.x;
            _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var pivot = target.position + pivotOffset;
            var direction = rotation * Vector3.back;

            float desired = distance;
            if (Physics.SphereCast(pivot, collisionRadius, direction, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desired = Mathf.Max(0.5f, hit.distance);
            }

            // Engelden hemen yaklaş, yavaşça uzaklaş.
            _currentDistance = desired < _currentDistance
                ? desired
                : Mathf.Lerp(_currentDistance, desired, 1f - Mathf.Exp(-followSharpness * 0.3f * Time.deltaTime));

            transform.SetPositionAndRotation(pivot + direction * _currentDistance, rotation);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
