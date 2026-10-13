using System;
using UnityEngine;

namespace YazLab.Companion
{
    /// <summary>Yoldaş davranışının ayarlanabilir parametreleri.</summary>
    [Serializable]
    public class CompanionSettings
    {
        [Header("Hız")]
        public float walkSpeed = 2.6f;
        public float runSpeed = 4.8f;

        [Header("Takip")]
        [Tooltip("Bu mesafeden yakınsa durur.")]
        public float followStopDistance = 2.2f;
        [Tooltip("Bu mesafeden uzaklaşınca tekrar takibe başlar.")]
        public float followStartDistance = 3.5f;
        [Tooltip("Bu mesafeden uzaksa koşar.")]
        public float followRunDistance = 7f;
        public float followRepathInterval = 0.25f;

        [Header("Görev")]
        public float arriveTolerance = 0.35f;
        public float interactReach = 1.8f;
        public float facingTolerance = 12f;
        public float turnSpeed = 360f;
        public float interactAnimationDelay = 0.45f;
        public float maxTaskDuration = 60f;

        [Header("Engel")]
        public float blockedWaitTimeout = 3f;
        public float sensorGraceAfterReplan = 1.5f;
        public int maxReplanAttempts = 4;
    }
}
