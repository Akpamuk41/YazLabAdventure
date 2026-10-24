using YazLab.Dialogue;

namespace YazLab.Npc
{
    /// <summary>Oyuncunun bir mesajına karşılık bekçi tarafında olanlar.</summary>
    public readonly struct GuardTurn
    {
        /// <summary>Bekçinin cevabı. Hata durumunda boştur, <see cref="Notice"/> doludur.</summary>
        public readonly string Reply;
        public readonly GuardMood Mood;
        public readonly int TrustChange;
        public readonly int Trust;
        public readonly bool DoorOpened;
        public readonly bool GaveUp;

        /// <summary>Oyuncuya gösterilecek sistem uyarısı (Ollama kapalı, bozuk cevap vb.).</summary>
        public readonly string Notice;
        public readonly LlmErrorKind Error;

        public GuardTurn(string reply, GuardMood mood, int trustChange, int trust, bool doorOpened, bool gaveUp)
        {
            Reply = reply;
            Mood = mood;
            TrustChange = trustChange;
            Trust = trust;
            DoorOpened = doorOpened;
            GaveUp = gaveUp;
            Notice = string.Empty;
            Error = LlmErrorKind.None;
        }

        private GuardTurn(int trust, LlmErrorKind error, string notice)
        {
            Reply = string.Empty;
            Mood = GuardMood.Neutral;
            TrustChange = 0;
            Trust = trust;
            DoorOpened = false;
            GaveUp = false;
            Notice = notice;
            Error = error;
        }

        public bool HasReply => !string.IsNullOrEmpty(Reply);

        public static GuardTurn Failed(int trust, LlmErrorKind error, string notice) => new(trust, error, notice);
    }
}
