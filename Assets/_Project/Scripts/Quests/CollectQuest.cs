using System.Collections.Generic;
using UnityEngine;
using YazLab.Core;

namespace YazLab.Quests
{
    /// <summary>
    /// Ara bölüm görevi: belirli sayıda nesne toplanınca bağlı mekanizmaları (geçit vb.) açar.
    /// Toplanan nesneleri tanımaz, yalnızca CollectiblePickedEvent olaylarını sayar.
    /// </summary>
    public class CollectQuest : MonoBehaviour, IResettable
    {
        [SerializeField] private string questId = "gold";
        [SerializeField, Min(1)] private int required = 3;
        [SerializeField] private string itemName = "Altın";
        [SerializeField] private string completedMessage = "Geçit açıldı!";

        [Tooltip("Görev bitince Activate() çağrılacak IActivatable bileşenler.")]
        [SerializeField] private List<MonoBehaviour> targets = new();

        [Tooltip("Görev sıfırlanırken birlikte sıfırlanacak toplanabilir nesneler.")]
        [SerializeField] private List<MonoBehaviour> collectibles = new();

        public int Collected { get; private set; }
        public int Required => required;
        public bool IsComplete => Collected >= required;

        private void OnEnable() => EventBus.Subscribe<CollectiblePickedEvent>(OnPicked);

        private void OnDisable() => EventBus.Unsubscribe<CollectiblePickedEvent>(OnPicked);

        private void OnValidate()
        {
            targets.RemoveAll(t => t != null && t is not IActivatable);
            collectibles.RemoveAll(c => c != null && c is not IResettable);
        }

        private void Start() => PublishProgress();

        private void OnPicked(CollectiblePickedEvent e)
        {
            if (e.QuestId != questId || IsComplete) return;

            Collected++;
            PublishProgress();

            if (!IsComplete)
            {
                EventBus.Publish(new HudMessageEvent($"{itemName} toplandı ({Collected}/{required})", new Color(1f, 0.85f, 0.3f)));
                return;
            }

            EventBus.Publish(new HudMessageEvent(completedMessage, new Color(0.5f, 1f, 0.5f)));
            foreach (var target in targets)
            {
                if (target is IActivatable activatable) activatable.Activate();
            }
        }

        public void ResetState()
        {
            Collected = 0;
            foreach (var collectible in collectibles)
            {
                if (collectible is IResettable resettable) resettable.ResetState();
            }

            foreach (var target in targets)
            {
                if (target is IResettable resettable) resettable.ResetState();
            }

            PublishProgress();
        }

        private void PublishProgress() => EventBus.Publish(new QuestProgressEvent(questId, Collected, required));
    }
}
