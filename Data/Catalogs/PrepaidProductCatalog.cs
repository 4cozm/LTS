namespace LTS.Utils;

public static class PrepaidProductCatalog
{
    public class ProductInfo
    {
        public string Code { get; init; } = "";
        public string DisplayName { get; init; } = "";
        public int Count { get; init; }
        public int Price { get; init; } // 원화

        public string GetDescription() => $"{DisplayName} / {Count}게임 / {Price:N0}원";
    }

    private static readonly Dictionary<string, ProductInfo> _products = new()
    {
        ["bronze"] = new ProductInfo
        {
            Code = "bronze",
            DisplayName = "브론즈권",
            Count = 10,
            Price = 55000
        },
        ["silver"] = new ProductInfo
        {
            Code = "silver",
            DisplayName = "실버권",
            Count = 20,
            Price = 100000
        },
        ["gold"] = new ProductInfo
        {
            Code = "gold",
            DisplayName = "골드권",
            Count = 30,
            Price = 135000
        }
    };

    public static bool IsValidProduct(string code) => _products.ContainsKey(code);

    public static ProductInfo? GetByCode(string code)
        => _products.TryGetValue(code, out var info) ? info : null;

    public static List<ProductInfo> GetAllProducts()
        => _products.Values.OrderBy(p => p.Count).ToList();
}
