using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using NineSolsAPI.Preload;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

namespace NineSolsAPI.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class CleanLogs {
    [HarmonyPatch(typeof(GameFlagManager), nameof(GameFlagManager.LoadFlags))]
    [HarmonyPrefix]
    private static void LoadFlagsPre(out bool __state) {
        __state = UnityEngine.Debug.unityLogger.logEnabled;
        UnityEngine.Debug.unityLogger.logEnabled = false;
    }

    [HarmonyPatch(typeof(GameFlagManager), nameof(GameFlagManager.LoadFlags))]
    [HarmonyPostfix]
    private static void AchievementAcquired(bool __state) => UnityEngine.Debug.unityLogger.logEnabled = __state;
}