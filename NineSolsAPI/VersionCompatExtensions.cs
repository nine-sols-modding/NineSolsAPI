using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace DebugModPlus;

[PublicAPI]
public static class VersionCompatExtensions {
    private static MethodBase? gameCoreChangeScene2 = typeof(GameCore).GetMethod("ChangeScene",
        [typeof(SceneConnectionPoint.ChangeSceneData), typeof(bool), typeof(bool)]);

    private static MethodBase? gameCoreChangeScene3 = typeof(GameCore).GetMethod("ChangeScene",
        [typeof(SceneConnectionPoint.ChangeSceneData), typeof(bool), typeof(bool), typeof(float)]);

    /**
     * Replacement for `GameCore.ChangeScene` which works on all game versions.
     */
    public static void ChangeSceneCompat(this GameCore gameCore, SceneConnectionPoint.ChangeSceneData changeSceneData,
        bool showTip, bool captureLastImage = false, float delayTime = 0) {
        if (gameCoreChangeScene2 != null) {
            gameCoreChangeScene2.Invoke(gameCore, [changeSceneData, showTip, captureLastImage]);
        } else if (gameCoreChangeScene3 != null) {
            gameCoreChangeScene3.Invoke(gameCore, [changeSceneData, showTip, captureLastImage, delayTime]);
        } else {
            throw new Exception("No candidate for GameCore.ChangeScene found");
        }
    }

    private static MethodBase? applicationCoreChangeScene = typeof(ApplicationCore).GetMethod("ChangeScene");

    private static MethodBase? applicationCoreChangeSceneClean = typeof(ApplicationCore).GetMethod("ChangeSceneClean");

    /**
     * Replacement for `ApplicationCore.ChangeScene[Clean]` which works on all game versions.
     */
    public static UniTask ChangeSceneCompat(this ApplicationCore applicationCore, string sceneName,
        bool showTip = true, bool showLoading = false, bool forceSpawnFromSavePoint = false, int savePointIndex = 0) {
        object[] args = [sceneName, showTip, showLoading, forceSpawnFromSavePoint, savePointIndex];
        if (applicationCoreChangeScene != null) {
            return (UniTask)applicationCoreChangeScene.Invoke(applicationCore, args);
        }

        if (applicationCoreChangeSceneClean != null) {
            return (UniTask)applicationCoreChangeSceneClean.Invoke(applicationCore, args);
        }

        throw new Exception("No candidate for ApplicationCode.ChangeScene[Clean] found");
    }

    public static MonsterStat MonsterStatCompat(this MonsterBase monsterBase) {
        var field = typeof(MonsterBase).GetField("_monsterStat") ?? typeof(MonsterBase).GetField("monsterStat");
        return (MonsterStat)field.GetValue(monsterBase);
    }
}