using UnityEngine;
using YazLab.Characters;
using YazLab.Companion.Commands;
using YazLab.Companion.Movement;
using YazLab.Companion.Sensors;
using YazLab.Companion.States;
using YazLab.Core;

namespace YazLab.Companion
{
    /// <summary>
    /// Otonom yoldaşın kompozisyon kökü: bağımlılıkları (hareket, sensör, animasyon) toplar,
    /// durum makinesini kurar ve oyuncu komutlarını durumlara çevirir. Karar mantığı durum sınıflarındadır.
    /// </summary>
    [RequireComponent(typeof(NavMeshMovement), typeof(RaycastSensor))]
    public class CompanionController : MonoBehaviour, ICommandableCompanion, ICompanionCommandReceiver, IRespawnable
    {
        [SerializeField] private string displayName = "Robi";
        [SerializeField] private Transform followTarget;
        [SerializeField] private CompanionStateId initialState = CompanionStateId.Follow;
        [SerializeField] private CompanionSettings settings = new();

        private CompanionContext _context;
        private CompanionStateMachine _machine;

        public string DisplayName => displayName;
        public GameObject Actor => gameObject;
        public CompanionStateId CurrentState => _machine?.Current?.Id ?? initialState;
        public bool IsFollowing => _context?.ReturnToFollow ?? initialState == CompanionStateId.Follow;
        public SensorReading LastReading => _context?.LastReading ?? default;

        private void Awake()
        {
            _context = new CompanionContext(
                gameObject,
                GetComponent<IMovementStrategy>(),
                GetComponent<ISensor>(),
                GetComponent<ICharacterAnimator>(),
                followTarget,
                settings)
            {
                ReturnToFollow = initialState == CompanionStateId.Follow
            };

            _machine = new CompanionStateMachine();
            _machine.Register(new IdleState(_context, _machine));
            _machine.Register(new FollowState(_context, _machine));
            _machine.Register(new MoveToTargetState(_context, _machine));
            _machine.Register(new BlockedState(_context, _machine));
            _machine.Register(new InteractState(_context, _machine));
        }

        private void Start() => _machine.ChangeState(initialState);

        private void Update() => _machine.Tick(Time.deltaTime);

        public void ExecuteCommand(ICompanionCommand command)
        {
            if (command == null) return;
            command.Execute(this);
        }

        public void BeginInteractionTask(IInteractable target, string description)
        {
            if (target == null || !target.CanInteract(gameObject))
            {
                EventBus.Publish(new HudMessageEvent($"{displayName}: {CompanionLines.Pick(CompanionLines.NotNeeded)}", new Color(1f, 0.8f, 0.4f)));
                return;
            }

            _context.StartTask(target, description);
            EventBus.Publish(new CompanionCommandIssuedEvent(description));
            EventBus.Publish(new HudMessageEvent($"{displayName}: {CompanionLines.Pick(CompanionLines.TaskStarted)}"));
            _machine.ChangeState(CompanionStateId.MoveToTarget);
        }

        public void BeginFollow()
        {
            _context.ClearTask();
            _context.ReturnToFollow = true;
            EventBus.Publish(new HudMessageEvent($"{displayName}: {CompanionLines.Pick(CompanionLines.Following)}"));
            _machine.ChangeState(CompanionStateId.Follow);
        }

        public void BeginStay()
        {
            _context.ClearTask();
            _context.ReturnToFollow = false;
            EventBus.Publish(new HudMessageEvent($"{displayName}: {CompanionLines.Pick(CompanionLines.Staying)}"));
            _machine.ChangeState(CompanionStateId.Idle);
        }

        /// <summary>Testler ve yeniden doğma için: görevi iptal eder, ajanı konuma ışınlar ve bekletir.</summary>
        public void Respawn(Vector3 position, Quaternion rotation)
        {
            _context.ClearTask();
            _context.Movement.Warp(position, rotation);
            _machine.ChangeState(_context.ReturnToFollow ? CompanionStateId.Follow : CompanionStateId.Idle);
        }

        public void SetFollowMode(bool follow) => _context.ReturnToFollow = follow;
    }
}
