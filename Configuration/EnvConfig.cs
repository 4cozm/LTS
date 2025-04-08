//환경 변수와 관련된 세팅
//사용하는 기술 Azure Vault

using Azure.Identity;
using DotNetEnv;
namespace LTS.Configuration;
public static class EnvConfig
{
    public static bool IsDevelopment { get; private set; }

    public static string MySqlUserName { get; private set; } = "";
    public static string MySqlIp { get; private set; } = "";
    public static string MySqlPassword { get; private set; } = "";

    public static void Configure(WebApplicationBuilder builder)
    {
        Env.Load();

        IsDevelopment = string.Equals(
            Environment.GetEnvironmentVariable("IsDevelopment"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        builder.Configuration.AddAzureKeyVault(
            new Uri("https://ltsdevkey.vault.azure.net/"),
            new DefaultAzureCredential());

        if (IsDevelopment)
        {
            Console.WriteLine("개발 환경으로 빌드 (Vault + .env 사용)");
            MySqlUserName = builder.Configuration["MYSQL-DEV-USERNAME"] ?? throw new InvalidOperationException("MYSQL-DEV-USERNAME is missing");
            MySqlIp = builder.Configuration["MYSQL-DEV-IP"] ?? throw new InvalidOperationException("MYSQL-DEV-IP is missing");
            MySqlPassword = builder.Configuration["MYSQL-DEV-PASSWORD"] ?? throw new InvalidOperationException("MYSQL-DEV-PASSWORD is missing");
        }
        else
        {
            Console.WriteLine("프로덕션 환경으로 빌드 (Vault만 사용)");
            MySqlUserName = builder.Configuration["MYSQL-USERNAME"] ?? throw new InvalidOperationException("MYSQL-USERNAME is missing");
            MySqlIp = builder.Configuration["MYSQL-IP"] ?? throw new InvalidOperationException("MYSQL-IP is missing");
            MySqlPassword = builder.Configuration["MYSQL-PASSWORD"] ?? throw new InvalidOperationException("MYSQL-PASSWORD is missing");
        }
        Console.WriteLine($"IP: {MySqlIp}, Password: {MySqlPassword}, User: {MySqlUserName}");
    }
}