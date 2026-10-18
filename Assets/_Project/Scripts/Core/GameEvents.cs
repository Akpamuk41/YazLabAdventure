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

    /// <summary>Oyuncu bir görev nesnesini (altın vb.) topladı.</summary>
    public readonly struct CollectiblePickedEvent : IGameEvent
    {
        public readonly string QuestId;
        public CollectiblePickedEvent(string questId) => QuestId = questId;
    }

    /// <summary>Toplama görevinin ilerlemesi değişti.</summary>
    public readonly struct QuestProgressEvent : IGameEvent
    {
        public readonly string QuestId;
        public readonly int Collected;
        public readonly int Required;

        public QuestProgressEvent(string questId, int collected, int required)
        {
            QuestId = questId;
            Collected = collected;
            Required = required;
        }

        public bool IsComplete => Collected >= Required;
    }

    /// <summary>
    /// Diyalog penceresi (ör. NPC bekçi) açıldı/kapandı. Diyalog açıkken Esc duraklatma menüsünü açmaz,
    /// diyalog Esc'yi kendisi kapatmak için kullanabilir.
    /// </summary>
    public readonly struct DialogStateChangedEvent : IGameEvent
    {
        public readonly bool IsOpen;
        public DialogStateChangedEvent(bool isOpen) => IsOpen = isOpen;
    }

    public readonly struct PlayerRespawnedEvent : IGameEvent { }

    public readonly struct GameCompletedEvent : IGameEvent { }
}
