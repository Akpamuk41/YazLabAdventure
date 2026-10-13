using UnityEngine;

namespace YazLab.Characters
{
    /// <summary>
    /// Karakterin gerçek yer değiştirme hızını ölçüp Animator'daki "Speed" parametresine yazar.
    /// Hareketi kim üretirse üretsin (CharacterController, NavMeshAgent) aynı şekilde çalışır.
    /// </summary>
    public class LocomotionAnimator : MonoBehaviour, ICharacterAnimator
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int InteractHash = Animator.StringToHash("Interact");

        [SerializeField] private Animator animator;
        [SerializeField] private float smoothing = 10f;

        [Tooltip("Model ölçeğine göre animasyon hızını normalize etmek için referans yürüme hızı.")]
        [SerializeField] private float speedScale = 1f;

        private Vector3 _lastPosition;
        private float _speed;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null || Time.deltaTime <= 0f) return;

            var delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;

            float rawSpeed = delta.magnitude / Time.deltaTime;

            // Işınlanma (respawn/warp) ani hız sıçraması üretmesin.
            if (rawSpeed > 30f) rawSpeed = 0f;

            _speed = Mathf.Lerp(_speed, rawSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            animator.SetFloat(SpeedHash, _speed * speedScale);
        }

        public void PlayInteract()
        {
            if (animator != null) animator.SetTrigger(InteractHash);
        }
    }
}
