using UnityEngine;

namespace YazLab.Core
{
    /// <summary>Oyuncu karakterini, onu tanıması gereken sistemlere (tetik alanları vb.) soyut olarak tanıtır.</summary>
    public interface IPlayerActor
    {
        Transform Transform { get; }
    }
}
