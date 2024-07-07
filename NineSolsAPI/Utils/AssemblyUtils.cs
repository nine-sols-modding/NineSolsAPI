using System.IO;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class AssemblyUtils {
    private static Stream? GetEmbeddedResource(Assembly assembly, string fileName) {
        var stream = assembly.GetManifestResourceStream(fileName);
        if (stream is null) {
            var embeddedResources = assembly.GetManifestResourceNames();
            Log.Error(
                embeddedResources.Length == 0
                    ? $"Could not load embedded resource '{fileName}', the assembly {assembly.GetName().Name} contains no resources"
                    : $"Could not load embedded resource '{fileName}', did you mean one of {embeddedResources.Join()}?");
            return null;
        }

        return stream;
    }


    public static AssetBundle? GetEmbeddedAssetBundle(string name) {
        var assembly = Assembly.GetCallingAssembly();
        var stream = GetEmbeddedResource(assembly, name);
        if (stream is null) return null;

        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return AssetBundle.LoadFromMemory(memoryStream.ToArray());
    }

    public static T? GetEmbeddedJson<T>(string name) {
        var assembly = Assembly.GetCallingAssembly();
        var stream = GetEmbeddedResource(assembly, name);
        if (stream is null) return default;

        using var reader = new StreamReader(stream);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }
}