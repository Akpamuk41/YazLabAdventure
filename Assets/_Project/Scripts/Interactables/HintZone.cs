using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>Oyuncu alanın içindeyken ekranın altında bağlamsal bir ipucu gösterir.</summary>
    [RequireComponent(typeof(Collider))]
    public class HintZone : MonoBehaviour
    {
        [SerializeField, TextArea] private string hint;
        [SerializeField] private bool disableWhenBridgeExtended = true;

        private bool _playerInside;
        private bool _suppressed;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnEnable() => EventBus.Subscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);

        private void OnDisable() => EventBus.Unsubscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<IPlayerActor>() == null) return;
            _playerInside = true;
            Refresh();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<IPlayerActor>() == null) return;
            _playerInside = false;
            Refresh();
        }

        private void OnBridgeStateChanged(BridgeStateChangedEvent e)
        {
            if (!disableWhenBridgeExtended) return;
            _suppressed = e.IsExtended;
            Refresh();
        }

        private void Refresh()
        {
            EventBus.Publish(new HintChangedEvent(_playerInside && !_suppressed ? hint : string.Empty));
        }
    }
}
