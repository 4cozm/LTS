var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();
app.Use(async (context, next) =>
{
    if (context.Request.Method == "POST")
    {
        Console.WriteLine("🔥 POST 요청 감지됨: " + context.Request.Path);
    }
    await next();
});

app.Run();

