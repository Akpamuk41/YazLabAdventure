namespace YazLab.Dialogue
{
    /// <summary>Sohbet geçmişindeki tek bir mesaj (system / user / assistant).</summary>
    public readonly struct ChatMessage
    {
        public const string SystemRole = "system";
        public const string UserRole = "user";
        public const string AssistantRole = "assistant";

        public readonly string Role;
        public readonly string Content;

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public static ChatMessage System(string content) => new(SystemRole, content);
        public static ChatMessage User(string content) => new(UserRole, content);
        public static ChatMessage Assistant(string content) => new(AssistantRole, content);
    }
}
