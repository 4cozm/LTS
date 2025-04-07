using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery();
var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();
app.UseRouting();

app.Run();

