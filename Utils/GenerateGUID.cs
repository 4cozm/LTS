namespace LTS.Utils;

public static class PrepaidCardUtil
{
    public static string GenerateCardCode()
    {
        return $"PC-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}