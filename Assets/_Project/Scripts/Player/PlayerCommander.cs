using UnityEngine;
using YazLab.Companion;
using YazLab.Companion.Commands;
using YazLab.Core;

namespace YazLab.Player
{
    /// <summary>
    /// Oyuncunun yoldaşa emir vermesini sağlar.
    /// F: en yakın uygun görevi ver (ör. şalteri indir), G: takip et / burada bekle.
    /// </summary>
    public class PlayerCommander : MonoBehaviour
    {
        [Tooltip("ICommandableCompanion uygulayan bileşen.")]
        [SerializeField] private MonoBehaviour companionBehaviour;

        private ICommandableCompanion _companion;
        private IPlayerInput _input;
        private CompanionTaskMarker[] _tasks;

        private void Awake()
        {
            _companion = companionBehaviour as ICommandableCompanion;
            _input = GetComponent<IPlayerInput>();
        }

        private void Start()
        {
            _tasks = FindObjectsByType<CompanionTaskMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void OnValidate()
        {
            if (companionBehaviour != null && companionBehaviour is not ICommandableCompanion)
            {
                Debug.LogWarning($"{companionBehaviour.name} ICommandableCompanion uygulamıyor.", this);
                companionBehaviour = null;
            }
        }

        private void Update()
        {
            if (_companion == null || _input == null) return;

            if (_input.CommandPressed) IssueTaskCommand();
            if (_input.FollowTogglePressed) ToggleFollow();
        }

        private void IssueTaskCommand()
        {
            var task = FindBestTask();
            if (task == null)
            {
                EventBus.Publish(new HudMessageEvent($"{_companion.DisplayName}: Şu an yapabileceğim bir görev yok."));
                return;
            }

            _companion.ExecuteCommand(new InteractCommand(task.Interactable, task.CommandLabel));
        }

        private void ToggleFollow()
        {
            ICompanionCommand command = _companion.IsFollowing ? new StayCommand() : new FollowCommand();
            _companion.ExecuteCommand(command);
        }

        private CompanionTaskMarker FindBestTask()
        {
            CompanionTaskMarker best = null;
            float bestDistance = float.MaxValue;

            foreach (var task in _tasks)
            {
                if (task == null || task.Interactable == null) continue;
                if (!task.Interactable.CanInteract(_companion.Actor)) continue;

                float distance = Vector3.Distance(transform.position, task.transform.position);
                if (distance > task.CommandRange || distance >= bestDistance) continue;

                best = task;
                bestDistance = distance;
            }

            return best;
        }
    }
}
