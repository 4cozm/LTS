using Microsoft.AspNetCore.Mvc;
using LTS.Configuration;
using LTS.Data;

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
Console.WriteLine("서버 작동 ✅");
app.Run();
