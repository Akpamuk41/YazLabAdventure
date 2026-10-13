using YazLab.Core;

namespace YazLab.Companion.Commands
{
    /// <summary>Komutların yoldaş üzerinde çağırabildiği davranışlar.</summary>
    public interface ICompanionCommandReceiver
    {
        void BeginInteractionTask(IInteractable target, string description);

        void BeginFollow();

        void BeginStay();
    }
}
