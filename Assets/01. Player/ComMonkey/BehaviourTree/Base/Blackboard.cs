using System;
using System.Collections.Generic;

namespace Kyu_BT
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public void Set<T>(string key, T value) => _data[key] = value;

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var v))
            {
                if (v is T t) return t;
                throw new InvalidCastException($"Blackboard key '{key}' is not of type {typeof(T)}.");
            }
            return defaultValue;
        }

        public bool Has(string key) => _data.ContainsKey(key);
        public bool Remove(string key) => _data.Remove(key);
        public void Clear() => _data.Clear();
    }
}
