using System;
using System.Collections.Generic;
using UnityEngine;

namespace YazLab.Core
{
    /// <summary>
    /// Bileşenlerin birbirini doğrudan tanımadan haberleşmesini sağlayan tip güvenli olay yolu.
    /// Companion, NPC ve oyun akışı yalnızca olay tipleri üzerinden konuşur (gevşek bağlılık).
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            Handlers[type] = Handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) Handlers.Remove(type);
            else Handlers[type] = remaining;
        }

        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out var existing)) return;

            // Bir dinleyicideki hata diğerlerini ve oyun akışını durdurmamalı.
            foreach (var handler in existing.GetInvocationList())
            {
                try
                {
                    ((Action<T>)handler).Invoke(gameEvent);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>Domain reload kapalıyken Play modları arasında eski abonelikleri temizler.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Handlers.Clear();
    }
}
