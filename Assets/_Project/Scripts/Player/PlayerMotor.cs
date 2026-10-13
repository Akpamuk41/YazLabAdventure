using UnityEngine;
using YazLab.Core;

namespace YazLab.Player
{
    /// <summary>Kameraya göre yönlenen CharacterController tabanlı oyuncu hareketi.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour, IPlayerActor, IRespawnable
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private IPlayerInput _input;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public Transform Transform => transform;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<IPlayerInput>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            var move = _input?.Move ?? Vector2.zero;
            var direction = CameraRelative(move);

            float targetSpeed = (_input != null && _input.SprintHeld) ? sprintSpeed : walkSpeed;
            var targetVelocity = direction * (targetSpeed * move.magnitude);
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * targetSpeed * Time.deltaTime);

            if (direction.sqrMagnitude > 0.001f)
            {
                var targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            _verticalVelocity = _controller.isGrounded ? -2f : _verticalVelocity + gravity * Time.deltaTime;

            var velocity = _horizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            // CharacterController aktifken transform'a yazılan konum bir sonraki Move'da ezilir.
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

            var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;

            return (forward.normalized * input.y + right.normalized * input.x).normalized;
        }
    }
}
