using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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


// https://github.com/hk-modding/api/blob/master/Assembly-CSharp/Utils/UnityExtensions.cs#L37
    /// <summary>
    /// Find a game object by name in the scene. The object's name must be given in the hierarchy.
    /// </summary>
    /// <param name="scene">The scene to search.</param>
    /// <param name="objName">The name of the object in the hierarchy, with '/' separating parent GameObjects from child GameObjects.</param>
    /// <returns>The GameObject if found; null if not.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the path to the game object is invalid.</exception>
    public static GameObject FindGameObject(Scene scene, string objName) {
        var rootObjects = scene.GetRootGameObjects();
        return GetGameObjectFromArray(rootObjects, objName);
    }

    internal static GameObject GetGameObjectFromArray(GameObject[] objects, string objName) {
        // Split object name into root and child names based on '/'
        string rootName;
        string childName = null;

        var slashIndex = objName.IndexOf('/');
        if (slashIndex == -1)
            rootName = objName;
        else if (slashIndex == 0 || slashIndex == objName.Length - 1)
            throw new ArgumentException("Invalid GameObject path");
        else {
            rootName = objName[..slashIndex];
            childName = objName[(slashIndex + 1)..];
        }

        // Get root object
        var obj = objects.FirstOrDefault(o => o.name == rootName);
        if (obj == null) {
            ToastManager.Toast($"root does not exist: {rootName}");
            return null;
        }

        // Get child object
        if (childName == null) return obj;


        var t = obj.transform.Find(childName);
        if (t == null) {
            ToastManager.Toast($"does not exist: {childName} in {rootName}");
            return null;
        } else
            return t.gameObject;
    }
}