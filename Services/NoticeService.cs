using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;


namespace LTS.Services;

public static class NoticeService
{
    private const string NoticeCookieKey = "LTS-Notice";

    // Razor Pages용
    public static IActionResult RedirectWithNotice(HttpContext context, string message, string redirectUrl)
    {
        var encoded = Uri.EscapeDataString(message);
        context.Response.Cookies.Append("LTS-Notice", encoded, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            IsEssential = true
        });

        return new RedirectResult(redirectUrl);
    }

    // 미들웨어용
    public static async Task RedirectWithNoticeAsync(HttpContext context, string message, string redirectUrl)
    {
        var encoded = Uri.EscapeDataString(message);
        context.Response.Cookies.Append("LTS-Notice", encoded, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            IsEssential = true
        });

        context.Response.Redirect(redirectUrl);
        await context.Response.CompleteAsync(); // 응답 종료
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
