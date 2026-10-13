using System;
using System.Collections.Generic;
using YazLab.Core;

namespace YazLab.Companion.States
{
    /// <summary>Sonlu durum makinesi. Her geçiş olay yolunda yayınlanır (HUD ve test kaydı için).</summary>
    public sealed class CompanionStateMachine
    {
        private readonly Dictionary<CompanionStateId, ICompanionState> _states = new();

        public ICompanionState Current { get; private set; }

        public event Action<CompanionStateId, CompanionStateId> StateChanged;

        public void Register(ICompanionState state) => _states[state.Id] = state;

        public void ChangeState(CompanionStateId id)
        {
            if (!_states.TryGetValue(id, out var next))
            {
                throw new ArgumentException($"Kayıtlı olmayan durum: {id}");
            }

            var previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();

            // Enter içinde başka bir duruma geçildiyse (ör. görev hemen başarısız oldu) o geçiş zaten yayınlandı.
            if (Current != next) return;

            var previousId = previous?.Id ?? id;
            StateChanged?.Invoke(previousId, id);
            EventBus.Publish(new CompanionStateChangedEvent(previousId.ToDisplayName(), id.ToDisplayName()));
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
