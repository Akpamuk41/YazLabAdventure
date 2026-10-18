using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YazLab.CameraSystem;
using YazLab.Core;
using YazLab.Player;

namespace YazLab.UI
{
    /// <summary>Başlangıç menüsü, duraklatma (Esc) ve oyun sonu ekranları arasındaki akışı yönetir.</summary>
    public class GameFlowController : MonoBehaviour
    {
        private enum FlowState { StartMenu, Playing, Paused, Ended }

        // Otomatik testlerde menü beklenmeden oyun başlar.
        private static readonly string[] AutoRunArguments = { "-runCompanionTests", "-runQuestTests" };

        [Tooltip("IPlayerInput uygulayan bileşen.")]
        [SerializeField] private MonoBehaviour inputBehaviour;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endSummaryText;
        [SerializeField] private float endScreenDelay = 1.5f;

        [Header("Butonlar")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button startQuitButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button endRestartButton;
        [SerializeField] private Button endMenuButton;

        // "Yeniden başla" sahneyi yüklediğinde menüyü atlamak için.
        private static bool _skipStartMenu;

        private IPlayerInput _input;
        private FlowState _state;
        private float _playTime;
        private float _pausedTimeScale = 1f;
        private bool _inputWasEnabled = true;
        private bool _dialogOpen;
        private int _dialogClosedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => _skipStartMenu = false;

        private void Awake()
        {
            _input = inputBehaviour as IPlayerInput;

            Listen(startButton, StartGame);
            Listen(startQuitButton, QuitGame);
            Listen(resumeButton, Resume);
            Listen(pauseRestartButton, Restart);
            Listen(pauseMenuButton, BackToMenu);
            Listen(endRestartButton, Restart);
            Listen(endMenuButton, BackToMenu);

            // Kamera Start'ta imleci kilitlemeden önce durum kurulmuş olmalı.
            bool skipMenu = _skipStartMenu || Environment.GetCommandLineArgs().Any(a => AutoRunArguments.Contains(a));
            _skipStartMenu = false;
            if (skipMenu) StartGame();
            else EnterStartMenu();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameCompletedEvent>(OnGameCompleted);
            EventBus.Subscribe<DialogStateChangedEvent>(OnDialogStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameCompletedEvent>(OnGameCompleted);
            EventBus.Unsubscribe<DialogStateChangedEvent>(OnDialogStateChanged);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !DialogOwnsEscape())
            {
                if (_state == FlowState.Playing) Pause();
                else if (_state == FlowState.Paused) Resume();
            }

            if (_state == FlowState.Playing) _playTime += Time.unscaledDeltaTime;
            // Editörde Esc imleci kendi de bırakıp sonra tekrar kilitleyebiliyor; menüdeyken serbest tut.
            else if (Cursor.lockState != CursorLockMode.None) ThirdPersonCamera.LockCursor(false);
        }

        private void EnterStartMenu()
        {
            SetState(FlowState.StartMenu);
            Time.timeScale = 0f;
            SetInput(false);
        }

        private void StartGame()
        {
            SetState(FlowState.Playing);
            Time.timeScale = 1f;
            SetInput(true);
        }

        private void Pause()
        {
            _inputWasEnabled = _input?.Enabled ?? true;
            _pausedTimeScale = Time.timeScale;
            SetState(FlowState.Paused);
            Time.timeScale = 0f;
            SetInput(false);
        }

        private void Resume()
        {
            SetState(FlowState.Playing);
            Time.timeScale = _pausedTimeScale;
            // Diyalog açıkken duraklatıldıysa girdi kapalı kalmalı.
            SetInput(_inputWasEnabled);
        }

        private void OnDialogStateChanged(DialogStateChangedEvent e)
        {
            _dialogOpen = e.IsOpen;
            if (!e.IsOpen) _dialogClosedFrame = Time.frameCount;
        }

        // Diyalog Esc ile kapandığı karede menü de açılmasın (Update sırası belirsiz).
        private bool DialogOwnsEscape() => _dialogOpen || _dialogClosedFrame == Time.frameCount;

        private void OnGameCompleted(GameCompletedEvent e)
        {
            if (_state == FlowState.Ended) return;

            _state = FlowState.Ended;
            StartCoroutine(ShowEndScreen());
        }

        private IEnumerator ShowEndScreen()
        {
            yield return new WaitForSecondsRealtime(endScreenDelay);

            SetState(FlowState.Ended);
            Time.timeScale = 0f;
            SetInput(false);

            if (endSummaryText != null)
            {
                var time = TimeSpan.FromSeconds(_playTime);
                endSummaryText.text = $"Süre: {(int)time.TotalMinutes:00}:{time.Seconds:00}";
            }
        }

        private void Restart()
        {
            _skipStartMenu = true;
            ReloadScene();
        }

        private void BackToMenu() => ReloadScene();

        private static void ReloadScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetState(FlowState state)
        {
            _state = state;
            if (startPanel != null) startPanel.SetActive(state == FlowState.StartMenu);
            if (pausePanel != null) pausePanel.SetActive(state == FlowState.Paused);
            if (endPanel != null) endPanel.SetActive(state == FlowState.Ended);
        }

        private void SetInput(bool enabled)
        {
            if (_input != null) _input.Enabled = enabled;
            ThirdPersonCamera.LockCursor(enabled);
        }

        private static void Listen(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
