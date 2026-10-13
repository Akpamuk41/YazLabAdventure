using UnityEngine;

namespace YazLab.Companion.States
{
    /// <summary>
    /// Sensör önde hareketli bir engel gördüğünde durur ve bekler. Engel kalkarsa yola devam eder;
    /// süre dolarsa yeniden rota hesaplatır (NavMesh oyulduğu için alternatif yol seçilir).
    /// Çok sayıda başarısız denemede görevi iptal eder.
    /// </summary>
    public sealed class BlockedState : CompanionStateBase
    {
        private const float MinimumWait = 0.3f;

        private float _timer;

        public BlockedState(CompanionContext context, CompanionStateMachine machine) : base(context, machine) { }

        public override CompanionStateId Id => CompanionStateId.Blocked;

        public override void Enter()
        {
            _timer = 0f;
            Context.Movement.Stop();
        }

        public override void Tick(float deltaTime)
        {
            if (!Context.HasTask)
            {
                ReturnToDefault();
                return;
            }

            if (TaskTimedOut)
            {
                FailTask("Görev zaman aşımına uğradı.");
                return;
            }

            _timer += deltaTime;
            var target = Context.TaskTarget.InteractionPoint;
            Context.LastReading = Context.Sensor.Sense(target);

            // Beklerken hedef yönüne bak ki sensör konisi doğru yeri tarasın.
            Context.Movement.RotateTowards(target.position, Context.Settings.turnSpeed * 0.5f);

            if (_timer >= MinimumWait && !Context.LastReading.DynamicObstacleAhead)
            {
                Machine.ChangeState(CompanionStateId.MoveToTarget);
                return;
            }

            if (_timer < Context.Settings.blockedWaitTimeout) return;

            Context.ReplanAttempts++;
            if (Context.ReplanAttempts > Context.Settings.maxReplanAttempts)
            {
                FailTask("Yol sürekli kapalı, hedefe ulaşamadım.");
                return;
            }

            Context.SensorGraceUntil = Time.time + Context.Settings.sensorGraceAfterReplan;
            Machine.ChangeState(CompanionStateId.MoveToTarget);
        }
    }
}
