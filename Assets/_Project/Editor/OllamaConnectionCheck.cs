using System.Threading;
using UnityEditor;
using UnityEngine;
using YazLab.Dialogue;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Oyuna girmeden Ollama bağlantısını ve JSON ayrıştırmayı dener. Sunucu kapalıyken çalıştırılırsa
    /// hata yönetiminin çökme olmadan uyarı verdiği görülür.
    /// </summary>
    public static class OllamaConnectionCheck
    {
        [MenuItem("YazLab/Ollama Bağlantısını Dene")]
        public static async void Run()
        {
            var settings = new OllamaSettings();
            ILlmClient client = new OllamaClient(settings);
            var messages = new[]
            {
                ChatMessage.System("Sen bir kale kapısının bekçisisin. Türkçe, kısa ve doğal konuş. " +
                                   "Yalnızca reply, mood, trust_change, open_door alanlarından oluşan JSON ver."),
                ChatMessage.User("Merhaba")
            };

            Debug.Log($"[Ollama] {settings.baseUrl} / {settings.model} deneniyor...");
            var response = await client.ChatAsync(messages, GuardDecision.JsonSchema, CancellationToken.None);

            if (!response.Success)
            {
                Debug.LogWarning($"[Ollama] Bağlantı başarısız ({response.Error}): {response.Details}");
                return;
            }

            Debug.Log($"[Ollama] Ham yanıt ({response.Latency:0.0} sn): {response.Content}");
            if (GuardDecisionParser.TryParse(response.Content, out var decision, out var error))
            {
                Debug.Log($"[Ollama] Ayrıştırıldı: reply=\"{decision.Reply}\" mood={decision.Mood} " +
                          $"trust={decision.TrustChange} open_door={decision.OpenDoor}");
            }
            else
            {
                Debug.LogWarning($"[Ollama] Yanıt ayrıştırılamadı: {error}");
            }
        }
    }
}
