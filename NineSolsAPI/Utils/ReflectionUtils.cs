using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace DebugModPlus;

[PublicAPI]
public static class ReflectionUtils {
    public static FieldInfo AccessFieldInfo(this object val, string fieldName) {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        var field = val.GetType().GetField(fieldName, flags);
        if (field == null) {
            var actualNames = val.GetType().GetFields(flags).Select(x => x.Name).Join(delimiter: ",\n");
            throw new Exception(
                $"Field {fieldName} was not found in type {val.GetType()} or base types \n{actualNames}");
        }

        return field;
    }

    public static T AccessField<T>(this object val, string fieldName) {
        return (T)val.AccessFieldInfo(fieldName).GetValue(val);
    }

    public static T? AccessProperty<T>(this object val, string propertyName) =>
        (T?)val.GetType().GetProperty(propertyName)!.GetValue(val);
}