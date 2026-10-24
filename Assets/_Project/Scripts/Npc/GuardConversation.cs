using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YazLab.Dialogue;

namespace YazLab.Npc
{
    /// <summary>
    /// Bekçiyle yapılan sohbetin durumu: geçmiş, güven puanı ve kapı kararı. MonoBehaviour değildir,
    /// LLM istemcisi dışarıdan verilir (testte sahte istemci takılabilir).
    /// Kapı yalnızca model "open_door" dediğinde VE güven puanı eşiği geçtiğinde açılır; böylece
    /// "kapıyı aç" yazmak tek başına işe yaramaz.
    /// </summary>
    public class GuardConversation
    {
        private const string FixJsonReminder =
            "Önceki cevabın geçerli JSON değildi. Yalnızca reply, mood, trust_change, open_door alanlarıyla JSON ver.";

        private readonly ILlmClient _client;
        private readonly GuardPersona _persona;
        private readonly List<ChatMessage> _history = new();

        public GuardConversation(ILlmClient client, GuardPersona persona)
        {
            _client = client;
            _persona = persona ?? new GuardPersona();
        }

        public int Trust { get; private set; }
        public bool DoorOpened { get; private set; }
        public bool GaveUp { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsFinished => DoorOpened || GaveUp;
        public GuardPersona Persona => _persona;

        public void Reset()
        {
            _history.Clear();
            Trust = 0;
            DoorOpened = false;
            GaveUp = false;
        }

        public async Task<GuardTurn> SendAsync(string playerText, CancellationToken cancellationToken)
        {
            if (IsBusy || IsFinished || string.IsNullOrWhiteSpace(playerText))
            {
                return GuardTurn.Failed(Trust, LlmErrorKind.None, string.Empty);
            }

            IsBusy = true;
            try
            {
                var userMessage = ChatMessage.User(playerText.Trim());
                var response = await _client.ChatAsync(BuildMessages(userMessage, null), GuardDecision.JsonSchema, cancellationToken);
                if (!response.Success) return Failed(response);

                if (!GuardDecisionParser.TryParse(response.Content, out var decision, out var error))
                {
                    // Bozuk çıktıda bir kez daha, düzeltme notuyla sorulur.
                    Debug.LogWarning($"[Bekçi] Geçersiz JSON ({error}), tekrar soruluyor: {response.Content}");
                    response = await _client.ChatAsync(BuildMessages(userMessage, FixJsonReminder), GuardDecision.JsonSchema, cancellationToken);
                    if (!response.Success) return Failed(response);

                    if (!GuardDecisionParser.TryParse(response.Content, out decision, out error))
                    {
                        Debug.LogWarning($"[Bekçi] Yine geçersiz JSON ({error}): {response.Content}");
                        return GuardTurn.Failed(Trust, LlmErrorKind.BadResponse,
                            $"{_persona.guardName} ne dediğini toparlayamadı. Bir daha söyler misin?");
                    }
                }

                return Apply(userMessage, decision);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private GuardTurn Apply(ChatMessage userMessage, GuardDecision decision)
        {
            Trust += decision.TrustChange;
            DoorOpened = decision.OpenDoor && Trust >= _persona.trustToOpen;
            GaveUp = !DoorOpened && Trust <= _persona.minTrust;

            _history.Add(userMessage);
            _history.Add(ChatMessage.Assistant(decision.Reply));
            int overflow = _history.Count - _persona.maxHistoryMessages;
            if (overflow > 0) _history.RemoveRange(0, overflow + overflow % 2);

            Debug.Log($"[Bekçi] mood={decision.Mood} trust={Trust} ({decision.TrustChange:+0;-0;0}) " +
                      $"open_door={decision.OpenDoor} -> kapı {(DoorOpened ? "AÇILDI" : "kapalı")}");
            return new GuardTurn(decision.Reply, decision.Mood, decision.TrustChange, Trust, DoorOpened, GaveUp);
        }

        private List<ChatMessage> BuildMessages(ChatMessage userMessage, string reminder)
        {
            var messages = new List<ChatMessage>(_history.Count + 3)
            {
                ChatMessage.System(_persona.systemPrompt +
                                   $"\nŞu anki güven puanın: {Trust} (kapı için en az {_persona.trustToOpen} gerekir).")
            };
            messages.AddRange(_history);
            messages.Add(userMessage);
            if (reminder != null) messages.Add(ChatMessage.System(reminder));
            return messages;
        }

        private GuardTurn Failed(LlmResponse response)
        {
            Debug.LogWarning($"[Bekçi] LLM hatası ({response.Error}): {response.Details}");
            string notice = response.Error switch
            {
                LlmErrorKind.Unreachable => "Bekçiye ulaşılamıyor: Ollama çalışmıyor gibi. Ollama'yı açıp tekrar dene.",
                LlmErrorKind.ModelNotFound => "Bekçinin dil modeli yüklü değil. Terminalde 'ollama pull llama3.2:3b' çalıştır.",
                LlmErrorKind.Timeout => $"{_persona.guardName} çok uzun düşündü ve cevap veremedi. Tekrar dene.",
                LlmErrorKind.Cancelled => string.Empty,
                _ => $"{_persona.guardName} anlaşılmaz bir şey mırıldandı. Tekrar dene."
            };
            return GuardTurn.Failed(Trust, response.Error, notice);
        }
    }
}
