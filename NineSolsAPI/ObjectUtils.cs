using System.Linq;
using UnityEngine;

namespace NineSolsAPI;

internal static class ObjectUtils {
    public static T FindDisabledByName<T>(string name) where T : Object {
        var x = Object.FindObjectsOfType<T>(true)
            .FirstOrDefault(x => x.name == name);
        if (!x) ToastManager.Toast($"FindDisabledByName({name}) not found");
        return x;
    }

    public static GameObject InstantiateAutoReference(GameObject orig, Transform parent,
        bool autoReferenceChildren = true) {
        var copy = Object.Instantiate(orig, parent, false);
        AutoAttributeManager.AutoReference(copy);
        if (autoReferenceChildren) AutoAttributeManager.AutoReferenceAllChildren(copy);
        return copy;
    }
}