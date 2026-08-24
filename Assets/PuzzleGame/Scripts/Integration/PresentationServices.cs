using System;
using System.Collections.Generic;

namespace PuzzleGame.Presentation
{
    /// <summary>
    /// Minimal service registry for the presentation layer.
    ///
    /// Core systems (Codex) bind their implementations at startup via
    /// <see cref="Register{T}"/>. When nothing is registered, demo mocks are
    /// installed lazily by the demo bootstrap so screens remain reviewable.
    /// Presentation code resolves seams with <see cref="Get{T}"/> only.
    /// </summary>
    public static class PresentationServices
    {
        static readonly Dictionary<Type, object> Registry = new Dictionary<Type, object>();

        public static void Register<T>(T implementation) where T : class
        {
            Registry[typeof(T)] = implementation;
        }

        public static bool Has<T>() where T : class
        {
            return Registry.ContainsKey(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var impl))
            {
                return (T)impl;
            }
            throw new InvalidOperationException(
                "No implementation registered for " + typeof(T).Name +
                ". Core systems must register real sources, or the demo bootstrap must install mocks.");
        }

        public static T GetOrNull<T>() where T : class
        {
            return Registry.TryGetValue(typeof(T), out var impl) ? (T)impl : null;
        }

        public static void Clear()
        {
            Registry.Clear();
        }
    }
}
