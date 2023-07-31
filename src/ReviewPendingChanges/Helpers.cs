using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ReviewPendingChanges;

public static class Helpers
{
    private static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) => keyValuePairs.ToDictionary(k => k.Key, v => v.Value);

    public static bool TryGetValue<T>(this IEnumerable<T> enumerable, Func<T, bool> evaluator, out T value)
    {
        value = default;
        foreach (var item in enumerable)
        {
            if (evaluator(item))
            {
                value = item;
                return true;
            }
        }

        return false;
    }

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }

    public static IDictionary<string, TEnum> MapEnumMembers<TEnum>() where TEnum : struct, IConvertible
        => Enum.GetValues(typeof(TEnum))
            .Cast<TEnum>()
            .Select(
                value =>
                {
                    var enumAttr =
                        typeof(TEnum)
                            .GetField(value.ToString() ?? string.Empty)
                            ?.GetCustomAttributes(typeof(EnumMemberAttribute), true)
                            .Cast<EnumMemberAttribute>()
                            .FirstOrDefault();
                    return enumAttr?.Value is not null
                        ? new KeyValuePair<string, TEnum>(enumAttr.Value, value)
                        : (KeyValuePair<string, TEnum>?)null;
                }
            )
            .Where(kvp => kvp is not null)
            .Select(kvp => kvp.Value)
            .ToDictionary();
}