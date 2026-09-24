
using Microsoft.AspNetCore.Diagnostics;
using project.Excpetions;
using project.Services;
using System.Reflection;

namespace project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IEventService, EventService>();

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    //не понятно почему, но иногда ctx.Exception может быть null, поэтому проверяем и берем ошибку из IExceptionHandlerFeature
                    var actualException = ctx.Exception is AggregateException ae
                        ? ae.InnerException
                        : ctx.Exception;
                    if (actualException == null) 
                    {
                        var exceptionFeature = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>();
                        actualException = ctx.Exception ?? exceptionFeature?.Error;
                    }

                    if (actualException is EventNotFoundExcpetion)
                    {
                        ctx.ProblemDetails.Status = StatusCodes.Status404NotFound;
                        ctx.ProblemDetails.Title = "Событие не найдено";
                        ctx.ProblemDetails.Detail = actualException.Message; ;
                    }
                    if (actualException is EntityAlreadyExistsException)
                    {
                        ctx.ProblemDetails.Status = StatusCodes.Status400BadRequest;
                        ctx.ProblemDetails.Title = "Событие уже существует";
                        ctx.ProblemDetails.Detail = actualException.Message;
                    }
                };
            });



           
            builder.Services.AddEndpointsApiExplorer(); 
            builder.Services.AddSwaggerGen(options =>
            {
                // Путь к XML-файлу с документацией
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });




            var app = builder.Build();

            app.UseExceptionHandler();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.Run();
        }
    }
}
