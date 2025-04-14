using System.Text.Json;
using LTS.Configuration;

namespace LTS.Data;

public static class GetGoogleAccessToken
{
    private static readonly HttpClient http = new();

    public static async Task<string> GetAsync()
    {
        // 만료되지 않았다면 캐시된 토큰 사용
        if (!string.IsNullOrEmpty(EnvConfig.AccessToken) && DateTime.UtcNow < EnvConfig.AccessTokenExpiry)
        {
            return EnvConfig.AccessToken!;
        }

        // 새 access token 발급
        var request = new Dictionary<string, string>
        {
            { "client_id", EnvConfig.GoogleApiId },
            { "client_secret", EnvConfig.GoogleApiPw },
            { "refresh_token", EnvConfig.GoogleApiRefreshToken ?? throw new InvalidOperationException("RefreshToken 없음") },
            { "grant_type", "refresh_token" }
        };

        var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(request));
        var json = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(json).RootElement;

        var accessToken = parsed.GetProperty("access_token").GetString()!;
        var expiresIn = parsed.GetProperty("expires_in").GetInt32(); // 보통 3600초

        EnvConfig.AccessToken = accessToken;
        EnvConfig.AccessTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 안전하게 1분 여유

        return accessToken;
    }
}
