using System.Collections.Concurrent;

namespace ReplayMod.Events
{
    public static class ReplayEventPool<T> where T : class, IReplayEvent, new()
    {
        private static readonly ConcurrentQueue<T> _pool = new();

        public static T Get()
        {
            return _pool.TryDequeue(out var item) ? item : new T();
        }

        public static void Return(T item)
        {
            item.Reset();
            _pool.Enqueue(item);
        }
    }
}
