using Repository.Services;
using System.Net;
using System.Text.Json;

namespace GuidProject.Extensions
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly ExceptionHandler _exceptionHandler;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, ExceptionHandler exceptionHandler)
        {
            _next = next;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                // Send Email using ExceptionHandler
                _exceptionHandler.HandleException(context, ex);

                await HandleExceptionAsync(context, ex.Message);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, string message)
        {
            var response = new { status = false, message = "An error occurred: " + message };
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = false }));
        }
    }
}
