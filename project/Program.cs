
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
            builder.Services.AddSingleton<IEventService, project.Services.EventService>();

            builder.Services.AddProblemDetails(); //IProblemDetailsService
            //builder.Services.AddProblemDetails(options =>
            //{
            //    options.CustomizeProblemDetails = ctx =>
            //    {
            //        //не понятно почему, но иногда ctx.Exception может быть null, поэтому проверяем и берем ошибку из IExceptionHandlerFeature
            //        var actualException = ctx.Exception is AggregateException ae
            //            ? ae.InnerException
            //            : ctx.Exception;
            //        if (actualException == null) 
            //        {
            //            var exceptionFeature = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>();
            //            actualException = ctx.Exception ?? exceptionFeature?.Error;
            //        }

            //        if (actualException is EventNotFoundExcpetion)
            //        {
            //            ctx.ProblemDetails.Status = StatusCodes.Status404NotFound;
            //            ctx.ProblemDetails.Title = "Событие не найдено";
            //            ctx.ProblemDetails.Detail = actualException.Message; ;
            //        }
            //        if (actualException is EntityAlreadyExistsException)
            //        {
            //            ctx.ProblemDetails.Status = StatusCodes.Status400BadRequest;
            //            ctx.ProblemDetails.Title = "Событие уже существует";
            //            ctx.ProblemDetails.Detail = actualException.Message;
            //        }
            //    };
            //});




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
