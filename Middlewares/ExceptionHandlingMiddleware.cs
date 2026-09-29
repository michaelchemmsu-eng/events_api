using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace project.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) 
        {
            try
            {
                await _next(context);
            }
            //ошибка валидации модели
            catch (Exception ex)
            {
                var (statusCode, title) = ex switch
                {
                    //404 Not Found для ситуаций, когда ресурс не найден;
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Resourse not found"),
                    
                    
                    //400 Bad Request для ошибок валидации;
                    ArgumentNullException or
                    ArgumentOutOfRangeException or
                    ArgumentException or
                    BadHttpRequestException => (StatusCodes.Status400BadRequest, "incorrect request"),

                    //500 Internal Server Error для непредвиденных ошибок.
                    _ => (StatusCodes.Status500InternalServerError, "Internal server error")
                };
                _logger.LogError(ex, "An error occurred while processing the request." +
                    " Method:{0}, Path:{1}", context.Request.Method, context.Request.Path);
                if (context.Response.HasStarted)
                { 
                    return;
                    _logger.LogWarning("Response has started. Cannot send Problem Details");
                }
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = ex.Message,
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsJsonAsync(problemDetails);

            }

        }

    }
}
