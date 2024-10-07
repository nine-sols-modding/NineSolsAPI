using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace NineSolsAPI.Patches;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SteamAPI {
    private const bool ENABLE_STEAM_API = true;

    [HarmonyPatch(typeof(SteamManager), "Awake")]
    [HarmonyPrefix]
    private static bool SteamApiAwake() => ENABLE_STEAM_API;
}