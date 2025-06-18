public static class StoreService
{
    private static readonly Dictionary<string, string> _storeNameMap = new()
    {
        { "GA", "광안점" },
        { "EH", "은행점" }
    };

    private static readonly List<string> _validStores = _storeNameMap.Keys.ToList();

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

    public static string? GetStoreDisplayName(string storeCode)
    {
        var hangul = _storeNameMap.TryGetValue(storeCode, out var name) ? name : null;
        if (hangul == null)
        {
            return storeCode;
        }
        return hangul;
    }
}
