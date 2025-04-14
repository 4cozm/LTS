//환경 변수와 관련된 세팅
//사용하는 기술 Azure Vault

using Azure.Identity;
using DotNetEnv;
using LTS.Data;
namespace LTS.Configuration;
public static class EnvConfig
{
    public static bool IsDevelopment { get; private set; }

    public static string MySqlUserName { get; private set; } = "";
    public static string MySqlIp { get; private set; } = "";
    public static string MySqlPassword { get; private set; } = "";

    public static string GoogleApiId { get; private set; } = "";
    public static string GoogleApiPw { get; private set; } = "";
    public static string? GoogleApiRefreshToken { get; private set; }

    public static string GoogleRedirectUri { get; private set; } = "";
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
            Console.WriteLine("⚠️ 프로덕션 환경으로 빌드 (Vault만 사용)");
            Console.WriteLine("🚨 만약 개발 환경에서 해당 메세지를 보는 경우,루트 디렉터리에 .env 파일을 생성하고, IsDevelopment=true 값을 넣으세요");
            MySqlUserName = builder.Configuration["MYSQL-USERNAME"] ?? throw new InvalidOperationException("MYSQL-USERNAME is missing");
            MySqlIp = builder.Configuration["MYSQL-IP"] ?? throw new InvalidOperationException("MYSQL-IP is missing");
            MySqlPassword = builder.Configuration["MYSQL-PASSWORD"] ?? throw new InvalidOperationException("MYSQL-PASSWORD is missing");
        }
        //공통 사용 환경변수 
        GoogleRedirectUri = IsDevelopment ? "https://localhost:5501/oauth2callback" : "https://ltsga.ddns.net/oauth2callback";
        GoogleApiPw = builder.Configuration["GOOGLE-API-CLIENT-PW"] ?? throw new InvalidOperationException("GOOGLE-API-CLIENT-PW is missing");
        GoogleApiId = builder.Configuration["GOOGLE-API-CLIENT-ID"] ?? throw new InvalidOperationException("GOOGLE-API-CLIENT-ID is missing");
        GoogleApiRefreshToken = builder.Configuration["GOOGLE-API-REFRESH-TOKEN"];
        if (string.IsNullOrEmpty(GoogleApiRefreshToken))
        {
            Console.WriteLine("🚨 Refresh Token이 없음. 구글 인증을 통해 발급 시도 중...");
            GoogleRefreshTokenProvider.PrintGoogleOAuthUrl(GoogleRedirectUri);
        }
        Console.WriteLine("환경 변수 로드 ✅");
    }
}