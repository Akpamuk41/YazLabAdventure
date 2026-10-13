using UnityEngine;
using YazLab.Companion.Movement;

namespace YazLab.Companion.States
{
    /// <summary>
    /// Görev hedefine NavMesh üzerinden yol bulur ve ilerler. Her karede sensör okunur:
    /// önünde hareketli engel varsa Blocked durumuna, hedefe vardığında Interact durumuna geçer.
    /// </summary>
    public sealed class MoveToTargetState : CompanionStateBase
    {
        private const float PartialPathTolerance = 2f;

        private float _partialTimer;

        public MoveToTargetState(CompanionContext context, CompanionStateMachine machine) : base(context, machine) { }

        public override CompanionStateId Id => CompanionStateId.MoveToTarget;

        public override void Enter()
        {
            _partialTimer = 0f;

            if (!Context.HasTask)
            {
                ReturnToDefault();
                return;
            }

            Context.Movement.SetSpeed(Context.Settings.runSpeed);
            if (!Context.Movement.MoveTo(Context.TaskTarget.InteractionPoint.position))
            {
                FailTask("Hedefe giden bir yol bulamadım.");
            }
        }

        public override void Tick(float deltaTime)
        {
            if (!Context.HasTask) return;

            if (TaskTimedOut)
            {
                FailTask("Görev zaman aşımına uğradı.");
                return;
            }

            var target = Context.TaskTarget;
            Context.LastReading = Context.Sensor.Sense(target.InteractionPoint);

            var status = Context.Movement.PathStatus;
            if (status == MovementPathStatus.Pending) return;

            if (status == MovementPathStatus.Invalid)
            {
                FailTask("Hedefe giden bir yol yok.");
                return;
            }

            if (Time.time >= Context.SensorGraceUntil && Context.LastReading.DynamicObstacleAhead)
            {
                Machine.ChangeState(CompanionStateId.Blocked);
                return;
            }

            if (Context.Movement.HasArrived(Context.Settings.arriveTolerance))
            {
                if (status == MovementPathStatus.Partial || IsFarFromInteractionPoint())
                {
                    // Yolun sonuna geldik ama hedefe ulaşamadık: geçici bir engel olabilir, bekle ve yeniden dene.
                    Machine.ChangeState(CompanionStateId.Blocked);
                    return;
                }

                Machine.ChangeState(CompanionStateId.Interact);
                return;
            }

            // Kısmi yol uzun süre devam ederse hedef gerçekten erişilemez demektir.
            _partialTimer = status == MovementPathStatus.Partial ? _partialTimer + deltaTime : 0f;
            if (_partialTimer > Context.Settings.maxTaskDuration * 0.5f)
            {
                FailTask("Hedefe ulaşamıyorum, yol kapalı.");
            }
        }

        private bool IsFarFromInteractionPoint()
        {
            var offset = Context.TaskTarget.InteractionPoint.position - Context.Self.position;
            offset.y = 0f;
            return offset.magnitude > PartialPathTolerance;
        }
    }
}
