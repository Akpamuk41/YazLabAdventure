using System;
using UnityEngine;

namespace YazLab.Dialogue
{
    /// <summary>
    /// Model çıktısını <see cref="GuardDecision"/>'a çevirir. Kırılgan metin eşleştirmesi yapılmaz,
    /// yalnızca JSON okunur. Bozuk/eksik çıktıda false döner; çağıran taraf güvenli bir cevaba düşer.
    /// </summary>
    public static class GuardDecisionParser
    {
        public static bool TryParse(string raw, out GuardDecision decision, out string error)
        {
            decision = default;
            error = string.Empty;

            string json = ExtractJsonObject(raw);
            if (json == null)
            {
                error = "Çıktıda JSON nesnesi yok.";
                return false;
            }

            GuardDecisionDto dto;
            try
            {
                dto = JsonUtility.FromJson<GuardDecisionDto>(json);
            }
            catch (ArgumentException ex)
            {
                error = $"Geçersiz JSON: {ex.Message}";
                return false;
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.reply))
            {
                error = "\"reply\" alanı boş.";
                return false;
            }

            int trust = Mathf.Clamp(dto.trust_change, GuardDecision.MinTrustChange, GuardDecision.MaxTrustChange);
            decision = new GuardDecision(dto.reply.Trim(), ParseMood(dto.mood), trust, dto.open_door);
            return true;
        }

        /// <summary>Model JSON'u ```json bloğuna veya açıklama metnine sararsa ilk {...} kısmını alır.</summary>
        private static string ExtractJsonObject(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            return start >= 0 && end > start ? raw.Substring(start, end - start + 1) : null;
        }

        private static GuardMood ParseMood(string mood) => (mood ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "friendly" => GuardMood.Friendly,
            "angry" => GuardMood.Angry,
            _ => GuardMood.Neutral
        };

        // JsonUtility alan adlarının JSON ile birebir aynı olmasını ister.
        [Serializable]
        private class GuardDecisionDto
        {
            public string reply;
            public string mood;
            public int trust_change;
            public bool open_door;
        }
    }
}
