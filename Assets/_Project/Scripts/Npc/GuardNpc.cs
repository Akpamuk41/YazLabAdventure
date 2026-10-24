using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YazLab.Core;
using YazLab.Dialogue;

namespace YazLab.Npc
{
    /// <summary>
    /// Kilitli kapıyı koruyan LLM bekçisi. Oyuncu E'ye basınca diyalog istenir; sohbet mantığı
    /// <see cref="GuardConversation"/>'da, kapı ise <see cref="IActivatable"/> üzerinden açılır.
    /// Diyalog penceresi bu sınıfın olaylarını dinler, bekçi pencereyi tanımaz.
    /// </summary>
    public class GuardNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private OllamaSettings ollama = new();
        [SerializeField] private GuardPersona persona = new();
        [SerializeField] private Transform interactionPoint;

        [Tooltip("IActivatable uygulayan kapı.")]
        [SerializeField] private MonoBehaviour door;

        [SerializeField] private float turnSpeed = 6f;

        private GuardConversation _conversation;
        private CancellationTokenSource _cancellation;
        private Transform _lookTarget;

        /// <summary>Oyuncu bekçiyle konuşmak istedi.</summary>
        public event Action DialogueRequested;

        /// <summary>Bekçi bir mesaja cevap verdi (veya hata uyarısı oluştu).</summary>
        public event Action<GuardTurn> TurnCompleted;

        public string DisplayName => $"{persona.guardName} ile konuş";
        public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;
        public GuardConversation Conversation => _conversation ??= new GuardConversation(new OllamaClient(ollama), persona);
        public bool IsThinking => Conversation.IsBusy;

        private void OnValidate()
        {
            if (door != null && door is not IActivatable) door = null;
        }

        private void OnDestroy()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }

        private void Update()
        {
            if (_lookTarget == null) return;

            var direction = _lookTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && actor.GetComponent<IPlayerActor>() != null && !Conversation.IsFinished;
        }

        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return;

            _lookTarget = actor.transform;
            DialogueRequested?.Invoke();
        }

        /// <summary>Oyuncunun yazdığı mesajı bekçiye iletir. Oyun beklerken donmaz.</summary>
        public async Task<GuardTurn> SayAsync(string playerText)
        {
            _cancellation ??= new CancellationTokenSource();
            var turn = await Conversation.SendAsync(playerText, _cancellation.Token);
            if (this == null) return turn;

            if (turn.DoorOpened) OpenDoor();
            else if (turn.GaveUp) EventBus.Publish(new HudMessageEvent($"{persona.guardName} seninle konuşmak istemiyor.", new Color(1f, 0.5f, 0.4f)));

            TurnCompleted?.Invoke(turn);
            return turn;
        }

        /// <summary>Diyalog kapanınca bekleyen istek iptal edilir.</summary>
        public void CancelPending()
        {
            if (_cancellation == null) return;
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }

        private void OpenDoor()
        {
            (door as IActivatable)?.Activate();
            EventBus.Publish(new HudMessageEvent($"{persona.guardName} kapıyı açtı!", new Color(0.5f, 1f, 0.5f)));
            EventBus.Publish(new ObjectiveChangedEvent("Kapıdan geç ve hedefe ulaş."));
        }
    }
}
