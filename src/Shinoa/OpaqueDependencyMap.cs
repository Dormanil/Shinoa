// <copyright file="OpaqueDependencyMap.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Collections.Generic;
    using Discord.Commands;

    public class OpaqueDependencyMap : IDependencyMap
    {
        private readonly Dictionary<Type, Func<object>> map = new Dictionary<Type, Func<object>>();

        public void Add<T>(T obj)
            where T : class
            => AddFactory(() => obj);

        public void AddTransient<T>()
            where T : class, new()
            => AddFactory(() => new T());

        public T Get<T>()
            where T : class => (T)Get(typeof(T));

        public bool TryAdd<T>(T obj)
            where T : class
            => TryAddFactory(() => obj);

        public bool TryAddTransient<T>()
            where T : class, new()
            => TryAddFactory(() => new T());

        public void AddTransient<TKey, TImpl>()
            where TKey : class
            where TImpl : class, TKey, new()
            => AddFactory<TKey>(() => new TImpl());

        public bool TryAddTransient<TKey, TImpl>()
            where TKey : class
            where TImpl : class, TKey, new()
            => TryAddFactory<TKey>(() => new TImpl());

        public void AddFactory<T>(Func<T> factory)
            where T : class
        {
            var t = typeof(T);
            if (map.ContainsKey(t)) throw new InvalidOperationException($"The dependency map already contains \"{t.FullName}\"");
            map.Add(t, factory);
        }

        public object Get(Type t)
        {
            if (!TryGet(t, out object res))
                throw new KeyNotFoundException($"The dependency map does not contain \"{t.FullName}\"");
            return res;
        }

        public bool TryAddFactory<T>(Func<T> factory)
            where T : class
        {
            var t = typeof(T);
            if (map.ContainsKey(t)) return false;
            map.Add(t, factory);
            return true;
        }

        public bool TryGet<T>(out T result)
            where T : class
        {
            var t = typeof(T);
            if (TryGet(t, out object res))
            {
                result = (T)res;
                return true;
            }

            result = default(T);
            return false;
        }

        public bool TryGet(Type t, out object result)
        {
            if (map.TryGetValue(t, out Func<object> factory))
            {
                result = factory();
                return true;
            }

            result = null;
            return false;
        }

        public void AddOpaque(object obj)
        {
            if (obj == null) return;
            var t = obj.GetType();
            if (map.ContainsKey(t)) throw new InvalidOperationException($"The dependency map already contains \"{t.FullName}\"");
            map.Add(t, () => obj);
        }

        public bool TryAddOpaque(object obj)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            if (map.ContainsKey(t)) return false;
            map.Add(t, () => obj);
            return true;
        }

        public bool TryRemove(Type t)
        {
            if (t == null) return false;
            return map.Remove(t);
        }
    }
}
