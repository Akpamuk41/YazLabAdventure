using UnityEngine;
using YazLab.Core;

namespace YazLab.Player
{
    /// <summary>Oyuncunun yakınındaki etkileşimli nesneleri bulur ve E tuşuyla kullanır (ör. NPC ile konuşma).</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 2f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly Collider[] _buffer = new Collider[16];
        private IPlayerInput _input;
        private IInteractable _current;

        private void Awake() => _input = GetComponent<IPlayerInput>();

        private void Update()
        {
            var nearest = FindNearest();
            if (!ReferenceEquals(nearest, _current))
            {
                _current = nearest;
                EventBus.Publish(new HintChangedEvent(_current != null ? $"[E] {_current.DisplayName}" : string.Empty));
            }

            if (_current != null && _input != null && _input.InteractPressed)
            {
                _current.Interact(gameObject);
            }
        }

        private IInteractable FindNearest()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _buffer, interactableMask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var interactable = _buffer[i].GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract(gameObject)) continue;

                float distance = (_buffer[i].transform.position - transform.position).sqrMagnitude;
                if (distance >= bestDistance) continue;

                best = interactable;
                bestDistance = distance;
            }

            return best;
        }
    }
}
