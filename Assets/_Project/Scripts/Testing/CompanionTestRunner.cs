using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using YazLab.Companion;
using YazLab.Companion.Commands;
using YazLab.Core;

namespace YazLab.Testing
{
    /// <summary>
    /// Otonom yoldaşı farklı başlangıç noktalarından tekrar tekrar görevlendirip başarı oranını ve
    /// ortalama tamamlama süresini ölçer. Sonuçlar rapor için CSV olarak kaydedilir.
    /// Oyunda F9 ile başlatılır; build'e "-runCompanionTests" argümanı verilirse otomatik çalışıp kapanır.
    /// </summary>
    public class CompanionTestRunner : MonoBehaviour
    {
        private const string AutoRunArgument = "-runCompanionTests";

        [SerializeField] private MonoBehaviour companionBehaviour;
        [SerializeField] private CompanionTaskMarker task;
        [Tooltip("Her denemeden önce sıfırlanacak IResettable bileşenler (şalter, köprü, piston).")]
        [SerializeField] private List<MonoBehaviour> resettables = new();
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(1)] private int trialsPerSpawn = 10;
        [SerializeField] private float trialTimeout = 60f;
        [SerializeField] private float timeScale = 3f;
        [SerializeField] private int randomSeed = 12345;

        private readonly List<TrialResult> _results = new();
        private ICommandableCompanion _companion;
        private IRespawnable _respawnable;
        private bool _running;

        // Aktif deneme ölçümleri
        private bool _trialFinished;
        private bool _trialSucceeded;
        private string _failureReason;
        private int _blockedCount;
        private float _distanceTravelled;

        private struct TrialResult
        {
            public int SpawnIndex;
            public int Trial;
            public bool Success;
            public float Duration;
            public float Distance;
            public int BlockedCount;
            public string Note;
        }

        private void Awake()
        {
            _companion = companionBehaviour as ICommandableCompanion;
            _respawnable = companionBehaviour as IRespawnable;
        }

        private void Start()
        {
            if (Environment.GetCommandLineArgs().Contains(AutoRunArgument)) StartCoroutine(RunAll(quitWhenDone: true));
        }

        private void Update()
        {
            if (!_running && Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                StartCoroutine(RunAll(quitWhenDone: false));
            }
        }

        private IEnumerator RunAll(bool quitWhenDone)
        {
            if (_companion == null || _respawnable == null || task == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[CompanionTest] Test çalıştırıcı eksik yapılandırılmış.");
                yield break;
            }

            _running = true;
            _results.Clear();
            UnityEngine.Random.InitState(randomSeed);
            float previousTimeScale = Time.timeScale;
            Time.timeScale = timeScale;

            EventBus.Subscribe<CompanionCommandCompletedEvent>(OnCompleted);
            EventBus.Subscribe<CompanionCommandFailedEvent>(OnFailed);
            EventBus.Subscribe<CompanionStateChangedEvent>(OnStateChanged);

            int total = spawnPoints.Length * trialsPerSpawn;
            for (int s = 0; s < spawnPoints.Length; s++)
            {
                for (int t = 0; t < trialsPerSpawn; t++)
                {
                    EventBus.Publish(new HudMessageEvent($"Test {_results.Count + 1}/{total} (başlangıç {s + 1})"));
                    yield return RunTrial(s, t);
                }
            }

            EventBus.Unsubscribe<CompanionCommandCompletedEvent>(OnCompleted);
            EventBus.Unsubscribe<CompanionCommandFailedEvent>(OnFailed);
            EventBus.Unsubscribe<CompanionStateChangedEvent>(OnStateChanged);

            ResetWorld();
            _companion.ExecuteCommand(new FollowCommand());
            Time.timeScale = previousTimeScale;
            _running = false;

            string path = WriteReport();
            EventBus.Publish(new HudMessageEvent($"Testler bitti: %{SuccessRate() * 100f:0} başarı. Rapor: {path}", Color.cyan));

            if (quitWhenDone) Application.Quit(SuccessRate() > 0f ? 0 : 1);
        }

