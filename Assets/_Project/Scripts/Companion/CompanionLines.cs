using System;

namespace YazLab.Companion
{
    /// <summary>
    /// Robi'nin söylediği cümleler. Aynı durumda hep aynı lafı etmesin diye her seferinde
    /// listeden rastgele biri seçilir (testlerin sabit tohumunu bozmamak için ayrı Random kullanılır).
    /// </summary>
    public static class CompanionLines
    {
        private static readonly Random Rng = new();

        public static readonly string[] TaskStarted =
        {
            "Tamamdır, bana bırak!",
            "Oke, hemen gidiyorum.",
            "Ben sığarım oraya, sen bekle.",
            "Tamam tamam, bir saniyede hallederim."
        };

        public static readonly string[] TaskCompleted =
        {
            "Hallettim! Hadi devam.",
            "Oldu bu iş!",
            "İşte bu, tamamdır.",
            "Yaptım, gelebilirsin artık."
        };

        public static readonly string[] NotNeeded =
        {
            "Onu zaten yaptık ya.",
            "Buna gerek yok bence."
        };

        public static readonly string[] NoTask =
        {
            "Burada benlik bir iş yok gibi.",
            "Hmm, şu an yapacak bir şey göremiyorum."
        };

        public static readonly string[] Following =
        {
            "Tamam, arkandayım.",
            "Geliyorum, bekle beni!",
            "Peşindeyim."
        };

        public static readonly string[] Staying =
        {
            "Peki, burada bekliyorum.",
            "Ben buradayım, işin bitince seslen.",
            "Tamam, kıpırdamıyorum."
        };

        public static string Pick(string[] lines) => lines[Rng.Next(lines.Length)];
    }
}
