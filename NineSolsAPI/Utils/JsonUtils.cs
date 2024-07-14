using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace NineSolsAPI.Utils;

[PublicAPI]
public static class JsonUtils {
    public static string Serialize(object? value, bool indent = false) =>
        JsonConvert.SerializeObject(value, indent ? Formatting.Indented : Formatting.None);

    public static T? Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value);

    public static T? DeserializeStream<T>(Stream stream) {
        using var streamReader = new StreamReader(stream);
        using var reader = new JsonTextReader(streamReader);

        var serializer = JsonSerializer.CreateDefault();
        return serializer.Deserialize<T>(reader);
    }
}