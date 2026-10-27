using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using YazLab.CameraSystem;
using YazLab.Core;
using YazLab.Dialogue;
using YazLab.Npc;
using YazLab.Player;

namespace YazLab.UI
{
    /// <summary>
    /// Bekçiyle serbest metin sohbet penceresi. Mesaj gönderilince cevap beklenirken "Düşünüyor..."
    /// gösterilir, oyun akmaya devam eder. Pencere açıkken oyuncu girdisi kapalıdır, Esc pencereyi kapatır.
    /// </summary>
    public class GuardDialogueView : MonoBehaviour
    {
        [SerializeField] private GuardNpc guard;

        [Tooltip("IPlayerInput uygulayan bileşen.")]
        [SerializeField] private MonoBehaviour inputBehaviour;

        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text logText;
        [SerializeField] private Text thinkingText;
        [SerializeField] private Text trustText;
        [SerializeField] private InputField inputField;
        [SerializeField] private int maxLogLines = 8;
        [SerializeField] private float closeDelayAfterEnd = 2.5f;

        private readonly List<string> _log = new();
        private IPlayerInput _input;
        private bool _isOpen;
        private float _closeTimer;

        private void Awake()
        {
            _input = inputBehaviour as IPlayerInput;
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (guard != null) guard.DialogueRequested += Open;
        }

        private void OnDisable()
        {
            if (guard != null) guard.DialogueRequested -= Open;
        }

        private void Update()
        {
            if (!_isOpen) return;

            if (_closeTimer > 0f)
            {
                _closeTimer -= Time.unscaledDeltaTime;
                if (_closeTimer <= 0f) Close();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (guard.IsThinking)
            {
                int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
                thinkingText.text = $"{guard.Conversation.Persona.guardName} düşünüyor" + new string('.', dots);
            }
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Submit();
            }
        }

        private void Open()
        {
            if (_isOpen) return;

            _isOpen = true;
            _closeTimer = 0f;
            panel.SetActive(true);
            titleText.text = guard.Conversation.Persona.guardName;
            if (_log.Count == 0) AddLine("<i><color=#AAAAAA>Bekçiye ne söylemek istediğini yaz ve Enter'a bas. (Esc: kapat)</color></i>");
            RefreshTrust();
            SetWaiting(false);

            if (_input != null) _input.Enabled = false;
            ThirdPersonCamera.LockCursor(false);
            EventBus.Publish(new DialogStateChangedEvent(true));
        }

        private void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            guard.CancelPending();
            panel.SetActive(false);

            if (_input != null) _input.Enabled = true;
            ThirdPersonCamera.LockCursor(true);
            EventBus.Publish(new DialogStateChangedEvent(false));
        }

        private async void Submit()
        {
            string text = inputField.text.Trim();
            if (text.Length == 0 || guard.Conversation.IsFinished) return;

            inputField.text = string.Empty;
            AddLine($"<color=#8FD3FF><b>Sen:</b></color> {text}");
            SetWaiting(true);

            var turn = await guard.SayAsync(text);
            if (this == null || !_isOpen) return;

            SetWaiting(false);
            if (turn.HasReply)
            {
                AddLine($"<color={MoodColor(turn.Mood)}><b>{guard.Conversation.Persona.guardName}:</b></color> {turn.Reply}");
            }
            else if (!string.IsNullOrEmpty(turn.Notice))
            {
                AddLine($"<color=#FFCC66><i>{turn.Notice}</i></color>");
            }

            RefreshTrust();
            if (turn.DoorOpened || turn.GaveUp)
            {
                AddLine(turn.DoorOpened
                    ? "<color=#77FF77><b>Kapı açıldı!</b></color>"
                    : "<color=#FF7766><b>Bekçi sırtını döndü, artık konuşmuyor.</b></color>");
                inputField.interactable = false;
                _closeTimer = closeDelayAfterEnd;
            }
        }

        private void SetWaiting(bool waiting)
        {
            thinkingText.text = string.Empty;
            inputField.interactable = !waiting && !guard.Conversation.IsFinished;
            if (inputField.interactable)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        private void RefreshTrust()
        {
            var conversation = guard.Conversation;
            trustText.text = $"Güven: {conversation.Trust} / {conversation.Persona.trustToOpen}";
        }

        private void AddLine(string line)
        {
            _log.Add(line);
            if (_log.Count > maxLogLines) _log.RemoveRange(0, _log.Count - maxLogLines);
            logText.text = string.Join("\n", _log);
        }

        private static string MoodColor(GuardMood mood) => mood switch
        {
            GuardMood.Friendly => "#9CFF9C",
            GuardMood.Angry => "#FF8C7A",
            _ => "#FFE9A8"
        };
    }
}
