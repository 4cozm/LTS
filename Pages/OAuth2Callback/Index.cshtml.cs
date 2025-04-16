using LTS.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTS.Pages.OAuth2Callback
{
    public class IndexModel : PageModel
    {
        public async Task OnGetAsync(string code)
        {
            await GoogleRefreshTokenProvider.HandleGoogleOAuthCallback(code);
        }
    }
}