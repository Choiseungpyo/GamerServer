using System;
using System.Collections.Generic;
using System.Linq;

public static class EventDispatcher
{
    private static Dictionary<Type, List<object>> listeners = new();

    public static void RegisterListener<T>(IEventListener<T> listener) where T : IEvent
    {
        var type = typeof(T);
        if (!listeners.ContainsKey(type))
            listeners[type] = new List<object>();

        if (!listeners[type].Contains(listener))
            listeners[type].Add(listener);
    }

    public static void UnregisterListener<T>(IEventListener<T> listener) where T : IEvent
    {
        var type = typeof(T);
        if (listeners.ContainsKey(type))
            listeners[type].Remove(listener);
    }

    public static void Dispatch<T>(T gameEvent) where T : IEvent
    {
        var type = typeof(T);
        if (listeners.TryGetValue(type, out var list))
        {
            foreach (var listener in list.Cast<IEventListener<T>>())
            {
                listener.OnEvent(gameEvent);
            }
        }
    }
}