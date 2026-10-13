using UnityEngine;
using UnityEngine.UI;
using YazLab.Core;

namespace YazLab.UI
{
    /// <summary>Görev, ipucu, yoldaş durumu ve geçici mesajları gösteren arayüz. Yalnızca olayları dinler.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text hintText;
        [SerializeField] private Text companionStateText;
        [SerializeField] private Text messageText;
        [SerializeField] private float messageDuration = 3.5f;
        [SerializeField] private float messageFade = 0.6f;

        private float _messageTimer;
        private Color _messageColor = Color.white;

        private void OnEnable()
        {
            EventBus.Subscribe<ObjectiveChangedEvent>(OnObjectiveChanged);
            EventBus.Subscribe<HintChangedEvent>(OnHintChanged);
            EventBus.Subscribe<CompanionStateChangedEvent>(OnCompanionStateChanged);
            EventBus.Subscribe<HudMessageEvent>(OnHudMessage);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ObjectiveChangedEvent>(OnObjectiveChanged);
            EventBus.Unsubscribe<HintChangedEvent>(OnHintChanged);
            EventBus.Unsubscribe<CompanionStateChangedEvent>(OnCompanionStateChanged);
            EventBus.Unsubscribe<HudMessageEvent>(OnHudMessage);
        }

        private void Start()
        {
            if (hintText != null) hintText.text = string.Empty;
            if (messageText != null) messageText.text = string.Empty;
        }

        private void Update()
        {
            if (messageText == null || _messageTimer <= 0f) return;

            _messageTimer -= Time.unscaledDeltaTime;
            var color = _messageColor;
            color.a = Mathf.Clamp01(_messageTimer / messageFade);
            messageText.color = color;
        }

        private void OnObjectiveChanged(ObjectiveChangedEvent e)
        {
            if (objectiveText != null) objectiveText.text = $"Görev: {e.Objective}";
        }

        private void OnHintChanged(HintChangedEvent e)
        {
            if (hintText != null) hintText.text = e.Hint;
        }

        private void OnCompanionStateChanged(CompanionStateChangedEvent e)
        {
            if (companionStateText != null) companionStateText.text = $"Robi: {e.NewState}";
        }

        private void OnHudMessage(HudMessageEvent e)
        {
            if (messageText == null) return;

            messageText.text = e.Message;
            _messageColor = e.Color;
            messageText.color = e.Color;
            _messageTimer = messageDuration;
        }
    }
}
