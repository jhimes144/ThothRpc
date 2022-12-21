using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Models.Dto;

namespace ThothRpc.Utility
{
    internal static class Pools
    {
        public static ObjectPool<MethodCallDto> MethodCallDtoPool { get; } = new ObjectPool<MethodCallDto>
            (() => new MethodCallDto(),
            m =>
            {
                m.ArgumentsData.Clear();
                m.CallId = null;
                m.ClassTarget = null;
                m.Method = null;
            });

        public static ObjectPool<MethodResponseDto> MethodResponseDtoPool { get; } = new ObjectPool<MethodResponseDto>
            (() => new MethodResponseDto(),
            m =>
            {
                m.Exception = null;
                m.ResultData = null;
            });

        public static void Recycle(IThothDto dto)
        {
            if (dto is MethodCallDto methodCall)
            {
                MethodCallDtoPool.Recycle(methodCall);
            }
            else if (dto is MethodResponseDto methodResponseDto)
            {
                MethodResponseDtoPool.Recycle(methodResponseDto);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    /// <summary>
    /// This is a forgiving object pool. It lives a simple but full life. If objects are not returned, then they are eligible for
    /// garbage collection. Conversely, objects "recycled" are not checked if they came from the pool
    /// and the pool happily accepts the object as if it was of its own sons.
    /// If the maxPoolSize is reached, then Rent just returns fresh instances indiscriminately.
    /// This class is thread-safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ObjectPool<T> where T : notnull
    {
        readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();

        volatile int _maxPoolSize;
        volatile int _currentPoolSize; // why dont we use bag.count? its expensive to call that property. Take a look at the source.

        public Func<T> Factory { get; }

        /// <summary>
        /// Action to do when an object is recycled. This is ran even if the pool is full.
        /// </summary>
        public Action<T>? Restock { get; }

        public ObjectPool(Func<T> factory, Action<T>? restock = null, int maxPoolSize = 3000)
        {
            _maxPoolSize = maxPoolSize;
            Factory = factory;
            Restock = restock;
        }

        public T Rent()
        {
            var obtained = _bag.TryTake(out var obj);

            if (obtained)
            {
                Interlocked.Decrement(ref _currentPoolSize);
                return obj!;
            }

            return Factory();
        }

        public void Recycle(T obj)
        {
            Restock?.Invoke(obj);

            if (_maxPoolSize > _currentPoolSize)
            {
                _bag.Add(obj);
                Interlocked.Increment(ref _currentPoolSize);
            }
        }
    }
}
