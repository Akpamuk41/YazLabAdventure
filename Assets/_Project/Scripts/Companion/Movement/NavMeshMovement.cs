using UnityEngine;
using UnityEngine.AI;

namespace YazLab.Companion.Movement
{
    /// <summary>NavMeshAgent üzerinden A* tabanlı yol bulma ile hareket.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [DefaultExecutionOrder(-50)]
    public class NavMeshMovement : MonoBehaviour, IMovementStrategy
    {
        [SerializeField] private float destinationSampleRadius = 2f;

        private NavMeshAgent _agent;

        public MovementPathStatus PathStatus
        {
            get
            {
                if (!_agent.isOnNavMesh) return MovementPathStatus.Invalid;
                if (_agent.pathPending) return MovementPathStatus.Pending;
                if (!_agent.hasPath) return MovementPathStatus.None;

                return _agent.pathStatus switch
                {
                    NavMeshPathStatus.PathComplete => MovementPathStatus.Complete,
                    NavMeshPathStatus.PathPartial => MovementPathStatus.Partial,
                    _ => MovementPathStatus.Invalid
                };
            }
        }

        public float RemainingDistance => _agent.pathPending ? float.PositiveInfinity : _agent.remainingDistance;

        public Vector3 Velocity => _agent.velocity;

        private void Awake() => _agent = GetComponent<NavMeshAgent>();

        // Ajan sahnede kapalı gelir: NavMeshSurface verisini OnEnable'da ekliyor, ajan ondan önce açılırsa
        // "no valid NavMesh" hatası veriyor. Start tüm OnEnable'lardan sonra çalışır.
        private void Start() => _agent.enabled = true;

        public bool MoveTo(Vector3 destination)
        {
            if (!_agent.isOnNavMesh) return false;

            // Hedef NavMesh dışında olabilir (ör. şalterin gövdesi); en yakın yürünebilir noktaya yönel.
            if (NavMesh.SamplePosition(destination, out var hit, destinationSampleRadius, _agent.areaMask))
            {
                destination = hit.position;
            }

            _agent.isStopped = false;
            _agent.updateRotation = true;
            return _agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }

        public void SetSpeed(float speed) => _agent.speed = speed;

        public bool HasArrived(float tolerance)
        {
            if (!_agent.isOnNavMesh || _agent.pathPending) return false;
            return !_agent.hasPath || _agent.remainingDistance <= Mathf.Max(tolerance, _agent.stoppingDistance);
        }

        public void RotateTowards(Vector3 worldPoint, float degreesPerSecond)
        {
            var direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            _agent.updateRotation = false;
            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, degreesPerSecond * Time.deltaTime);
        }

        public void Warp(Vector3 position, Quaternion rotation)
        {
            if (NavMesh.SamplePosition(position, out var hit, destinationSampleRadius, _agent.areaMask))
            {
                position = hit.position;
            }

            _agent.Warp(position);
            transform.rotation = rotation;
            Stop();
        }
    }
}
