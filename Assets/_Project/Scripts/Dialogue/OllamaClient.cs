using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace YazLab.Dialogue
{
    /// <summary>
    /// Ollama'nın /api/chat uç noktasına asenkron istek atar. UnityWebRequest ana iş parçacığını
    /// bloklamaz; sunucu kapalıysa, model yoksa veya süre dolarsa exception yerine hata sonucu döner.
    /// </summary>
    public class OllamaClient : ILlmClient
    {
        private readonly OllamaSettings _settings;

        public OllamaClient(OllamaSettings settings) => _settings = settings ?? new OllamaSettings();

        public async Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, string jsonSchema, CancellationToken cancellationToken)
        {
            float start = Time.realtimeSinceStartup;
            string url = _settings.baseUrl.TrimEnd('/') + "/api/chat";

            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildBody(messages, jsonSchema))),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.Max(1, _settings.timeoutSeconds)
            };
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return LlmResponse.Fail(LlmErrorKind.Cancelled, "İstek iptal edildi.", Elapsed(start));
            }

            float latency = Elapsed(start);
            string body = request.downloadHandler?.text ?? string.Empty;

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                bool timedOut = latency >= _settings.timeoutSeconds - 0.5f
                                || (request.error ?? string.Empty).IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0;
                return timedOut
                    ? LlmResponse.Fail(LlmErrorKind.Timeout, $"{_settings.timeoutSeconds} sn içinde yanıt gelmedi.", latency)
                    : LlmResponse.Fail(LlmErrorKind.Unreachable, $"Ollama'ya bağlanılamadı ({_settings.baseUrl}): {request.error}", latency);
            }

            var dto = TryParse(body);
            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = string.IsNullOrEmpty(dto?.error) ? request.error : dto.error;
                var kind = request.responseCode == 404 || (error ?? string.Empty).Contains("not found")
                    ? LlmErrorKind.ModelNotFound
                    : LlmErrorKind.BadResponse;
                return LlmResponse.Fail(kind, $"HTTP {request.responseCode}: {error}", latency);
            }

            string content = dto?.message?.content;
            if (string.IsNullOrWhiteSpace(content))
            {
                return LlmResponse.Fail(LlmErrorKind.BadResponse, "Modelden boş yanıt geldi.", latency);
            }

            return LlmResponse.Ok(content, latency);
        }

        private static Task SendAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() =>
            {
                request.Abort();
                completion.TrySetCanceled();
            });

            request.SendWebRequest().completed += _ =>
            {
                registration.Dispose();
                completion.TrySetResult(true);
            };

            return completion.Task;
        }

        private string BuildBody(IReadOnlyList<ChatMessage> messages, string jsonSchema)
        {
            var json = new StringBuilder();
            json.Append("{\"model\":").Append(Quote(_settings.model));
            json.Append(",\"stream\":false");
            if (!string.IsNullOrWhiteSpace(jsonSchema)) json.Append(",\"format\":").Append(jsonSchema);
            json.Append(",\"options\":{\"temperature\":")
                .Append(_settings.temperature.ToString("0.##", CultureInfo.InvariantCulture)).Append('}');

            json.Append(",\"messages\":[");
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append("{\"role\":").Append(Quote(messages[i].Role))
                    .Append(",\"content\":").Append(Quote(messages[i].Content)).Append('}');
            }
            json.Append("]}");
            return json.ToString();
        }

        private static string Quote(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static ChatResponseDto TryParse(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                return JsonUtility.FromJson<ChatResponseDto>(body);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static float Elapsed(float start) => Time.realtimeSinceStartup - start;

        [Serializable]
        private class ChatResponseDto
        {
            public MessageDto message;
            public string error;
        }

        [Serializable]
        private class MessageDto
        {
            public string role;
            public string content;
        }
    }
}
