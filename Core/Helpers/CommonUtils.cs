namespace Core.Helpers;

public static class CommonUtils
{
    public static readonly Func<string, bool> IsEmptyString = (value) => string.IsNullOrEmpty(value) && string.IsNullOrWhiteSpace(value);
    public static readonly Func<int, bool> IsActiveStream = (state) => (state switch { 0 => true, 1 => true, -1 => false });
    public static readonly Func<IDictionary<string, string>, bool> IsEmptySet = (map) => map == null && map.Count == 0;
}