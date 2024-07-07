using System;
using JetBrains.Annotations;

namespace NineSolsAPI.Preload;

[AttributeUsage(AttributeTargets.Field)]
[MeansImplicitUse]
[PublicAPI]
public class PreloadAttribute(string scene, string path) : Attribute {
    public string Scene = scene;
    public string Path = path;
}