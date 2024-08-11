using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class ObjectUtils {
    public static GameObject InstantiateAutoReference(GameObject orig, Transform? parent,
        bool autoReferenceChildren = true) {
        var copy = Object.Instantiate(orig, parent, false);
        if (!copy) return copy;
        AutoAttributeManager.AutoReference(copy);
        if (autoReferenceChildren) AutoAttributeManager.AutoReferenceAllChildren(copy);
        return copy;
    }

    public static GameObject InstantiateAutoReference(GameObject orig, bool autoReferenceChildren = true) {
        var copy = Object.Instantiate(orig);
        if (!copy) return copy;
        AutoAttributeManager.AutoReference(copy);
        if (autoReferenceChildren) AutoAttributeManager.AutoReferenceAllChildren(copy);
        return copy;
    }

    public static GameObject InstantiateInit(GameObject orig, Transform? parent = null) {
        var copy = InstantiateAutoReference(orig, parent);

        var levelAwakeList = copy.GetComponentsInChildren<ILevelAwake>(true);
        for (var i = levelAwakeList.Length - 1; i >= 0; i--) {
            var context = levelAwakeList[i];
            try {
                context.EnterLevelAwake();
            } catch (Exception ex) {
                Log.Error(ex.StackTrace);
            }
        }

        var levelAwakeReverseList = copy.GetComponentsInChildren<ILevelAwakeReverse>(true);
        for (var i = levelAwakeReverseList.Length - 1; i >= 0; i--) {
            var context = levelAwakeReverseList[i];
            try {
                context.EnterLevelAwakeReverse();
            } catch (Exception ex) {
                Log.Error(ex.StackTrace);
            }
        }

        var levelStartList = copy.GetComponentsInChildren<ILevelStart>(true);
        for (var i = levelStartList.Length - 1; i >= 0; i--) {
            var context = levelStartList[i];
            try {
                context.EnterLevelStart();
            } catch (Exception ex) {
                Log.Error(ex.StackTrace);
            }
        }

        var resetList = copy.GetComponentsInChildren<IResetter>(true);
        for (var i = resetList.Length - 1; i >= 0; i--) {
            var context = resetList[i];
            try {
                context.EnterLevelReset();
            } catch (Exception ex) {
                Log.Error(ex.StackTrace);
            }
        }

        return copy;
    }


    // https://github.com/hk-modding/api/blob/master/Assembly-CSharp/Utils/UnityExtensions.cs#L37
    /// <summary>
    /// Find a game object by name in the scene. The object's name must be given in the hierarchy.
    /// </summary>
    /// <param name="scene">The scene to search.</param>
    /// <param name="path">The name of the object in the hierarchy, with '/' separating parent GameObjects from child GameObjects.</param>
    /// <returns>The GameObject if found; null if not.</returns>
    /// <exception cref="System.ArgumentException">Thrown if the path to the game object is invalid.</exception>
    public static GameObject? LookupPath(Scene scene, string path) {
        var rootObjects = scene.GetRootGameObjects();
        return GetGameObjectFromArray(rootObjects, path);
    }

    public static GameObject? LookupPath(string path) {
        for (var i = 0; i < SceneManager.sceneCount; i++) {
            var scene = SceneManager.GetSceneAt(i);
            var found = LookupPath(scene, path);
            if (found is not null) return found;
        }

        return null;
    }


    internal static GameObject? GetGameObjectFromArray(GameObject[] objects, string objName) {
        // Split object name into root and child names based on '/'
        string rootName;
        string? childName = null;

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
        if (obj is null) {
            Log.Warning($"root does not exist: {rootName}");
            return null;
        }

        // Get child object
        if (childName == null) return obj;


        var t = obj.transform.Find(childName);
        if (!t) {
            Log.Warning($"does not exist: {childName} in {rootName}");
            return null;
        } else
            return t.gameObject;
    }

    public static T? FindDisabledByName<T>(string name) where T : Object {
        var x = Object.FindObjectsOfType<T>(true)
            .FirstOrDefault(x => x.name == name);
        if (!x) Log.Warning($"FindDisabledByName({name}) not found");
        return x;
    }

    public static string ObjectPath(GameObject obj) {
        List<string> segments = [];
        for (var current = obj; current != null; current = current.transform.parent?.gameObject)
            segments.Add(current.name);

        segments.Reverse();
        return segments.Join(delimiter: "/");
    }
}