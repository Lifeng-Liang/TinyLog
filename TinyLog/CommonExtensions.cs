using System;
using System.Collections.Generic;

namespace Fp
{
    public static class CommonExtensions
    {
        public static IEnumerable<int> To(this int start, int end, int step = 1)
        {
            for (int i = start; i < end; i += step)
            {
                yield return i;
            }
        }

        public static IEnumerable<int> Skip(this IEnumerable<int> list, int n)
        {
            foreach (var i in list)
            {
                if (n > 0)
                {
                    n--;
                    continue;
                }
                yield return i;
            }
        }

        public static IEnumerable<TR> MapObj<T, TR>(this IEnumerable<object> list, Func<T, TR> cb)
        {
            foreach (T item in list)
            {
                yield return cb(item);
            }
        }

        public static IEnumerable<TR> Map<T, TR>(this IEnumerable<T> list, Func<T, TR> cb)
        {
            foreach (T item in list)
            {
                yield return cb(item);
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<T> list, Func<T, TR?> cb) where TR : struct
        {
            foreach (var item in list)
            {
                var n = cb(item);
                if (n.HasValue)
                {
                    yield return n.Value;
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<T> list, Func<T, TR> cb) where TR : class
        {
            foreach (var item in list)
            {
                var n = cb(item);
                if (n != null)
                {
                    yield return n;
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<IEnumerable<T>> list, Func<T, TR?> cb) where TR : struct
        {
            foreach (var arr in list)
            {
                foreach (var item in arr)
                {
                    var n = cb(item);
                    if (n.HasValue)
                    {
                        yield return n.Value;
                    }
                }
            }
        }

        public static IEnumerable<TR> FlatMap<T, TR>(this IEnumerable<IEnumerable<T>> list, Func<T, TR> cb) where TR : class
        {
            foreach (var arr in list)
            {
                foreach (var item in arr)
                {
                    var n = cb(item);
                    if (n != null)
                    {
                        yield return n;
                    }
                }
            }
        }

        public static TR Reduce<T, TR>(this IEnumerable<T> list, Func<TR, T, TR> cb, TR baseValue)
        {
            TR prev = baseValue;
            foreach (T next in list)
            {
                prev = cb(prev, next);
            }
            return prev;
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (cb(item))
                {
                    yield return item;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> list, Action<T> cb)
        {
            foreach (T item in list)
            {
                cb(item);
            }
        }

        public static bool Every<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (!cb(item))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Some<T>(this IEnumerable<T> list, Func<T, bool> cb)
        {
            foreach (T item in list)
            {
                if (cb(item))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<T> ToList<T>(this IEnumerable<T> list)
        {
            return new List<T>(list);
        }
    }
}
