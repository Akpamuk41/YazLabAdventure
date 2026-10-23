namespace YazLab.Dialogue
{
    public enum GuardMood
    {
        Neutral,
        Friendly,
        Angry
    }

    /// <summary>Bekçinin bir cevabının oyun açısından anlamı: ne söyledi, nasıl hissetti, kapıyı açıyor mu.</summary>
    public readonly struct GuardDecision
    {
        public const int MinTrustChange = -2;
        public const int MaxTrustChange = 2;

        /// <summary>Modelden istenen çıktının şeması (Ollama "format" alanına gönderilir).</summary>
        public const string JsonSchema =
            "{\"type\":\"object\",\"properties\":{" +
            "\"reply\":{\"type\":\"string\"}," +
            "\"mood\":{\"type\":\"string\",\"enum\":[\"neutral\",\"friendly\",\"angry\"]}," +
            "\"trust_change\":{\"type\":\"integer\",\"minimum\":-2,\"maximum\":2}," +
            "\"open_door\":{\"type\":\"boolean\"}}," +
            "\"required\":[\"reply\",\"mood\",\"trust_change\",\"open_door\"]}";

        public readonly string Reply;
        public readonly GuardMood Mood;
        public readonly int TrustChange;
        public readonly bool OpenDoor;

        public GuardDecision(string reply, GuardMood mood, int trustChange, bool openDoor)
        {
            Reply = reply;
            Mood = mood;
            TrustChange = trustChange;
            OpenDoor = openDoor;
        }
    }
}
