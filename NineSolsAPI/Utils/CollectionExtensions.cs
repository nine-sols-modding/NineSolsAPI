using System.Collections.Generic;
using JetBrains.Annotations;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class CollectionExtensions {
    public static T? GetValueOrDefault<T>(this T[] array, int index) where T : class =>
        array.Length > index ? array[index] : null;

    public static void AddRange<T>(this HashSet<T> hashSet, params T[] items) {
        foreach (var item in items) {
            hashSet.Add(item);
        }
    }


    public static void AddToKey<TKey, TValue>(this IDictionary<TKey, ICollection<TValue>> dict, TKey key,
        TValue value) {
        if (dict.TryGetValue(key, out var list)) {
            list.Add(value);
            return;
        }

        dict[key] = [value];
    }
}