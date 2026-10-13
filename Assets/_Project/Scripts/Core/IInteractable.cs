using UnityEngine;

namespace YazLab.Core
{
    /// <summary>Oyuncu veya yoldaş tarafından kullanılabilen dünya nesnesi.</summary>
    public interface IInteractable
    {
        string DisplayName { get; }

        /// <summary>Etkileşimi yapan karakterin durması gereken nokta.</summary>
        Transform InteractionPoint { get; }

        bool CanInteract(GameObject actor);

        void Interact(GameObject actor);
    }
}
