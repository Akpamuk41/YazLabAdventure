using UnityEngine;
using YazLab.Characters;
using YazLab.Companion.Movement;
using YazLab.Companion.Sensors;
using YazLab.Core;

namespace YazLab.Companion
{
    /// <summary>Durumların paylaştığı veri ve servisler. Durumlar MonoBehaviour'a değil bu bağlama bağımlıdır.</summary>
    public sealed class CompanionContext
    {
        public CompanionContext(GameObject actor, IMovementStrategy movement, ISensor sensor,
            ICharacterAnimator animator, Transform followTarget, CompanionSettings settings)
        {
            Actor = actor;
            Movement = movement;
            Sensor = sensor;
            Animator = animator;
            FollowTarget = followTarget;
            Settings = settings;
        }

        public GameObject Actor { get; }
        public Transform Self => Actor.transform;
        public IMovementStrategy Movement { get; }
        public ISensor Sensor { get; }
        public ICharacterAnimator Animator { get; }
        public Transform FollowTarget { get; }
        public CompanionSettings Settings { get; }

        public SensorReading LastReading { get; set; }

        // Aktif görev bilgisi
        public IInteractable TaskTarget { get; private set; }
        public string TaskDescription { get; private set; }
        public float TaskStartTime { get; private set; }
        public int ReplanAttempts { get; set; }
        public float SensorGraceUntil { get; set; }

        public bool HasTask => TaskTarget != null;
        public float TaskElapsed => Time.time - TaskStartTime;

        /// <summary>Görev bittiğinde yoldaşın döneceği mod: takip mi, bekleme mi.</summary>
        public bool ReturnToFollow { get; set; } = true;

        public void StartTask(IInteractable target, string description)
        {
            TaskTarget = target;
            TaskDescription = description;
            TaskStartTime = Time.time;
            ReplanAttempts = 0;
            SensorGraceUntil = 0f;
        }

        public void ClearTask()
        {
            TaskTarget = null;
            TaskDescription = null;
        }
    }
}
