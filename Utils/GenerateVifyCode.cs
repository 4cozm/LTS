namespace LTS.Utils;


public static class GenerateCodeUtils
{
    public static string GenerateVerificationCode(int length = 6)
    {
        const string digits = "0123456789";
        var rand = new Random();
        return new string(Enumerable.Range(0, length).Select(_ => digits[rand.Next(digits.Length)]).ToArray());
    }
}

