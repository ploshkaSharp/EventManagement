using System.Reflection;
using Microsoft.OpenApi;
using EventManagement.Services;
using EventManagement.Middleware;
using EventManagement.Data;
using EventManagement.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
  options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
  options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
  options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Register Repositories
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
// Register Services (DbContext is scoped)
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();
// Register Background Service (singleton)
builder.Services.AddHostedService<BookingBackgroundService>();

// Настройка Swagger с поддержкой XML-комментариев
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo
  {
    Title = "Event Management API",
    Version = "v1",
    Description = "API для управления мероприятиями и их бронированием"
  });

  // Включение XML-комментариев для документации
  var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  c.IncludeXmlComments(xmlPath);

  // Добавление аннотаций для типов ответов
  c.EnableAnnotations();
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();  
  db.Database.Migrate();
}

// middleware для глобальной обработки ошибок. Ставить первым в pipeline для перехвата всех исключений
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();