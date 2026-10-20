using SkyNet;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
Bakery.Core.Local.Accessor = app.Services.GetRequiredService<IHttpContextAccessor>();
app.UseMiddleware<IHandler>();

app.UseStaticHttpCurrent();

app.Run();
