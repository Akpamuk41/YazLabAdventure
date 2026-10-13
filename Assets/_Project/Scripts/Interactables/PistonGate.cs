using UnityEngine;
using UnityEngine.AI;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>
    /// Koridoru periyodik olarak kapatan dinamik engel. Yukarıdayken NavMesh'i oyarak (carve)
    /// yolu kapatır; yoldaşın Raycast sensörü de bu engeli algılayıp bekler veya yeniden rota çizer.
    /// </summary>
    [RequireComponent(typeof(NavMeshObstacle))]
    public class PistonGate : MonoBehaviour, IResettable
    {
        [SerializeField] private float upDuration = 4f;
        [SerializeField] private float downDuration = 4f;
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private float travel = 2.6f;
        [SerializeField] private float phaseOffset;
        [SerializeField] private bool randomizePhaseOnReset = true;

        [Tooltip("Engelin yolu kapattığı kabul edilen minimum yükseklik oranı.")]
        [SerializeField, Range(0f, 1f)] private float blockingThreshold = 0.35f;

        private NavMeshObstacle _obstacle;
        private Vector3 _upPosition;
        private float _timer;

        public bool IsBlocking { get; private set; }

        private float CycleLength => upDuration + downDuration + 2f * moveDuration;

        private void Awake()
        {
            _obstacle = GetComponent<NavMeshObstacle>();
            _obstacle.carving = true;
            _obstacle.carveOnlyStationary = false;
            _upPosition = transform.localPosition;
            _timer = phaseOffset;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float height01 = EvaluateHeight(_timer % CycleLength);

            transform.localPosition = _upPosition + Vector3.down * (travel * (1f - height01));

            bool blocking = height01 > blockingThreshold;
            if (blocking == IsBlocking) return;

            IsBlocking = blocking;
            _obstacle.enabled = blocking;
        }

        public void ResetState()
        {
            _timer = randomizePhaseOnReset ? Random.Range(0f, CycleLength) : phaseOffset;
        }

        /// <summary>Döngü: yukarıda bekle → in → aşağıda bekle → çık.</summary>
        private float EvaluateHeight(float t)
        {
            if (t < upDuration) return 1f;
            t -= upDuration;
            if (t < moveDuration) return 1f - t / moveDuration;
            t -= moveDuration;
            if (t < downDuration) return 0f;
            t -= downDuration;
            return t / moveDuration;
        }
    }
}
