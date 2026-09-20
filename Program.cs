
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
            builder.Services.AddScoped<IEventService, EventService>();

            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = ctx =>
                {
                    
                    if (ctx.Exception is EventNotFoundExcpetion)
                    {
                        ctx.ProblemDetails.Status = StatusCodes.Status404NotFound;
                        ctx.ProblemDetails.Title = "Событие не найдено";
                        ctx.ProblemDetails.Detail = ctx.Exception.Message;
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
