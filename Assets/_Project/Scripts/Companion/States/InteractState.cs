using UnityEngine;

namespace YazLab.Companion.States
{
    /// <summary>
    /// Hedefin yanında sensörle mesafe/açı doğrulaması yapar, hedefe döner, etkileşim animasyonunu
    /// oynatır ve ardından hedefle etkileşime girer.
    /// </summary>
    public sealed class InteractState : CompanionStateBase
    {
        private const float MaxAlignTime = 4f;

        private float _timer;
        private float _interactTimer;
        private bool _animationStarted;

        public InteractState(CompanionContext context, CompanionStateMachine machine) : base(context, machine) { }

        public override CompanionStateId Id => CompanionStateId.Interact;

        public override void Enter()
        {
            _timer = 0f;
            _interactTimer = 0f;
            _animationStarted = false;
            Context.Movement.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (!Context.HasTask)
            {
                ReturnToDefault();
                return;
            }

            var target = Context.TaskTarget;
            if (!target.CanInteract(Context.Actor))
            {
                CompleteTask();
                return;
            }

            _timer += deltaTime;
            var targetTransform = ((Component)target).transform;
            Context.LastReading = Context.Sensor.Sense(targetTransform);
            var reading = Context.LastReading;
            var settings = Context.Settings;

            if (_animationStarted)
            {
                _interactTimer += deltaTime;
                if (_interactTimer < settings.interactAnimationDelay) return;

                target.Interact(Context.Actor);
                CompleteTask();
                return;
            }

            if (reading.TargetDistance > settings.interactReach)
            {
                Machine.ChangeState(CompanionStateId.MoveToTarget);
                return;
            }

            Context.Movement.RotateTowards(targetTransform.position, settings.turnSpeed);
            bool aligned = Mathf.Abs(reading.TargetAngle) <= settings.facingTolerance;

            if (aligned && reading.TargetVisible)
            {
                _animationStarted = true;
                Context.Animator?.PlayInteract();
                return;
            }

            if (_timer > MaxAlignTime)
            {
                FailTask("Uzanamıyorum, önü kapalı.");
            }
        }
    }
}
