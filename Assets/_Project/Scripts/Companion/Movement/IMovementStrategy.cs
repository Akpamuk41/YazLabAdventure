using UnityEngine;

namespace YazLab.Companion.Movement
{
    public enum MovementPathStatus
    {
        None,
        Pending,
        Complete,
        Partial,
        Invalid
    }

    /// <summary>
    /// Ajanın nasıl hareket ettiğini durumlardan soyutlar. NavMesh ile çalışan uygulama varsayılandır;
    /// ileride ML-Agents tabanlı bir uygulama aynı arayüzle takılabilir.
    /// </summary>
    public interface IMovementStrategy
    {
        MovementPathStatus PathStatus { get; }
        float RemainingDistance { get; }
        Vector3 Velocity { get; }

        bool MoveTo(Vector3 destination);
        void Stop();
        void SetSpeed(float speed);
        bool HasArrived(float tolerance);
        void RotateTowards(Vector3 worldPoint, float degreesPerSecond);
        void Warp(Vector3 position, Quaternion rotation);
    }
}
