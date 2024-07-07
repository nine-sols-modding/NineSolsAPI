using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NineSolsAPI.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NineSolsAPI.Preload;

public interface IPreloadTarget {
    void Set(GameObject preloaded, string scene, string path);

    void Unset(GameObject preloaded);

    class ReflectionPreloadTarget(object instance, FieldInfo field) : IPreloadTarget {
        public void Set(GameObject preloaded, string scene, string path) {
            field.SetValue(instance, preloaded);
        }

        public void Unset(GameObject preloaded) {
            field.SetValue(instance, null);
        }
    }

    class ListPreloadTarget(List<GameObject> preloads) : IPreloadTarget {
        public void Set(GameObject preloaded, string scene, string path) {
            preloads.Add(preloaded);
        }

        public void Unset(GameObject preloaded) {
            preloads.Clear();
        }
    }
}

[PublicAPI]
public class Preloader(Action<float> onProgress) {
    private const int PreloadBatchSize = 4; // TODO choose a good number

    public static bool IsPreloading = false;

    private Dictionary<string, List<(string, IPreloadTarget)>> preloadTypes = [];


    public void AddPreload(string scene, string path, IPreloadTarget target) {
        if (!preloadTypes.TryGetValue(scene, out var scenePreloads)) {
            scenePreloads = [];
            preloadTypes.Add(scene, scenePreloads);
        }

        scenePreloads.Add((path, target));
    }

    public void AddPreloadList(IEnumerable<(string, string)> paths, List<GameObject> outList) {
        var listTarget = new IPreloadTarget.ListPreloadTarget(outList);
        foreach (var (scene, path) in paths) AddPreload(scene, path, listTarget);
    }

    public void AddPreloadClass<T>(T obj) {
        if (IsPreloading) {
            Log.Error("tried to call AddPreloadClass during preloading");
            return;
        }

        var fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields) {
            var preloadAttr = field.GetCustomAttribute<PreloadAttribute>();
            if (preloadAttr is null) continue;

            AddPreload(preloadAttr.Scene, preloadAttr.Path, new IPreloadTarget.ReflectionPreloadTarget(obj, field));
        }
    }

    private bool preloaded = false;
    private List<(GameObject, IPreloadTarget)> preloadObjs = [];

    private List<AsyncOperation> preloadOperationQueue = [];
    private List<AsyncOperation> inProgressLoads = [];
    private List<AsyncOperation> inProgressUnloads = [];

    private int target;

    private IEnumerator DoPreloadScene(string sceneName, List<(string, IPreloadTarget)> scenePreloads) {
        var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (loadOp == null) {
            ToastManager.Toast($"Error loading scene: {sceneName}");
            target -= 1;
            yield break;
        }

        preloadOperationQueue.Add(loadOp);
        inProgressLoads.Add(loadOp);
        yield return loadOp;

        var scene = SceneManager.GetSceneByName(sceneName);
        try {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObj in rootObjects) rootObj.SetActive(false);

            foreach (var (path, preloadTarget) in scenePreloads) {
                var obj = ObjectUtils.GetGameObjectFromArray(rootObjects, path);
                if (obj is null) {
                    Log.Error($"could not preload {path} in {sceneName}");
                    preloadTarget.Set(null, sceneName, path);
                    continue;
                }

                var copy = Object.Instantiate(obj);
                copy.SetActive(false);
                Object.DontDestroyOnLoad(copy);
                AutoAttributeManager.AutoReference(copy);

                preloadObjs.Add((copy, preloadTarget));
                preloadTarget.Set(copy, sceneName, path);
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
            target = preloadTypes.Count;
            var preloadsToDo = preloadTypes.GetEnumerator();

            float progress = 0;

            while (progress < 1) {
                while (preloadOperationQueue.Count < PreloadBatchSize && preloadsToDo.MoveNext()) {
                    var (sceneName, scenePreloads) = preloadsToDo.Current;
                    NineSolsAPICore.Instance.StartCoroutine(DoPreloadScene(sceneName, scenePreloads));

                    if (inProgressLoads.Count % 4 == 0) {
                        var watchRes = System.Diagnostics.Stopwatch.StartNew();
                        yield return Resources.UnloadUnusedAssets();
                        watchRes.Stop();
                        Log.Info($"collecting resources in ${watchRes.ElapsedMilliseconds}");
                    }
                }

                yield return null;

                const float loadUnloadWorkRatio = 0.5f;
                var progressSum = inProgressLoads.Sum(loadOp => loadOp.progress * loadUnloadWorkRatio);
                progressSum += inProgressUnloads.Sum(loadOp => loadOp.progress * (1 - loadUnloadWorkRatio));

                progress = target == 0 ? 1 : progressSum / target;
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


    internal IEnumerator Preload() {
        // TODO: support preload hot reloading anywhere
        if (!preloaded || SceneManager.GetActiveScene().name == "TitleScreenMenu")
            yield return DoPreload();

        onProgress(1.0f);
    }

    internal void Unload() {
        foreach (var (obj, target) in preloadObjs) {
            if (obj)
                Object.Destroy(obj);
            target.Unset(obj);
        }

        preloadObjs.Clear();
    }
}