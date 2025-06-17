using Microsoft.AspNetCore.Mvc;
using LTS.Configuration;
using LTS.Services;
using LTS.MiddleWare;

var builder = WebApplication.CreateBuilder(args);
EnvConfig.Configure(builder);


// 서비스 등록 (DI)
builder.Services
    .AddLtsCoreServices()
    .AddInfrastructureServices()
    .AddWebUiServices()
    .AddCommonUtilities()
    .AddHostedService<TcpClientBackgroundService>();
// Web 및 Razor 설정
ConfigureWeb(builder.Services);

var app = builder.Build();
app.UseSession();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapHub<StatusHub>("/statusHub");
app.UseMiddleware<SessionValidationMiddleware>();


Console.WriteLine("등록된 매장🏢");
Console.WriteLine(string.Join(", ", StoreService.GetAllStores()));

Console.WriteLine("서버 작동 ✅");
app.Run();

void ConfigureWeb(IServiceCollection services)
{
    services.AddRazorPages(options =>
    {
        options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
    });
    services.AddAntiforgery();
}