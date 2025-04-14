using LTS.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTS.Pages.OAuth2Callback
{
    public class IndexModel : PageModel
    {
        public async Task OnGetAsync(string code)
        {
            Console.WriteLine(code);
            // 여기서 code를 넘겨주어 Google OAuth callback 처리
            var refreshToken = await GoogleRefreshTokenProvider.HandleGoogleOAuthCallback(code);

            // 받은 refreshToken을 원하는 방식으로 사용
            // 예: return View(refreshToken);
        }
    }
}