using UnityEngine;

namespace YazLab.Core
{
    public interface IRespawnable
    {
        void Respawn(Vector3 position, Quaternion rotation);
    }
}
