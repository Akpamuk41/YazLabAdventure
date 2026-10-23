using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YazLab.Dialogue
{
    /// <summary>
    /// Yerel dil modeli soyutlaması. NPC hangi sunucunun (Ollama vb.) kullanıldığını bilmez.
    /// </summary>
    public interface ILlmClient
    {
        /// <param name="messages">System promptu dahil tüm sohbet geçmişi.</param>
        /// <param name="jsonSchema">Modelden istenen çıktı şeması (JSON). Boşsa serbest metin istenir.</param>
        Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellationToken);
    }
}
