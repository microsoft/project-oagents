
namespace SupportCenter.ApiService.Extensions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides extension methods for <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key or the default value of <typeparamref name="T"/> if the key does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the value to return.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <returns>The value associated with the specified key, or the default value of <typeparamref name="T"/> if the key does not exist.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> or <paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the value associated with the specified key cannot be converted to <typeparamref name="T"/>.</exception>
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
