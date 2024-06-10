using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace NineSolsAPI;

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

    [HarmonyPatch(typeof(VersionText), "Start")]
    [HarmonyPostfix]
    private static void Version(ref VersionText __instance) {
        __instance.text.text = $"Modding API: {NineSolsAPICore.PluginVersion}\n{__instance.text.text}";
    }
}