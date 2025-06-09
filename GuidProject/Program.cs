using GuidProject.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ✅ Enable Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// ✅ Move Service Registrations to ServiceExtensions
builder.Services.ConfigureDependencyInjection(builder.Configuration);

var app = builder.Build();

// ✅ Move Middleware Configuration to MiddlewareExtensions
app.ConfigureMiddleware();

app.Run();
