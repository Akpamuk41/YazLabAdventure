using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>Uçuruma düşen karakteri son kontrol noktasına geri getirir.</summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;

        public void SetRespawnPoint(Transform point) => respawnPoint = point;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (respawnPoint == null) return;

            var respawnable = other.GetComponentInParent<IRespawnable>();
            if (respawnable == null) return;

            respawnable.Respawn(respawnPoint.position, respawnPoint.rotation);
            EventBus.Publish(new HudMessageEvent("Uçuruma düştün! Kontrol noktasına dönüldü.", new Color(1f, 0.6f, 0.4f)));
            if (other.GetComponentInParent<IPlayerActor>() != null) EventBus.Publish(new PlayerRespawnedEvent());
        }
    }
}
