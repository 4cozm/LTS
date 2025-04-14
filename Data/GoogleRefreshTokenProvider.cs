using LTS.Configuration;
using System.Text.Json;
namespace LTS.Data;

public class GoogleAuthState
{
    public bool IsAuthInProgress { get; private set; } = false;
    public DateTime? StartTime { get; private set; } = null;
    public TimeSpan TimeoutDuration { get; } = TimeSpan.FromMinutes(5); // 타임아웃 기간 (5분)

    // 인증 시작
    public void StartAuthProcess()
    {
        IsAuthInProgress = true;
        StartTime = DateTime.UtcNow;  // 시작 시간 기록
    }

    // 인증 종료
    public void EndAuthProcess()
    {
        IsAuthInProgress = false;
        StartTime = null;
    }

    // 인증 프로세스가 진행 중인지, 그리고 타임아웃이 발생했는지 확인
    public bool IsTimeout()
    {
        if (!IsAuthInProgress || !StartTime.HasValue)
            return false;

        return DateTime.UtcNow - StartTime.Value > TimeoutDuration;
    }

    // 타임아웃을 고려하여 인증이 진행 중인지 확인
    public bool IsValidAuthProcess()
    {
        return IsAuthInProgress && !IsTimeout();
    }
}

public static class GoogleRefreshTokenProvider
{
    // 인증 상태 객체
    private static readonly GoogleAuthState _authState = new();

    public static GoogleAuthState AuthState => _authState;
    // 인증 URL을 출력하는 메서드
    public static void PrintGoogleOAuthUrl(string redirectUri)
    {
        if (AuthState.IsValidAuthProcess())  // 인증이 진행 중이고 타임아웃되지 않았다면
        {
            Console.WriteLine("인증 프로세스가 이미 진행 중입니다.");
            return;
        }

        var clientId = EnvConfig.GoogleApiId;
        var scope = "https://www.googleapis.com/auth/spreadsheets";
        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                      $"scope={Uri.EscapeDataString(scope)}&" +
                      $"access_type=offline&" +
                      $"include_granted_scopes=true&" +
                      $"response_type=code&" +
                      $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                      $"prompt=consent&" +
                      $"client_id={clientId}";

        AuthState.StartAuthProcess();  // 인증 시작
        Console.WriteLine("🔗 아래 링크를 브라우저에 복사해서 여세요:");
        Console.WriteLine(authUrl);
    }

    // 인증 코드 처리 후 상태 변경
    public static async Task<string> HandleGoogleOAuthCallback(string code)
    {
        if (!AuthState.IsValidAuthProcess())  // 인증이 진행 중이지 않거나 타임아웃된 경우
        {
            throw new InvalidOperationException("인증 프로세스가 진행되지 않았거나 타임아웃되었습니다.");
        }

        // 토큰 요청을 위한 코드 처리
        var tokenRequest = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", EnvConfig.GoogleApiId },
                { "client_secret", EnvConfig.GoogleApiPw },
                { "redirect_uri", EnvConfig.GoogleRedirectUri },
                { "grant_type", "authorization_code" }
            };

        var http = new HttpClient();
        var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(tokenRequest));
        var json = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(json).RootElement;

        if (!parsed.TryGetProperty("refresh_token", out var refreshTokenElement))
        {
            throw new InvalidOperationException("Refresh token 발급 실패. prompt=consent을 확인하세요.");
        }

        var refreshToken = refreshTokenElement.GetString() ?? throw new InvalidOperationException("Refresh token is missing from Google");
        // 인증이 끝났으므로 상태를 false로 설정
        AuthState.EndAuthProcess();  // 인증 종료

        // Azure Vault에 저장 (이 예시에서는 저장을 생략)
        // await client.SetSecretAsync("GOOGLE-API-REFRESH-TOKEN", refreshToken);

        return refreshToken;
    }
}
