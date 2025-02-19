using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class TextureUtils {
    public static void WritePNGToDisk(string path, Texture2D source) {
        var tex = source.isReadable ? source : Duplicate(source);
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    public static Texture2D Duplicate(Texture2D source) {
        var renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        var previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        var readableTexture = new Texture2D(source.width, source.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableTexture.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableTexture;
    }
}