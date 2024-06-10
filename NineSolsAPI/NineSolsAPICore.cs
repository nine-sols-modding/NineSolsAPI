using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using NineSolsAPI.Menu;
using UnityEngine;

namespace NineSolsAPI;

[PublicAPI]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class NineSolsAPICore : BaseUnityPlugin {
    public const string PluginGUID = "ninesolsapi";
    public const string PluginName = "NineSolsAPI";
    public const string PluginVersion = "1.0.0";

    public static Canvas FullscreenCanvas => Instance.fullscreenCanvas;

    internal static NineSolsAPICore Instance = null!;
    private Canvas fullscreenCanvas = null!;
    internal ToastManager ToastManager = null!;
    internal KeybindManager KeybindManager = new();
    private TitlescreenModifications titlescreenModifications = new();
    private Harmony harmony = null!;

    private void Awake() {
        Instance = this;
        Log.Init(Logger);
        harmony = Harmony.CreateAndPatchAll(typeof(Patches), PluginGUID);
        fullscreenCanvas = CreateFullscreenCanvas();
        ToastManager = new ToastManager();
        titlescreenModifications.Load();


        RCGLifeCycle.DontDestroyForever(gameObject);
        Logger.LogInfo("Nine Sols API loaded");
    }

    private void OnDestroy() {
        KeybindManager.Unload();
        titlescreenModifications.Unload();
        Destroy(FullscreenCanvas.gameObject);
        harmony.UnpatchSelf();

        Logger.LogInfo("Nine Sols API unloaded");
    }

    private void Update() {
        ToastManager.Update();
        KeybindManager.Update();
    }

    private Canvas CreateFullscreenCanvas() {
        var fullscreenCanvasObject = new GameObject("NineSolsAPI-FullscreenCanvas");
        var theFullscreenCanvas = fullscreenCanvasObject.AddComponent<Canvas>();
        theFullscreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        RCGLifeCycle.DontDestroyForever(fullscreenCanvasObject);
        return theFullscreenCanvas;
    }
}