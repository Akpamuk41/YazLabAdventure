using UnityEngine;

namespace YazLab.Companion.Sensors
{
    /// <summary>
    /// Ajanın önüne yelpaze şeklinde ışınlar göndererek engellerin mesafesini/açısını ölçer
    /// ve hedefe görüş hattı (line of sight) olup olmadığını kontrol eder.
    /// </summary>
    public class RaycastSensor : MonoBehaviour, ISensor
    {
        [SerializeField, Min(1)] private int rayCount = 11;
        [SerializeField, Range(10f, 360f)] private float fieldOfView = 140f;
        [SerializeField] private float range = 6f;
        [SerializeField] private float eyeHeight = 0.5f;

        [Tooltip("Statik ve hareketli tüm engel katmanları.")]
        [SerializeField] private LayerMask obstacleMask;

        [Tooltip("Ajanın beklemesini gerektiren hareketli engel katmanları.")]
        [SerializeField] private LayerMask dynamicObstacleMask;

        [Tooltip("Hareketli engelin 'önümde' sayıldığı koni yarı açısı.")]
        [SerializeField] private float forwardConeHalfAngle = 25f;
        [SerializeField] private float stopDistance = 1.6f;

        private float[] _angles;
        private float[] _distances;
        private SensorReading _lastReading;

        private Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        private void Awake() => AllocateBuffers();

        private void OnValidate() => AllocateBuffers();

        private void AllocateBuffers()
        {
            _angles = new float[rayCount];
            _distances = new float[rayCount];
            for (int i = 0; i < rayCount; i++)
            {
                _angles[i] = rayCount == 1 ? 0f : Mathf.Lerp(-fieldOfView / 2f, fieldOfView / 2f, i / (rayCount - 1f));
            }
        }

        public SensorReading Sense(Transform target)
        {
            if (_angles == null || _angles.Length != rayCount) AllocateBuffers();

            bool obstacleDetected = false;
            bool dynamicAhead = false;
            float nearestDistance = range;
            float nearestAngle = 0f;

            for (int i = 0; i < rayCount; i++)
            {
                var direction = Quaternion.AngleAxis(_angles[i], Vector3.up) * transform.forward;
                if (Physics.Raycast(Eye, direction, out var hit, range, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    _distances[i] = hit.distance;
                    obstacleDetected = true;

                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        nearestAngle = _angles[i];
                    }

                    bool isDynamic = (dynamicObstacleMask.value & (1 << hit.collider.gameObject.layer)) != 0;
                    if (isDynamic && Mathf.Abs(_angles[i]) <= forwardConeHalfAngle && hit.distance <= stopDistance)
                    {
                        dynamicAhead = true;
                    }
                }
                else
                {
                    _distances[i] = range;
                }
            }

            bool hasTarget = target != null;
            bool targetVisible = false;
            float targetDistance = 0f;
            float targetAngle = 0f;

            if (hasTarget)
            {
                var toTarget = target.position - transform.position;
                toTarget.y = 0f;
                targetDistance = toTarget.magnitude;
                targetAngle = Vector3.SignedAngle(transform.forward, toTarget, Vector3.up);
                targetVisible = HasLineOfSight(target);
            }

            _lastReading = new SensorReading((float[])_angles.Clone(), (float[])_distances.Clone(),
                obstacleDetected, nearestDistance, nearestAngle, dynamicAhead,
                hasTarget, targetVisible, targetDistance, targetAngle);
            return _lastReading;
        }

        /// <summary>Hedefe giden ışın önce bir engele çarpıyorsa hedef görünmüyordur.</summary>
        private bool HasLineOfSight(Transform target)
        {
            var targetPoint = target.position + Vector3.up * eyeHeight;
            var direction = targetPoint - Eye;
            float distance = direction.magnitude;
            if (distance < 0.01f) return true;

            if (!Physics.Raycast(Eye, direction / distance, out var hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        private void OnDrawGizmosSelected()
        {
            if (_lastReading.RayDistances == null) return;

            for (int i = 0; i < _lastReading.RayAngles.Length; i++)
            {
                var direction = Quaternion.AngleAxis(_lastReading.RayAngles[i], Vector3.up) * transform.forward;
                bool hit = _lastReading.RayDistances[i] < range;
                Gizmos.color = hit ? Color.red : Color.green;
                Gizmos.DrawLine(Eye, Eye + direction * _lastReading.RayDistances[i]);
            }
        }
    }
}
