public static class StoreService
{
    private static readonly Dictionary<string, (string Name, string Phone)> _storeMap = new()
    {
        { "GA", ("광안점", "051-758-1123") },
        { "EH", ("은행점", "042-222-9111") }
    };

    private static readonly List<string> _validStores = _storeMap.Keys.ToList();

    private static readonly List<string> _validRoles =
    [
        "Staff",
        "Manager"
    ];

    public static bool IsValidStoreName(string storeName)
    {
        return _validStores.Contains(storeName);
    }

    public static List<string> GetAllStores()
    {
        return _validStores;
    }

    public static bool IsValidRole(string roleName)
    {
        return _validRoles.Contains(roleName);
    }

    public static List<string> GetAllRoles()
    {
        return _validRoles;
    }

    public static string GetStoreDisplayName(string? storeCode)
    {
        if (string.IsNullOrWhiteSpace(storeCode))
            return "지점 정보 없음";

        return _storeMap.TryGetValue(storeCode, out var info)
            ? info.Name
            : $"알 수 없음({storeCode})";
    }

    public static string? GetStorePhoneNumber(string storeCode)
    {
        return _storeMap.TryGetValue(storeCode, out var info)
            ? info.Phone
            : null;
    }
}
