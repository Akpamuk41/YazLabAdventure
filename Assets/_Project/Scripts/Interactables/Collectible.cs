using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>Oyuncu üzerinden geçince toplanan görev nesnesi (ör. altın). Hangi göreve ait olduğunu olayla bildirir.</summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour, IResettable
    {
        [SerializeField] private string questId = "gold";
        [SerializeField] private Transform visual;
        [SerializeField] private float spinSpeed = 120f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Collider _trigger;
        private Vector3 _visualBasePosition;

        public bool IsCollected { get; private set; }

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            if (visual != null) _visualBasePosition = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null || IsCollected) return;

            visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            visual.localPosition = _visualBasePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected || other.GetComponentInParent<IPlayerActor>() == null) return;

            SetCollected(true);
            EventBus.Publish(new CollectiblePickedEvent(questId));
        }

        public void ResetState() => SetCollected(false);

        private void SetCollected(bool collected)
        {
            IsCollected = collected;
            _trigger.enabled = !collected;
            if (visual != null) visual.gameObject.SetActive(!collected);
        }
    }
}
