using System.Net;
using System.Text.Json;

namespace PersonalDiaryApp.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Normal çalışıyorsa devam
            }
            catch (Exception ex)
            {
                _logger.LogError($"Bir hata oluştu: {ex.Message}");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    status = context.Response.StatusCode,
                    message = "Sunucuda bir hata oluştu.",
                    error = ex.Message // istersen user'a gösterme sadece logla
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}

