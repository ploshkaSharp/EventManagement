using System.Reflection;
using Microsoft.OpenApi;
using EventManagement.Services;
using EventManagement.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка Swagger с поддержкой XML-комментариев
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Management API",
        Version = "v1",
        Description = "API для управления мероприятиями"
    });

    // Включение XML-комментариев для документации
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Добавление аннотаций для типов ответов
    c.EnableAnnotations();
});
// Singleton (in-memory)
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();
// middleware для глобальной обработки ошибок. Ставить первым в pipeline для перехвата всех исключений
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();