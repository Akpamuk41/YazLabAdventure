using UnityEngine;

namespace YazLab.Core
{
    /// <summary>
    /// Oyun akışındaki görev metnini olaylara göre günceller.
    /// Senaryo: köprüyü indir → altınları toplayıp geçidi aç → bekçiyi ikna et (LLM NPC) → hedefe ulaş.
    /// </summary>
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private string startObjective = "Uçurumun karşısına geç. Köprü kalkık!";
        [Tooltip("Sahnede toplama görevi yoksa köprüden sonra gösterilir.")]
        [SerializeField] private string bridgeObjective = "Köprü indi! Karşıya geç ve kapıya ilerle.";
        [Tooltip("{0}: toplanan, {1}: gereken.")]
        [SerializeField] private string questObjective = "Geçidi açmak için altınları topla ({0}/{1})";
        [SerializeField] private string questCompletedObjective = "Geçit açıldı! Kapıdaki bekçiyle konuş.";
        [SerializeField] private string completedObjective = "Tebrikler, macerayı tamamladın!";

        private bool _bridgeExtended;
        private bool _gameCompleted;
        private QuestProgressEvent? _questProgress;

        private void OnEnable()
        {
            EventBus.Subscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);
            EventBus.Subscribe<QuestProgressEvent>(OnQuestProgress);
            EventBus.Subscribe<GameCompletedEvent>(OnGameCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BridgeStateChangedEvent>(OnBridgeStateChanged);
            EventBus.Unsubscribe<QuestProgressEvent>(OnQuestProgress);
            EventBus.Unsubscribe<GameCompletedEvent>(OnGameCompleted);
        }

        private void Start() => Refresh();

        private void OnBridgeStateChanged(BridgeStateChangedEvent e)
        {
            _bridgeExtended = e.IsExtended;
            Refresh();
        }

        private void OnQuestProgress(QuestProgressEvent e)
        {
            _questProgress = e;
            Refresh();
        }

        private void OnGameCompleted(GameCompletedEvent e)
        {
            _gameCompleted = true;
            Refresh();
        }

        private void Refresh() => EventBus.Publish(new ObjectiveChangedEvent(CurrentObjective()));

        private string CurrentObjective()
        {
            if (_gameCompleted) return completedObjective;

            // Görev bittiyse oyuncu zaten karşı taraftadır; köprü durumu artık belirleyici değil.
            if (_questProgress is { IsComplete: true }) return questCompletedObjective;
            if (!_bridgeExtended) return startObjective;
            if (_questProgress is not { } progress) return bridgeObjective;
            return string.Format(questObjective, progress.Collected, progress.Required);
        }
    }
}
