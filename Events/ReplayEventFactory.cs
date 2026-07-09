using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ReplayMod.Events
{
    public static class ReplayEventFactory
    {
        public static T GetEvent<T>() where T : class, IReplayEvent, new()
            => ReplayEventPool<T>.Get();

        public static void Return(IReplayEvent ev)
        {
            if (ev == null) return;
            var type = ev.GetType();
            if (_returners.TryGetValue(type, out var returner))
                returner(ev);
            else
                throw new ArgumentException($"No return delegate registered for type {type}");
        }

        private static Dictionary<EventType, Func<IReplayEvent>> _creators = new();
        private static Dictionary<Type, Action<IReplayEvent>> _returners = new();

        //auto registration by attributes
        public static void RegisterTypes()
        {
            var assembly = typeof(IReplayEvent).Assembly;

            var eventTypes = assembly.GetTypes()
                .Where(type => typeof(IReplayEvent).IsAssignableFrom(type)
                && !type.IsAbstract
                && !type.IsInterface);

            foreach (var type in eventTypes)
            {
                var attribute = type.GetCustomAttribute<ReplayEventAttribute>();
                if (attribute == null)
                {
                    Plugin.logger.LogWarning($"Event type [{type.Name}] missing attribute, skipping registration");
                    continue;
                }

                //generate generic method
                var method = typeof(ReplayEventFactory)
                .GetMethod(nameof(RegisterEvent), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(type);

                method.Invoke(null, new object[] { attribute.Type });

                Plugin.logger.LogInfo($"Event type {type} registered");
            }
        }

        private static void RegisterEvent<T>(EventType type) where T : class, IReplayEvent, new()
        {
            _creators[type] = () => ReplayEventPool<T>.Get();
            _returners[typeof(T)] = ev => ReplayEventPool<T>.Return((T)ev);
        }

        public static IReplayEvent Create(EventType type)
        {
            if (_creators.TryGetValue(type, out var creator))
                return creator();
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown event type");
        }

        public static IReplayEvent CreateAndRead(EventType type, BinaryReader reader)
        {
            var ev = Create(type);
            ev.Read(reader);
            return ev;
        }   

        public static void ReturnToPool(this IReplayEvent ev)
        {
            Return(ev);
        }
    }
}