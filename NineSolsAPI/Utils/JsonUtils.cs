using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class JsonUtils {
    public static string Serialize(object? value, bool indent = false) =>
        JsonConvert.SerializeObject(value, indent ? Formatting.Indented : Formatting.None);

    public static T? Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value);
}