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
        // var excludedPaths = new[] { "/", "/Index" };

        if (!excludedPaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
        {
            var token = context.Request.Cookies["LTS-Session"];
            if (string.IsNullOrEmpty(token))
            {
                await NoticeService.RedirectWithNoticeAsync(context, "로그인이 필요합니다", "/Index");
                return;
            }

        //     var (isValid, employee) = LoginService.TryGetValidEmployeeFromToken(token);

            if (!isValid || employee == null)
            {
                await NoticeService.RedirectWithNoticeAsync(context, "세션이 만료되었거나 유효하지 않습니다", "/Index");
                return;
            }
            if (!employee.IsPasswordChanged)
            {
                await NoticeService.RedirectWithNoticeAsync(context, "첫 로그인시 비밀번호를 변경해야 합니다", "/ChangePassword");
                return;
            }
            context.Items["Employee"] = employee;
        }

        await _next(context);
    }
}
