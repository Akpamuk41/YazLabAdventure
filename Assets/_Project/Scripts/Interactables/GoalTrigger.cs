using UnityEngine;
using YazLab.Core;

namespace YazLab.Interactables
{
    /// <summary>Oyuncu kilitli kapının arkasındaki hedefe ulaştığında oyunu bitirir.</summary>
    [RequireComponent(typeof(Collider))]
    public class GoalTrigger : MonoBehaviour
    {
        private bool _completed;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_completed || other.GetComponentInParent<IPlayerActor>() == null) return;

            _completed = true;
            EventBus.Publish(new GameCompletedEvent());
            EventBus.Publish(new HudMessageEvent("Hedefe ulaştın!", new Color(1f, 0.85f, 0.3f)));
        }
    }
}
