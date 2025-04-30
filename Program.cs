using Microsoft.AspNetCore.Mvc;
using LTS.Configuration;
using LTS.Services;

var builder = WebApplication.CreateBuilder(args);
EnvConfig.Configure(builder);


// Razor Pages 설정 및 Antiforgery
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

Console.WriteLine("등록된 매장🏢");
Console.WriteLine(string.Join(", ", StoreService.GetAllStores()));

Console.WriteLine("서버 작동 ✅");
app.Run();
