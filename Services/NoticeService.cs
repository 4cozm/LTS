using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Web;

namespace LTS.Services;

public static class NoticeService
{
    private const string NoticeCookieKey = "LTS-Notice";

    public static void RedirectWithNotice(HttpContext context, string message, string redirectUrl)
    {
        var encoded = Uri.EscapeDataString(message);
        context.Response.Cookies.Append(NoticeCookieKey, encoded, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            IsEssential = true,
            Secure = false, // HTTPS만 쓸 거면 true
            SameSite = SameSiteMode.Lax
        });

        context.Response.Redirect(redirectUrl);
    }

    public static void TransferToTempData(HttpContext context, ITempDataDictionary tempData)
    {
        // 쿠키에서 메시지 가져오기
        if (context.Request.Cookies.TryGetValue(NoticeCookieKey, out var encodedMessage))
        {
            var message = Uri.UnescapeDataString(encodedMessage);
            tempData["Notice"] = message;

            // 쿠키는 한 번만 사용되게 삭제
            context.Response.Cookies.Delete(NoticeCookieKey);
        }
    }
}
