using LTS.Data.Repository;
using LTS.Services;
using LTS.MiddleWare;

namespace LTS.Configuration;

public static class DependencyInjection
{
    // 🔹 도메인 서비스 (로그인, 직원관리 등 핵심 기능)
    public static IServiceCollection AddLtsCoreServices(this IServiceCollection services)
    {
        services.AddScoped<EmployeeRepository>();
        services.AddScoped<LoginService>();
        services.AddSingleton<SessionStore>();
        services.AddScoped<SessionValidationMiddleware>();
        // TODO: Add more core services (예: 회원가입, 근무조회 등)

        return services;
    }

    // 🔹 인프라 서비스 (DB, 외부 API, 파일 시스템 등)
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 예: MySql 연결 설정, HttpClient 설정 등
        // services.AddHttpClient<IMyApiClient, MyApiClient>();

        return services;
    }

    // 🔹 UI/Web 관련 서비스 (Razor Pages, MVC, 필터 등)
    public static IServiceCollection AddWebUiServices(this IServiceCollection services)
    {
        services.AddRazorPages(options =>
        {
            // 모든 Razor 페이지에 자동 CSRF 방어 필터 적용
            options.Conventions.ConfigureFilter(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
        });

        services.AddAntiforgery();

        return services;
    }

    // 🔹 공통 유틸리티 (환경설정, 시간, 로깅 등 범용 기능)
    public static IServiceCollection AddCommonUtilities(this IServiceCollection services)
    {
        // 예: 시간 관련 유틸, 환경설정 래퍼 등
        // services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        return services;
    }
}
