using UnityEngine;

namespace NineSolsAPI;

internal static class GameVersions {
    public const string BuildGuidSpeedrunpatch = "d4c12f4d7e8442e79988244014fb92d2";

    public static bool IsVersion(string version) => Application.buildGUID == version;
}