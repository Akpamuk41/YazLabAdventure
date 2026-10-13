using UnityEngine;

namespace YazLab.Core
{
    /// <summary>
    /// Oyun akışındaki görev metnini olaylara göre günceller.
    /// Senaryo: köprüyü indir → kapıya ulaş → bekçiyi ikna et (LLM NPC) → hedefe ulaş.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private string startObjective = "Uçurumun karşısına geç. Köprü kalkık!";
        [SerializeField] private string bridgeObjective = "Köprü indi! Karşıya geç ve kapıya ilerle.";
        [SerializeField] private string completedObjective = "Tebrikler, macerayı tamamladın!";

        private void OnEnable()
        {
            EventBus.Subscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);
            EventBus.Subscribe<GameCompletedEvent>(OnGameCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);
            EventBus.Unsubscribe<GameCompletedEvent>(OnGameCompleted);
        }

        private void Start() => EventBus.Publish(new ObjectiveChangedEvent(startObjective));

        private void OnBridgeStateChanged(BridgeStateChangedEvent e)
        {
            EventBus.Publish(new ObjectiveChangedEvent(e.IsExtended ? bridgeObjective : startObjective));
        }

        private void OnGameCompleted(GameCompletedEvent e)
        {
            EventBus.Publish(new ObjectiveChangedEvent(completedObjective));
        }
    }
}
