using YazLab.Core;

namespace YazLab.Companion.Commands
{
    /// <summary>Yoldaşın bir hedefe gidip onunla etkileşime girmesini ister (ör. şalteri indir).</summary>
    public sealed class InteractCommand : ICompanionCommand
    {
        private readonly IInteractable _target;

        public InteractCommand(IInteractable target, string description)
        {
            _target = target;
            Description = description;
        }

        public string Description { get; }

        public void Execute(ICompanionCommandReceiver receiver) => receiver.BeginInteractionTask(_target, Description);
    }

    public sealed class FollowCommand : ICompanionCommand
    {
        public string Description => "Beni takip et";

        public void Execute(ICompanionCommandReceiver receiver) => receiver.BeginFollow();
    }

    public sealed class StayCommand : ICompanionCommand
    {
        public string Description => "Burada bekle";

        public void Execute(ICompanionCommandReceiver receiver) => receiver.BeginStay();
    }
}
