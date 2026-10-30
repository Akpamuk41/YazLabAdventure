using System;
using UnityEngine;

namespace YazLab.Dialogue
{
    [Serializable]
    public class OllamaSettings
    {
        [Tooltip("Ollama sunucusu. Model başka bir bilgisayarda çalışıyorsa onun IP'si yazılır.")]
        public string baseUrl = "http://localhost:11434";

        [Tooltip("Ollama'da yüklü model adı (ollama list).")]
        public string model = "llama3.2:3b";

        [Tooltip("Bu süreden uzun süren istek zaman aşımı sayılır. İlk istekte model yüklendiği için biraz uzun tutuldu.")]
        public int timeoutSeconds = 45;

        [Range(0f, 1.5f)] public float temperature = 0.4f;
    }
}
