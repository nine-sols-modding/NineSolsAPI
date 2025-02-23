using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using NineSolsAPI.Preload;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

namespace NineSolsAPI.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Patches {
    [HarmonyPatch(typeof(AchievementData), "OnAcquired")]
    [HarmonyPrefix]
    private static bool AchievementAcquired(ref AchievementData __instance) {
        // disable achievements on modded installs
        Log.Info($"Prevented Achievement '{__instance.name}' from activating.");
        return false;
    }

    /*[HarmonyPatch(typeof(VersionText), "Start")]
    [HarmonyPostfix]
    private static void Version(ref VersionText __instance) {
        var prefix = $"Modding API: {NineSolsAPICore.PluginVersion}";

        if (__instance.text != null)
            __instance.text.text = $"{prefix}\n{__instance.text.text}";
        else
            __instance.TMPtext.text = $"{prefix}\n{__instance.TMPtext.text}";
    }*/


    [HarmonyPatch(typeof(LogoLogic), nameof(LogoLogic.Start))]
    [HarmonyPostfix]
    private static void Start(ref LogoLogic __instance) {
        if (Application.buildGUID == GameVersions.SpeedrunPatch) {
            return;
        }

        RuntimeInitHandler.LoadCore();
        SceneManager.LoadScene(__instance.NextScene);
    }


    [HarmonyPatch(typeof(GameLevel), nameof(GameLevel.Awake))]
    [HarmonyPrefix]
    private static bool GameLevelAwake() => !Preloader.IsPreloading;
}