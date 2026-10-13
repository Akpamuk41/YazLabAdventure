using UnityEngine;

namespace YazLab.Player
{
    /// <summary>Oyuncu girdisi soyutlaması; oyun mantığı klavye/gamepad ayrıntısını bilmez.</summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        Vector2 Look { get; }
        bool SprintHeld { get; }
        bool CommandPressed { get; }
        bool FollowTogglePressed { get; }
        bool InteractPressed { get; }

        /// <summary>Diyalog gibi metin girişi sırasında oyun girdisini kapatmak için.</summary>
        bool Enabled { get; set; }
    }
}
