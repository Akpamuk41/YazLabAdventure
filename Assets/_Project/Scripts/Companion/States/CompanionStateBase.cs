using UnityEngine;
using YazLab.Core;

namespace YazLab.Companion.States
{
    /// <summary>Durumların ortak yardımcıları: bağlam erişimi, geçiş ve görev sonlandırma.</summary>
    public abstract class CompanionStateBase : ICompanionState
    {
        protected readonly CompanionContext Context;
        protected readonly CompanionStateMachine Machine;

        protected CompanionStateBase(CompanionContext context, CompanionStateMachine machine)
        {
            Context = context;
            Machine = machine;
        }

        public abstract CompanionStateId Id { get; }

        public virtual void Enter() { }

        public abstract void Tick(float deltaTime);

        public virtual void Exit() { }

        protected void CompleteTask()
        {
            var description = Context.TaskDescription;
            var duration = Context.TaskElapsed;
            Context.ClearTask();

            EventBus.Publish(new CompanionCommandCompletedEvent(description, duration));
            EventBus.Publish(new HudMessageEvent($"Robi görevi tamamladı: {description} ({duration:0.0} sn)", new Color(0.6f, 1f, 0.6f)));
            ReturnToDefault();
        }

        protected void FailTask(string reason)
        {
            Context.ClearTask();
            Context.Movement.Stop();

            EventBus.Publish(new CompanionCommandFailedEvent(reason));
            EventBus.Publish(new HudMessageEvent($"Robi: {reason}", new Color(1f, 0.55f, 0.45f)));
            ReturnToDefault();
        }

        protected void ReturnToDefault()
        {
            Machine.ChangeState(Context.ReturnToFollow ? CompanionStateId.Follow : CompanionStateId.Idle);
        }

        protected bool TaskTimedOut => Context.HasTask && Context.TaskElapsed > Context.Settings.maxTaskDuration;
    }
}
