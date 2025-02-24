using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using NineSolsAPI.Preload;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

namespace NineSolsAPI.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PatchesSpeedrunpatch {
    [HarmonyPatch(typeof(GameFlagManager), "LoadFlags")]
    [HarmonyPrefix]
    private static void LoadFlagsPre(out bool __state) {
        __state = UnityEngine.Debug.unityLogger.logEnabled;
        UnityEngine.Debug.unityLogger.logEnabled = false;
    }

    [HarmonyPatch(typeof(GameFlagManager), "LoadFlags")]
    [HarmonyPostfix]
    private static void LoadFlagsPost(bool __state) {
        UnityEngine.Debug.unityLogger.logEnabled = __state;
    }


    [HarmonyPatch(typeof(StatDataCollection), nameof(StatDataCollection.ClearStats))]
    [HarmonyPrefix]
    private static void ClearFlagsPre(out bool __state) {
        UnityEngine.Debug.Log("Clear All Stats");
        __state = UnityEngine.Debug.unityLogger.logEnabled;
        UnityEngine.Debug.unityLogger.logEnabled = false;
    }

    [HarmonyPatch(typeof(StatDataCollection), nameof(StatDataCollection.ClearStats))]
    [HarmonyPostfix]
    private static void ClearFlagsPost(bool __state) {
        UnityEngine.Debug.unityLogger.logEnabled = __state;
    }
}