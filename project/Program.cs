
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using project.Middlewares;
using project.Services;
using System.Reflection;


namespace project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //настроим автоматическую валидацию входных данных в контроллерах
            builder.Services.AddControllers().ConfigureApiBehaviorOptions(option => 
            {
                option.InvalidModelStateResponseFactory = context => 
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var path = context.HttpContext.Request.Path;//путь запроса
                    var actionName = context.ActionDescriptor.DisplayName;//

                    //получим ошибки валидации
                    var errors = context.ModelState
                    .Where(
                        keyVal => keyVal
                    .Value?
                    .Errors
                    .Count > 0)
                        .ToDictionary(
                            keyVal => keyVal.Key
                        , keyVal => keyVal
                        .Value!
                        .Errors
                        .Select(e => e.ErrorMessage));

                    //залоггируем ошбику валидации
                    logger.LogError("Vallidation Error:{0}, {1}", path, actionName);

                    var problemDetails = new ValidationProblemDetails(context.ModelState)
                    {
                        Title = "Vallidation Error occurred",
                        Status = StatusCodes.Status400BadRequest,
                        Instance = path
                    };
                    var result = new BadRequestObjectResult(problemDetails);
                    return result;

                };
            });

            builder.Services.AddSingleton<IBookingRepository, InMemoryBookingDictionary>();
            builder.Services.AddSingleton<IBookingService, BookingService>();
            builder.Services.AddHostedService<BookingBackgroundService>();



            builder.Services.AddSingleton<IEventService, project.Services.EventService>();

            builder.Services.AddProblemDetails(); 



            builder.Services.AddEndpointsApiExplorer(); 
            builder.Services.AddSwaggerGen(options =>
            {
                // Путь к XML-файлу с документацией
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });




            var app = builder.Build();

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            //app.UseExceptionHandler();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.Run();
        }
    }
}
