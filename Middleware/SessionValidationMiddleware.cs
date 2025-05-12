using LTS.Services;


namespace LTS.MiddleWare;
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // 인증 제외 경로 지정
        var excludedPaths = new[] { "/", "/Index", "/Register" };

        if (!excludedPaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
        {
            var token = context.Request.Cookies["LTS-Session"];
            if (string.IsNullOrEmpty(token) || !LoginService.ValidateToken(token))
            {
                context.Response.Redirect("/Index");
                return; // 미들웨어 체인 중단
            }
        }

        // 다음 미들웨어로 이동
        await _next(context);
    }
}
