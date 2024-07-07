using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using NineSolsAPI.Menu;
using NineSolsAPI.Preload;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NineSolsAPI;

[PublicAPI]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class NineSolsAPICore : BaseUnityPlugin {
    // ReSharper disable once InconsistentNaming
    public const string PluginGUID = "ninesolsapi";
    public const string PluginName = "NineSolsAPI";
    public const string PluginVersion = "1.0.0";

    public static Canvas FullscreenCanvas => Instance.fullscreenCanvas;

    internal static NineSolsAPICore Instance = null!;
    private Canvas fullscreenCanvas = null!;
    private Preloader preloader = null!;
    internal ToastManager ToastManager = null!;
    internal KeybindManager KeybindManager = new();
    private TitlescreenModifications titlescreenModifications = new();
    private Harmony harmony = null!;

    private RectTransform? progressBar;

    private float LoadProgress {
        set {
            SetProgress(value);
            if (value >= 1) OnLoadDone();
        }
    }

    public static Preloader Preloader => Instance.preloader;

    private void Awake() {
        Instance = this;
        Log.Init(Logger);
        LoadProgress = 0;
        harmony = Harmony.CreateAndPatchAll(typeof(Patches), PluginGUID);
        fullscreenCanvas = CreateFullscreenCanvas();
        preloader = new Preloader(progress => LoadProgress = progress);
        ToastManager = new ToastManager();
        titlescreenModifications.Load();
        SceneManager.sceneLoaded += OnSceneLoaded;

        RCGLifeCycle.DontDestroyForever(gameObject);
        Logger.LogInfo("Nine Sols API loaded");
    }


    private void Start() {
        Invoke(nameof(AfterStart), 0);
        StartCoroutine(preloader.Preload());
    }

    private void AfterStart() {
        if (SceneManager.GetActiveScene().name == "Logo") progressBar = CreateProgressBar();
    }

    private void OnLoadDone() {
        if (progressBar == null) return;

        Destroy(progressBar.parent.gameObject);
        progressBar = null;

        SceneManager.LoadScene("TitleScreenMenu");
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        KeybindManager.Unload();
        titlescreenModifications.Unload();
        Destroy(FullscreenCanvas.gameObject);
        preloader.Unload();
        harmony.UnpatchSelf();

        Logger.LogInfo("Nine Sols API unloaded");
    }

    private void Update() {
        ToastManager.Update();
        KeybindManager.Update();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
        titlescreenModifications.MaybeExtendMainMenu(scene);
    }

    private Canvas CreateFullscreenCanvas() {
        var fullscreenCanvasObject = new GameObject("NineSolsAPI-FullscreenCanvas");
        var theFullscreenCanvas = fullscreenCanvasObject.AddComponent<Canvas>();
        theFullscreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        RCGLifeCycle.DontDestroyForever(fullscreenCanvasObject);
        return theFullscreenCanvas;
    }

    private const int ProgressWidth = 600;
    private const int ProgressHeight = 30;

    private RectTransform CreateProgressBar() {
        var progressContainer = new GameObject();
        // progressContainer.SetActive(false);
        progressContainer.transform.SetParent(FullscreenCanvas.transform);
        var progressContainerImg = progressContainer.AddComponent<Image>();
        progressContainerImg.sprite = NullSprite([0xff, 0xff, 0xff, 0xff]);
        progressContainerImg.useSpriteMesh = true;
        var progressContainerTransform = progressContainer.GetComponent<RectTransform>();
        progressContainerTransform.pivot = new Vector2(0.5f, 0.5f);
        progressContainerTransform.sizeDelta = new Vector2(ProgressWidth, ProgressHeight);
        progressContainerTransform.anchoredPosition = Vector2.zero;
        progressContainerTransform.anchorMin = new Vector2(0.5f, 0.2f);
        progressContainerTransform.anchorMax = new Vector2(0.5f, 0.2f);

        var progress = new GameObject();
        progress.transform.SetParent(progressContainer.transform);
        var progressImg = progress.AddComponent<Image>();
        progressImg.sprite = NullSprite([0x34, 0x34, 0xeb, 0xff]);
        progressImg.useSpriteMesh = true;
        var progressTransform = progress.GetComponent<RectTransform>();
        progressTransform.pivot = new Vector2(0.0f, 0.5f);
        progressTransform.sizeDelta = new Vector2(0, ProgressHeight);
        progressTransform.anchoredPosition = Vector2.zero;
        progressTransform.anchorMin = new Vector2(0.0f, 0.5f);
        progressTransform.anchorMax = new Vector2(0.0f, 0.5f);

        return progressTransform;

        static Sprite NullSprite(byte[]? data = null) {
            var tex = new Texture2D(1, 1);
            data ??= [0x00, 0x00, 0x00, 0x00];
            tex.LoadRawTextureData(data);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
        }
    }

    private void SetProgress(float progress) {
        if (progressBar == null) return;

        if (!progressBar.gameObject.activeSelf)
            if (progress != 0 && progress < 1.0)
                progressBar.parent.gameObject.SetActive(true);

        progressBar.sizeDelta = progressBar.sizeDelta with { x = ProgressWidth * progress };
    }
}