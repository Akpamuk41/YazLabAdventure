using System.Collections.Generic;
using System.Text;

namespace YazLab.Dialogue
{
    /// <summary>
    /// Küçük model bazen Türkçe cevabın içine İngilizce kelime ya da Çince/Kiril harf karıştırıyor.
    /// Bu sınıf cevabı kabaca kontrol eder; sorun varsa model bir kez daha sorulur.
    /// </summary>
    public static class ReplyLanguageCheck
    {
        // Türkçede ayrı bir anlamı olan kelimeler (can, her, it, on, of...) bilerek listede yok.
        private static readonly HashSet<string> EnglishWords = new()
        {
            "the", "you", "your", "and", "are", "is", "was", "what", "this", "that", "with", "have",
            "please", "sorry", "hello", "welcome", "beautiful", "good", "yes", "door", "king", "queen",
            "why", "here", "there", "not", "my", "me", "to", "for", "will", "can't", "don't", "i'm"
        };

        private const string TurkishLetters = "çğıöşüâîûÇĞİÖŞÜÂÎÛ";
        private static readonly char[] ForeignLetters = { 'w', 'q', 'x' };

        public static bool IsTurkish(string reply, out string problem)
        {
            problem = string.Empty;
            if (string.IsNullOrWhiteSpace(reply)) return true;

            foreach (char c in reply)
            {
                // Çince, Kiril, Arapça ama ayrıca š, é, ß gibi Türkçede olmayan harfler.
                if (char.IsLetter(c) && c > '\u007F' && TurkishLetters.IndexOf(c) < 0)
                {
                    problem = $"Türkçe olmayan harf: '{c}'";
                    return false;
                }
            }

            foreach (string word in Words(reply))
            {
                if (EnglishWords.Contains(word))
                {
                    problem = $"İngilizce kelime: '{word}'";
                    return false;
                }

                // Türk alfabesinde w, q, x yok (working, wirklich, welcome...).
                if (word.IndexOfAny(ForeignLetters) >= 0)
                {
                    problem = $"yabancı kelime: '{word}'";
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<string> Words(string text)
        {
            var word = new StringBuilder();
            foreach (char c in text + " ")
            {
                if (char.IsLetter(c) || c == '\'')
                {
                    word.Append(char.ToLowerInvariant(c));
                }
                else if (word.Length > 0)
                {
                    yield return word.ToString().Trim('\'');
                    word.Clear();
                }
            }
        }
    }
}
