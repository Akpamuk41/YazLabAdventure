using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>Oyuncu bu alana girdiğinde düşme bölgelerinin yeniden doğma noktasını günceller.</summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private KillZone[] killZones;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<IPlayerActor>() == null) return;

            foreach (var zone in killZones)
            {
                if (zone != null) zone.SetRespawnPoint(respawnPoint);
            }
        }
    }
}
