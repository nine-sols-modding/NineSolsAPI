using JetBrains.Annotations;
using UnityEngine;

namespace NineSolsAPI;

[PublicAPI]
public static class GameVersions {
    public const string SpeedrunPatch = "d4c12f4d7e8442e79988244014fb92d2";

    public static T Select<T>(string version, T yes, T no) {
        return IsVersion(version) ? yes : no;
    }

    public static bool IsVersion(string version) => Application.buildGUID == version;
}