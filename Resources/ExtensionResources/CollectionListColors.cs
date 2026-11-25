namespace CollectionLib.Resources.ExtensionResources;

internal static class CollectionListColors
{
    public static Color Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (CollectionListDefaultResourceDictionary.Instance.TryGetValue(key, out object? value) && value is Color color) return color;

        throw new KeyNotFoundException($"CollectionLib color resource '{key}' was not found.");
    }

    public static Color ForTheme(string lightKey, string darkKey) => Get(Application.Current?.RequestedTheme == AppTheme.Dark ? darkKey : lightKey);

    public static Color[] GetPalette(params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        return keys.Select(Get).ToArray();
    }
}