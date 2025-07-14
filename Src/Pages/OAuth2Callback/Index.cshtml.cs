
using LTS.Base;
using LTS.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTS.Pages.OAuth2Callback
{
    public class IndexModel : BasePageModel
    {
        public async Task OnGetAsync(string code)
        {
            await GoogleRefreshTokenProvider.HandleGoogleOAuthCallback(code);
        }
    }
}
