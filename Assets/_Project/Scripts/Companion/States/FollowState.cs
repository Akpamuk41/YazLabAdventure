using UnityEngine;

namespace YazLab.Companion.States
{
    /// <summary>
    /// Oyuncuyu histerezisli mesafe kontrolüyle takip eder: belli bir mesafeden uzaklaşınca yürür,
    /// yaklaşınca durur, çok uzaktaysa koşar. Oyuncuya yol yoksa (ör. köprü kalkıkken) en yakın noktada bekler.
    /// </summary>
    public sealed class FollowState : CompanionStateBase
    {
        private float _repathTimer;
        private bool _moving;

        public FollowState(CompanionContext context, CompanionStateMachine machine) : base(context, machine) { }

        public override CompanionStateId Id => CompanionStateId.Follow;

        public override void Enter()
        {
            _repathTimer = 0f;
            _moving = false;
        }

        public override void Tick(float deltaTime)
        {
            var target = Context.FollowTarget;
            if (target == null) return;

            Context.LastReading = Context.Sensor.Sense(target);
            var settings = Context.Settings;
            float distance = Vector3.Distance(Context.Self.position, target.position);

            if (_moving && distance <= settings.followStopDistance)
            {
                _moving = false;
                Context.Movement.Stop();
                return;
            }

            if (!_moving && distance < settings.followStartDistance) return;

            _moving = true;
            _repathTimer -= deltaTime;
            if (_repathTimer > 0f) return;

            _repathTimer = settings.followRepathInterval;
            Context.Movement.SetSpeed(distance > settings.followRunDistance ? settings.runSpeed : settings.walkSpeed);
            Context.Movement.MoveTo(target.position);
        }
    }
}
