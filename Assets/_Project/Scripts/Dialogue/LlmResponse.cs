namespace YazLab.Dialogue
{
    public enum LlmErrorKind
    {
        None,
        Unreachable,
        Timeout,
        ModelNotFound,
        BadResponse,
        Cancelled
    }

    /// <summary>
    /// LLM isteğinin sonucu. İstemci hiçbir zaman exception fırlatmaz; hata durumu burada taşınır
    /// ki oyun her koşulda çalışmaya devam etsin.
    /// </summary>
    public readonly struct LlmResponse
    {
        public readonly LlmErrorKind Error;
        public readonly string Content;
        public readonly string Details;
        public readonly float Latency;

        private LlmResponse(LlmErrorKind error, string content, string details, float latency)
        {
            Error = error;
            Content = content;
            Details = details;
            Latency = latency;
        }

        public bool Success => Error == LlmErrorKind.None;

        public static LlmResponse Ok(string content, float latency) => new(LlmErrorKind.None, content, string.Empty, latency);

        public static LlmResponse Fail(LlmErrorKind error, string details, float latency) => new(error, string.Empty, details, latency);
    }
}
