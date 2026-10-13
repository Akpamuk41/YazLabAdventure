using UnityEngine;

namespace YazLab.Companion.Sensors
{
    /// <summary>Ajanın çevresini algılama soyutlaması. Hedef verilmezse yalnızca engeller taranır.</summary>
    public interface ISensor
    {
        SensorReading Sense(Transform target);
    }
}
