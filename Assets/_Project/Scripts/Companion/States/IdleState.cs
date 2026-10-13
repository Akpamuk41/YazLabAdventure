namespace YazLab.Companion.States
{
    /// <summary>Olduğu yerde bekler; yalnızca yeni bir emirle çıkılır.</summary>
    public sealed class IdleState : CompanionStateBase
    {
        public IdleState(CompanionContext context, CompanionStateMachine machine) : base(context, machine) { }

        public override CompanionStateId Id => CompanionStateId.Idle;

        public override void Enter() => Context.Movement.Stop();

        public override void Tick(float deltaTime)
        {
            Context.LastReading = Context.Sensor.Sense(null);
        }
    }
}
