using System;
using System.Reflection;
using JetBrains.Annotations;

namespace DebugModPlus;

[PublicAPI]
public static class VersionCompatExtensions {
    private static MethodBase? changeScene2 = typeof(GameCore).GetMethod("ChangeScene",
        [typeof(SceneConnectionPoint.ChangeSceneData), typeof(bool), typeof(bool)]);

    private static MethodBase? changeScene3 = typeof(GameCore).GetMethod("ChangeScene",
        [typeof(SceneConnectionPoint.ChangeSceneData), typeof(bool), typeof(bool), typeof(float)]);

    /**
     * Replacement for `GameCore.ChangeScene` which works on all game versions.
     */
    public static void ChangeSceneCompat(this GameCore gameCore, SceneConnectionPoint.ChangeSceneData changeSceneData,
        bool showTip, bool captureLastImage = false, float delayTime = 0) {
        if (changeScene2 != null) {
            changeScene2.Invoke(gameCore, [changeSceneData, showTip, captureLastImage]);
        } else if (changeScene3 != null) {
            changeScene3.Invoke(gameCore, [changeSceneData, showTip, captureLastImage, delayTime]);
        } else {
            throw new Exception("No candidate for GameCore.ChangeScene found");
        }
    }
}