namespace YazLab.Companion.Sensors
{
    /// <summary>
    /// Sensörün tek bir okumada ürettiği gözlem. Açılar derece cinsinden ve ajanın ileri yönüne göredir
    /// (negatif = sol, pozitif = sağ). ML-Agents'a geçilirse bu değerler doğrudan gözlem vektörü olur.
    /// </summary>
    public readonly struct SensorReading
    {
        public readonly float[] RayAngles;
        public readonly float[] RayDistances;

        public readonly bool ObstacleDetected;
        public readonly float NearestObstacleDistance;
        public readonly float NearestObstacleAngle;

        /// <summary>İleri koni içinde, durma mesafesinden yakın hareketli bir engel var mı?</summary>
        public readonly bool DynamicObstacleAhead;

        public readonly bool HasTarget;
        public readonly bool TargetVisible;
        public readonly float TargetDistance;
        public readonly float TargetAngle;

        public SensorReading(float[] rayAngles, float[] rayDistances,
            bool obstacleDetected, float nearestObstacleDistance, float nearestObstacleAngle,
            bool dynamicObstacleAhead,
            bool hasTarget, bool targetVisible, float targetDistance, float targetAngle)
        {
            RayAngles = rayAngles;
            RayDistances = rayDistances;
            ObstacleDetected = obstacleDetected;
            NearestObstacleDistance = nearestObstacleDistance;
            NearestObstacleAngle = nearestObstacleAngle;
            DynamicObstacleAhead = dynamicObstacleAhead;
            HasTarget = hasTarget;
            TargetVisible = targetVisible;
            TargetDistance = targetDistance;
            TargetAngle = targetAngle;
        }
    }
}
