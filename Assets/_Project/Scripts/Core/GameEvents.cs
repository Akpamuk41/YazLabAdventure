using UnityEngine;

namespace YazLab.Core
{
    /// <summary>Olay yolunda taşınabilen tüm olayların işaret arayüzü.</summary>
    public interface IGameEvent { }

    public readonly struct LeverActivatedEvent : IGameEvent
    {
        public readonly string LeverId;
        public LeverActivatedEvent(string leverId) => LeverId = leverId;
    }

    public readonly struct BridgeStateChangedEvent : IGameEvent
    {
        public readonly bool IsExtended;
        public BridgeStateChangedEvent(bool isExtended) => IsExtended = isExtended;
    }

    /// <summary>Ortam geometrisi değişti, NavMesh yeniden oluşturulmalı.</summary>
    public readonly struct NavMeshDirtyEvent : IGameEvent { }

    public readonly struct CompanionStateChangedEvent : IGameEvent
    {
        public readonly string PreviousState;
        public readonly string NewState;

        public CompanionStateChangedEvent(string previousState, string newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }

    public readonly struct CompanionCommandIssuedEvent : IGameEvent
    {
        public readonly string Description;
        public CompanionCommandIssuedEvent(string description) => Description = description;
    }

    public readonly struct CompanionCommandCompletedEvent : IGameEvent
    {
        public readonly string Description;
        public readonly float Duration;

        public CompanionCommandCompletedEvent(string description, float duration)
        {
            Description = description;
            Duration = duration;
        }
    }

    public readonly struct CompanionCommandFailedEvent : IGameEvent
    {
        public readonly string Reason;
        public CompanionCommandFailedEvent(string reason) => Reason = reason;
    }

    /// <summary>Ekranda kısa süreli gösterilecek bilgi mesajı.</summary>
    public readonly struct HudMessageEvent : IGameEvent
    {
        public readonly string Message;
        public readonly Color Color;

        public HudMessageEvent(string message) : this(message, Color.white) { }

        public HudMessageEvent(string message, Color color)
        {
            Message = message;
            Color = color;
        }
    }

    /// <summary>Ekranın altındaki etkileşim ipucu. Boş metin ipucunu gizler.</summary>
    public readonly struct HintChangedEvent : IGameEvent
    {
        public readonly string Hint;
        public HintChangedEvent(string hint) => Hint = hint;
    }

    public readonly struct ObjectiveChangedEvent : IGameEvent
    {
        public readonly string Objective;
        public ObjectiveChangedEvent(string objective) => Objective = objective;
    }

    public readonly struct PlayerRespawnedEvent : IGameEvent { }

    public readonly struct GameCompletedEvent : IGameEvent { }
}
