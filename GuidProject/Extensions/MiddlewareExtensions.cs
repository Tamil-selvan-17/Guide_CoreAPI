using Microsoft.Extensions.FileProviders;

namespace GuidProject.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void ConfigureMiddleware(this WebApplication app)
        {
            // ✅ Global Exception Handling Middleware
            app.UseMiddleware<GlobalExceptionHandler>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // ✅ Enable Response Compression
            app.UseResponseCompression();

            // ✅ Enable CORS BEFORE authentication
            app.UseCors("AllowAngular");

            // ✅ Enable Static Files for "Source"
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Source")),
                RequestPath = "/Source"
            });

            // ✅ Enable Authentication & Authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
        }
    }
}
