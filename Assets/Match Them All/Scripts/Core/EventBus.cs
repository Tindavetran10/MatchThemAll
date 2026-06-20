using System;
using System.Collections.Generic;

namespace MatchThemAll.Scripts
{
    /// <summary>
    /// A lightweight, strongly-typed Event Bus for decoupled communication between systems.
    /// Replaces scattered static actions and singletons with a unified publishing/subscribe model.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Subscribes a listener to an event of type T.</summary>
        public static void Subscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (!_subscribers.ContainsKey(eventType)) 
                _subscribers[eventType] = new List<Delegate>();

            if (!_subscribers[eventType].Contains(listener)) 
                _subscribers[eventType].Add(listener);
        }

        /// <summary>Unsubscribes a listener from an event of type T.</summary>
        public static void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var subscriber)) 
                subscriber.Remove(listener);
        }

        /// <summary>Publishes an event of type T to all registered listeners.</summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (_subscribers.TryGetValue(eventType, out var delegates))
            {
                // Iterate backwards to allow listeners to unsubscribe during an event without breaking the loop
                for (int i = delegates.Count - 1; i >= 0; i--)
                {
                    if (delegates[i] is Action<T> action) 
                        action.Invoke(eventData);
                }
            }
        }

        /// <summary>Clears all event subscriptions. Call this when shutting down or completely resetting the application.</summary>
        public static void ClearAll() => _subscribers.Clear();
    }
}
