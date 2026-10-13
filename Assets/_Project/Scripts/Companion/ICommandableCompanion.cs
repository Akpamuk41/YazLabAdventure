using UnityEngine;
using YazLab.Companion.Commands;

namespace YazLab.Companion
{
    /// <summary>Oyuncunun emir verebildiği yoldaş. Oyuncu tarafı yalnızca bu arayüzü bilir.</summary>
    public interface ICommandableCompanion
    {
        string DisplayName { get; }
        GameObject Actor { get; }
        CompanionStateId CurrentState { get; }
        bool IsFollowing { get; }

        void ExecuteCommand(ICompanionCommand command);
    }
}
