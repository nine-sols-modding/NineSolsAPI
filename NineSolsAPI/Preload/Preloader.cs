using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NineSolsAPI.Preload;

internal class Preloader(Action<float> onProgress) {
    private const int PreloadBatchSize = 5; // TODO choose a good number

    [PublicAPI] public static bool IsPreloading = false;

    private Dictionary<string, List<(string, object instance, FieldInfo field)>> preloadTypes = [];

    public void AddPreloadClass<T>(T obj) {
        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields) {
            var preloadAttr = field.GetCustomAttribute<PreloadAttribute>();
            if (preloadAttr is null) continue;

            if (!preloadTypes.TryGetValue(preloadAttr.Scene, out var scenePreloads)) {
                scenePreloads = [];
                preloadTypes.Add(preloadAttr.Scene, scenePreloads);
            }

            scenePreloads.Add((preloadAttr.Path, obj, field));
        }
    }

    private bool preloaded = false;
    private List<GameObject> preloadObjs = [];

    private List<AsyncOperation> preloadOperationQueue = [];
    private List<AsyncOperation> inProgressLoads = [];
    private List<AsyncOperation> inProgressUnloads = [];

    private IEnumerator DoPreloadScene(string sceneName, List<(string, object, FieldInfo)> scenePreloads) {
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        preloadOperationQueue.Add(loadOp);
        inProgressLoads.Add(loadOp);
        yield return loadOp;

        var scene = SceneManager.GetSceneByName(sceneName);
        try {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects) rootObj.SetActive(false);

            foreach (var (path, instance, instanceField) in scenePreloads) {
                var obj = ObjectUtils.GetGameObjectFromArray(rootObjects, path);
                if (obj is null) throw new Exception($"could not preload {path} in {sceneName}");

                var copy = Object.Instantiate(obj);
                Object.DontDestroyOnLoad(copy);
                AutoAttributeManager.AutoReference(copy);

                preloadObjs.Add(copy);
                instanceField.SetValue(instance, copy);
            }
        } catch (Exception e) {
            Log.Error(e);
        }

        var unloadOp = SceneManager.UnloadSceneAsync(sceneName);
        inProgressUnloads.Add(unloadOp);
        yield return unloadOp;

        preloadOperationQueue.Remove(loadOp);
    }

    private IEnumerator DoPreload() {
        Log.Info($"Preloading {preloadTypes.Count} scenes");
        if (preloadTypes.Count == 0) yield break;

        var watch = System.Diagnostics.Stopwatch.StartNew();

        IsPreloading = true;
        DestroyAllGameObjects.DestroyingAll = true; // to prevent SingletonBehaviour initialization
        try {
            var target = preloadTypes.Count;
            var preloadsToDo = preloadTypes.GetEnumerator();

            float progress = 0;

            while (progress < 1) {
                while (preloadOperationQueue.Count < PreloadBatchSize && preloadsToDo.MoveNext()) {
                    var (sceneName, scenePreloads) = preloadsToDo.Current;
                    NineSolsAPICore.Instance.StartCoroutine(DoPreloadScene(sceneName, scenePreloads));
                }

                yield return null;

                const float loadUnloadWorkRatio = 0.5f;
                var progressSum = inProgressLoads.Sum(loadOp => loadOp.progress * loadUnloadWorkRatio);
                progressSum += inProgressUnloads.Sum(loadOp => loadOp.progress * (1 - loadUnloadWorkRatio));

                progress = progressSum / target;
                onProgress(progress);

                Log.Info($"progress {progress}/1, in flight {preloadOperationQueue.Count}");
            }
        } finally {
            DestroyAllGameObjects.DestroyingAll = false;
            IsPreloading = false;
            preloaded = true;

            inProgressLoads.Clear();
            inProgressUnloads.Clear();
            preloadOperationQueue.Clear();
        }

        watch.Stop();
        Log.Info($"Preloading done with {preloadObjs.Count} objects in {watch.ElapsedMilliseconds}ms");
    }


    public IEnumerator Preload() {
        // TODO: support preload hot reloading anywhere
        if (!preloaded || SceneManager.GetActiveScene().name == "TitleScreenMenu")
            yield return DoPreload();
    }

    public void Unload() {
        foreach (var obj in preloadObjs)
            if (obj)
                Object.Destroy(obj);
        preloadObjs.Clear();
    }
}