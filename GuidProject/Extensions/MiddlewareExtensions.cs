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

            // ✅ Enable Authentication & Authorization
            app.UseAuthentication();

            app.MapControllers();
        }
    }
}
