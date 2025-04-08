using Microsoft.AspNetCore.Mvc;
using Azure.Identity;
using DotNetEnv;

Env.Load();
var builder = WebApplication.CreateBuilder(args);
var envVar = Environment.GetEnvironmentVariable("ENV");
Console.WriteLine($"🌱 현재 빌드 환경: {envVar}");

// Azure Key Vault 연동
builder.Configuration.AddAzureKeyVault(
    new Uri("https://ltsdevkey.vault.azure.net/"),
    new DefaultAzureCredential());

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
app.Run();
