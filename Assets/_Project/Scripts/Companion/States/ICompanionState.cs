namespace YazLab.Companion.States
{
    public interface ICompanionState
    {
        CompanionStateId Id { get; }

        void Enter();

        void Tick(float deltaTime);

        void Exit();
    }
}
