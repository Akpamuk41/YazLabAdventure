namespace YazLab.Companion.Commands
{
    /// <summary>Oyuncunun yoldaşa verdiği emir (Command deseni).</summary>
    public interface ICompanionCommand
    {
        string Description { get; }

        void Execute(ICompanionCommandReceiver receiver);
    }
}
