using System;
using System.IO;
using System.Linq;
using BepInEx;
using JetBrains.Annotations;
using UnityEngine;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class ModDirs {
    public static string DataDir(BaseUnityPlugin mod, params string[] subDirs) =>
        DataDir(mod.Info.Metadata.GUID, subDirs);

    private static string DataDir(string modGuid, params string[] subDirs) {
        var gameDir = Directory.GetParent(Application.dataPath);
        if (gameDir == null) {
            throw new Exception($"{Application.dataPath} is not a valid game directory?");
        }

        var folder = subDirs.Aggregate(Path.Combine(gameDir.FullName, "ModData", modGuid), Path.Combine);
        Directory.CreateDirectory(folder);
        return folder;
    }
}