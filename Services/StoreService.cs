namespace LTS.Services
{
    public static class StoreService
    {
        private static readonly List<string> _validStores =
        [
            "GA",
            "EH"
        ];

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
    }
}