        private IEnumerator RunTrial(int spawnIndex, int trial)
        {
            ResetWorld();
            _companion.ExecuteCommand(new StayCommand());
            var spawn = spawnPoints[spawnIndex];
            _respawnable.Respawn(spawn.position, spawn.rotation);

            // Köprü sıfırlanınca NavMesh'in güncellenmesi için kısa bir süre bekle.
            yield return new WaitForSeconds(0.5f);

            _trialFinished = false;
            _trialSucceeded = false;
            _failureReason = string.Empty;
            _blockedCount = 0;
            _distanceTravelled = 0f;

            float start = Time.time;
            var lastPosition = _companion.Actor.transform.position;
            _companion.ExecuteCommand(new InteractCommand(task.Interactable, task.CommandLabel));

            while (!_trialFinished && Time.time - start < trialTimeout)
            {
                yield return null;
                var position = _companion.Actor.transform.position;
                _distanceTravelled += Vector3.Distance(position, lastPosition);
                lastPosition = position;
            }

            if (!_trialFinished) _failureReason = "Zaman aşımı";

            _results.Add(new TrialResult
            {
                SpawnIndex = spawnIndex,
                Trial = trial,
                Success = _trialSucceeded,
                Duration = Time.time - start,
                Distance = _distanceTravelled,
                BlockedCount = _blockedCount,
                Note = _trialSucceeded ? string.Empty : _failureReason
            });

            Debug.Log($"[CompanionTest] spawn={spawnIndex} trial={trial} success={_trialSucceeded} " +
                      $"time={Time.time - start:0.00}s dist={_distanceTravelled:0.0}m blocked={_blockedCount} {_failureReason}");
        }

        private void ResetWorld()
        {
            foreach (var behaviour in resettables)
            {
                if (behaviour is IResettable resettable) resettable.ResetState();
            }
        }

        private void OnCompleted(CompanionCommandCompletedEvent e)
        {
            _trialFinished = true;
            _trialSucceeded = true;
        }

        private void OnFailed(CompanionCommandFailedEvent e)
        {
            _trialFinished = true;
            _trialSucceeded = false;
            _failureReason = e.Reason;
        }

        private void OnStateChanged(CompanionStateChangedEvent e)
        {
            if (e.NewState == CompanionStateId.Blocked.ToDisplayName()) _blockedCount++;
        }

        private float SuccessRate() => _results.Count == 0 ? 0f : _results.Count(r => r.Success) / (float)_results.Count;

        private string WriteReport()
        {
            var culture = CultureInfo.InvariantCulture;
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));
            Directory.CreateDirectory(directory);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", culture);

            var csv = new StringBuilder("spawn,trial,success,duration_s,distance_m,blocked_count,note\n");
            foreach (var r in _results)
            {
                csv.AppendLine(string.Join(",",
                    r.SpawnIndex + 1, r.Trial + 1, r.Success ? 1 : 0,
                    r.Duration.ToString("0.00", culture), r.Distance.ToString("0.00", culture),
                    r.BlockedCount, $"\"{r.Note}\""));
            }

            var summary = new StringBuilder();
            summary.AppendLine("| Başlangıç | Deneme | Başarı oranı | Ort. süre (sn) | Ort. mesafe (m) | Ort. engel bekleme |");
            summary.AppendLine("|---|---|---|---|---|---|");
            foreach (var group in _results.GroupBy(r => r.SpawnIndex))
            {
                summary.AppendLine(SummaryRow($"S{group.Key + 1}", group.ToList(), culture));
            }
            summary.AppendLine(SummaryRow("**Toplam**", _results, culture));

            string csvPath = Path.Combine(directory, $"companion_tests_{stamp}.csv");
            File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, $"companion_summary_{stamp}.md"), summary.ToString(), Encoding.UTF8);
            Debug.Log($"[CompanionTest] Özet:\n{summary}");
            return csvPath;
        }

        private static string SummaryRow(string label, List<TrialResult> results, CultureInfo culture)
        {
            var successes = results.Where(r => r.Success).ToList();
            float rate = results.Count == 0 ? 0f : successes.Count / (float)results.Count;
            string avgTime = successes.Count == 0 ? "-" : successes.Average(r => r.Duration).ToString("0.00", culture);
            string avgDistance = successes.Count == 0 ? "-" : successes.Average(r => r.Distance).ToString("0.0", culture);
            string avgBlocked = results.Count == 0 ? "-" : results.Average(r => r.BlockedCount).ToString("0.0", culture);
            return $"| {label} | {results.Count} | %{(rate * 100f).ToString("0", culture)} | {avgTime} | {avgDistance} | {avgBlocked} |";
        }
    }
}
