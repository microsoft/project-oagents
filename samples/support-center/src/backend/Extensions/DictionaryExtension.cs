namespace SupportCenter.Extensions
{
    using System;
    using System.Collections.Generic;

    public static class DictionaryExtensions
    {
        public static T GetValueOrDefault<T>(this IDictionary<string, string> dictionary, string key)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(key);

            if (dictionary.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (InvalidCastException)
                {
                    throw new InvalidOperationException($"Value for key '{key}' is not of type {typeof(T).Name}");
                }
            }
            else
            {
                return default;
            }
        }
    }
}
