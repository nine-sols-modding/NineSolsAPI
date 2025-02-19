namespace NineSolsAPI.Utils;

public static class CollectionExtensions {
    public static T? GetValueOrDefault<T>(this T[] array, int index) where T : class =>
        array.Length > index ? array[index] : null;
}