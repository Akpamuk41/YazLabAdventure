using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using YazLab.Companion;
using YazLab.Companion.Commands;
using YazLab.Core;
using YazLab.Interactables;
using YazLab.Quests;

namespace YazLab.Testing
{
    /// <summary>
    /// Ara bölüm akışını uçtan uca doğrular: altınlar toplanır → geçit açılır → NavMesh güncellenir →
    /// yoldaş açılan geçitten oyuncuya ulaşır. Build'e "-runQuestTests" verilirse çalışıp kapanır.
    /// </summary>
    public class QuestFlowTestRunner : MonoBehaviour
    {
        private const string AutoRunArgument = "-runQuestTests";

        [SerializeField] private CollectQuest quest;
        [SerializeField] private SlidingGate gate;
        [SerializeField] private Collectible[] coins;
        [SerializeField] private MonoBehaviour playerBehaviour;
        [SerializeField] private MonoBehaviour companionBehaviour;
        [SerializeField] private Transform beforeGatePoint;
        [SerializeField] private Transform courtyardPoint;
        [SerializeField] private float companionTimeout = 20f;

        private readonly List<(string Check, bool Passed, string Detail)> _checks = new();

        private void Start()
        {
            if (Environment.GetCommandLineArgs().Contains(AutoRunArgument)) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var player = playerBehaviour as IRespawnable;
            var companion = companionBehaviour as ICommandableCompanion;
            var companionBody = companionBehaviour as IRespawnable;
            if (quest == null || gate == null || coins == null || coins.Length == 0 || player == null || companion == null || companionBody == null)
            {
                Debug.LogError("[QuestTest] Test çalıştırıcı eksik yapılandırılmış.");
                Application.Quit(1);
                yield break;
            }

            // NavMesh ve sahnenin oturması için kısa bekleme.
            yield return new WaitForSeconds(1f);

            Check("Başlangıçta geçit kapalı", !gate.IsActive, $"IsActive={gate.IsActive}");
            Check("Kapalı geçitten avluya yol yok", PathStatus() != NavMeshPathStatus.PathComplete, PathStatus().ToString());

            for (int i = 0; i < coins.Length; i++)
            {
                player.Respawn(coins[i].transform.position, Quaternion.identity);
                yield return new WaitForSeconds(0.4f);
                Check($"Altın {i + 1} toplandı", coins[i].IsCollected && quest.Collected == i + 1, $"Collected={quest.Collected}");
            }

            // Aynı altın ikinci kez sayılmamalı.
            player.Respawn(coins[0].transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.4f);
            Check("Toplanan altın tekrar sayılmıyor", quest.Collected == coins.Length, $"Collected={quest.Collected}");

            Check("Görev tamamlandı", quest.IsComplete, $"{quest.Collected}/{quest.Required}");
            yield return new WaitForSeconds(3f);
            Check("Geçit açıldı", gate.IsActive, $"IsActive={gate.IsActive}");
            Check("Açık geçitten avluya yol var", PathStatus() == NavMeshPathStatus.PathComplete, PathStatus().ToString());

            // Yoldaş geçidin önünden, avludaki oyuncuyu takip edebilmeli.
            player.Respawn(courtyardPoint.position, courtyardPoint.rotation);
            companionBody.Respawn(beforeGatePoint.position, beforeGatePoint.rotation);
            companion.ExecuteCommand(new FollowCommand());

            float start = Time.time;
            while (Time.time - start < companionTimeout && companion.Actor.transform.position.z < gate.transform.position.z + 1f)
            {
                yield return null;
            }

            float z = companion.Actor.transform.position.z;
            Check("Robi açılan geçitten geçti", z > gate.transform.position.z + 1f, $"z={z:0.0}, süre={Time.time - start:0.0}s");

            bool allPassed = _checks.All(c => c.Passed);
            WriteReport(allPassed);
            Application.Quit(allPassed ? 0 : 1);
        }

        private NavMeshPathStatus PathStatus()
        {
            var path = new NavMeshPath();
            NavMesh.SamplePosition(beforeGatePoint.position, out var from, 2f, NavMesh.AllAreas);
            NavMesh.SamplePosition(courtyardPoint.position, out var to, 2f, NavMesh.AllAreas);
            NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
            return path.status;
        }

        private void Check(string name, bool passed, string detail)
        {
            _checks.Add((name, passed, detail));
            Debug.Log($"[QuestTest] {(passed ? "OK  " : "FAIL")} {name} ({detail})");
        }

        private void WriteReport(bool allPassed)
        {
            var culture = CultureInfo.InvariantCulture;
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults"));
            Directory.CreateDirectory(directory);

            var report = new StringBuilder("| Kontrol | Sonuç | Ayrıntı |\n|---|---|---|\n");
            foreach (var (check, passed, detail) in _checks) report.AppendLine($"| {check} | {(passed ? "Geçti" : "Kaldı")} | {detail} |");
            report.AppendLine($"\n**Genel:** {(allPassed ? "Geçti" : "Kaldı")}");

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", culture);
            File.WriteAllText(Path.Combine(directory, $"quest_flow_{stamp}.md"), report.ToString(), Encoding.UTF8);
            Debug.Log($"[QuestTest] Özet:\n{report}");
        }
    }
}
