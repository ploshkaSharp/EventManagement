# Event Management
REST API для управления мероприятиями и их бронированием с CRUD-операциями (создание, просмотр, обновление, удаление).

Валидация входных данных (обязательность заполнения, дата окончания должна быть позже даты начала).

Интерфейсы для изоляции бизнес-логики (`IEventService`, `IBookingService`).

Swagger для упрощения тестирования и документирования.

Используется Entity Framework Core Migrations для управления схемой БД. Все изменения структуры базы данных выполняются через миграции.

## Структура базы данных:

- Таблица "Events":
  * Id (uuid, PK) - уникальный идентификатор мероприятия
  * Title (varchar(200), NOT NULL) - название мероприятия
  * Description (varchar(1000)) - описание мероприятия
  * StartAt (timestamp with time zone, NOT NULL) - дата и время начала
  * EndAt (timestamp with time zone, NOT NULL) - дата и время окончания
  * TotalSeats (integer, NOT NULL) - общее количество мест
  * AvailableSeats (integer, NOT NULL) - количество свободных мест

- Таблица "Bookings":
  * Id (uuid, PK) - уникальный идентификатор бронирования
  * EventId (uuid, FK -> Events.Id) - идентификатор мероприятия
  * Status (varchar(20), NOT NULL) - статус бронирования (Pending/Confirmed/Rejected)
  * CreatedAt (timestamp with time zone, NOT NULL) - дата создания
  * ProcessedAt (timestamp with time zone) - дата обработки

- Внешние ключи:
  * FK_Bookings_Events_EventId - связывает Bookings.EventId с Events.Id
  * ON DELETE RESTRICT - запрещает удаление мероприятия с активными бронями

- Индексы:
  * IX_Events_StartAt - для ускорения фильтрации по дате
  * IX_Events_Title - для ускорения поиска по названию
  * IX_Bookings_EventId - для ускорения поиска броней по мероприятию
  * IX_Bookings_Status - для ускорения фильтрации по статусу
  * IX_Bookings_CreatedAt - для ускорения сортировки по дате создания


## Cтек разработки

- **.NET 10 (ASP.NET Core Web API, EF Core, C#)**
- **PostgreSQL**
- **Swashbuckle.AspNetCore** (Swagger)
- **xUnit**

## Архитектура и структура проекта

Проект построен на принципах Clean Architecture с разделением на четыре слоя:

1. Domain (EventManagement.Domain)
   - Доменные сущности (Event, Booking)
   - Перечисления (BookingStatus)
   - Доменные исключения
   - Не зависит от внешних библиотек

2. Application (EventManagement.Application)
   - Use Cases (EventService, BookingService)
   - DTO
   - Интерфейсы портов (репозитории)
   - Фоновые сервисы
   - Зависит только от Domain

3. Infrastructure (EventManagement.Infrastructure)
   - Реализации репозиториев
   - DbContext и конфигурации
   - Миграции
   - Зависит от Application и Domain

4. Presentation (EventManagement.Presentation)
   - Контроллеры
   - Глобальная обработка ошибок
   - Composition Root (Program.cs)
   - Зависит от Application и Infrastructure

Направление зависимостей:
```bash
Presentation → Infrastructure
Presentation → Application
Infrastructure → Application
Infrastructure → Domain
Application → Domain
```

```bash
EventManagement/
├── **Application**/
│ └── Background/
│ │ └── BookingBackgroundService.cs  #Фоновый сервис для обработки бронирований
│ └── DTO/
│ │ ├── BookingDTO.cs                #DTO объекты (бронирование)
│ │ ├── EventDTO.cs                  #DTO объекты (мероприятия)
│ │ ├── EventFilterDTO.cs            #DTO для параметров фильтрации
│ │ └── PaginateResultDTO.cs         #DTO для пагинированного результата
│ └── Mapper/
│ │ ├── BookingMapper.cs             #Маппинг DTO объектов (бронирование)
│ │ └── EventMapper.cs               #Маппинг DTO объектов (мероприятия)
│ └── Ports/
│ │ ├── IBookingRepository.cs        #Интерфейс репозитория управления бронированием
│ │ └── IEventRepository.cs          #Интерфейс репозитория управления мероприятиями
│ └─ Services/
│ │ ├── BookingService.cs            #Реализация бизнес-логики управления бронированиями
│ │ ├── Constants.cs                 #Константы
│ │ └── EventService.cs              #Реализация бизнес-логики управления мероприятиями
│ └── DependencyInjection.cs         #extension-метод для DI
│
├── **Domain**/
│ └─ Models/
│ │ ├── Booking.cs                   #Модель бронирования мероприятия
│ │ ├── BookingStatus.cs             #Перечисление статусов бронирования 
│ │ └── Event.cs                     #Доменная модель (сущность)
│ └─ Exceptions/
│   ├── BadRequestException.cs       #Исключение - Некорректный запрос
│   ├── NotAvailableException.cs     #Исключение - Нет доступных мест
│   ├── NotFoundException.cs         #Исключение - Ресурс не найден
│   └── ValidationException.cs       #Исключение - Ошибка валидации
│
├── **Infastructure**/
│ ├─ DataAccess/
│ │  └── Configurations/             #Настрока таблиц в БД
│ │   ├── BookingConfiguration.cs    #Настройка таблицы Bookings
│ │   └── EventConfiguration.cs      #Настройка таблицы Events
│ └── AppDbContext.cs                #Контекст БД
│ └─ Repositories/
│   ├── BookingRepository.cs         #Репозиторий бронирований
│   └── EventRepository.cs           #Репозиторий мероприятий
│
├── **Presentation**/
│ └─ Controllers/
│ │ ├── BookingController.cs         #Эндпоинты API (бронирование)
│ │ └── EventsController.cs          #Эндпоинты API (мероприятия)
│ └─ Middleware/
│   └── GlobalExceptionHandlingMiddleware.cs  #Глобальная обработка исключений (middleware)
│ └─ ErrorResponse.cs               #Модель ответа об ошибке в формате Problem Details (RFC 7807)
│
├── Tests/
│ └── Migration/
│     └── DBSchemeTests.cs           #Тесты схемы БД после миграции
│ └── Integration/
│     ├── InitTests.cs               #Инициализация интеграционных тестов
│     ├── BookingRepositoryTests.cs  #Интеграционные тесты бронирования
│     └── EventRepositoryTests.cs    #Интеграционные тесты мероприятий
│ ├── UnitData/
│ │   └── DataGenerator.cs           #Генератор тестовых данных юнит тестов
│ ├── Unit/
│ │   ├── BookingServiceSeatsTest.cs #Тестовые сценарии логики мест для бронирования
│ │   ├── BookingServiceTest.cs      #Тестовые сценарии для бронирования
│ │   └── EventServiceTest.cs        #Тестовые сценарии (успешные, неуспешные, пограничные)
│ └── Tests.csproj                   #Проект с тестами
├── Program.cs                       #Точка входа в приложение с конфигурацией DI
├── appsettings.json                 #Настройки приложения
└── appsettings.Development.json     #Настройки приложения (окружение разработчика)
```

## Мероприятия

### Модель мероприятия 
```
  /// Уникальный идентификатор мероприятия (Guid)
  Id 
  
  /// Название мероприятия
  Title

  /// Описание мероприятия (необязательное)
  Description

  /// Дата и время начала мероприятия
  StartAt
  
  /// Дата и время окончания мероприятия  
  EndAt
  
  /// Общее количество мест на мероприятии  
  TotalSeats
  
  /// Текущее количество свободных мест  
  AvailableSeats
  
  /// Попытаться забронировать места (с указанием количества мест, по умолчанию 1)
  TryReserveSeats
  
  /// Освободить места (с указанием количества мест, по умолчанию 1)
  ReleaseSeats
```

## Бронирование

### Возможные статусы бронирования
```
  /// Ожидание
  Pending = 0
    
  /// Подтверждено
  Confirmed = 1
    
  /// Отклонено
  Rejected = 2
```

### Модель бронирования мероприятия Booking
```
  /// Уникальный идентификатор брони (GUID)
  Id

  /// Идентификатор мероприятия (GUID), к которому относится бронь
  EventId

  /// Текущий статус брони (перечислены в предыдущем разделе)
  Status

  /// Дата и время создания брони 
  CreatedAt

  /// Дата и время обработки брони (необязательное)
  ProcessedAt
```
### Логика фоновой обработки бронирования
После успешного создания брони для мероприятия, брони присваивается уникальный ИД. Статус бронирования устанвливается равным Ожидание (`Pending`). Устанавлвается текущее время создания брони.

Фоновый сервис с периодом 5 секунд собирает вновь созданные брони. Для каждой новой брони имитируется обработка бронирования (задержка 2 сек). После чего брони устанавливается статус равный Подтверждено (`Confirmed`), устанавливается время последней обработки брони (`ProcessedAt`) равное текущему.


## АУТЕНТИФИКАЦИЯ И АВТОРИЗАЦИЯ

### Ролевая модель
-------------------
Система поддерживает две роли:
- User - обычный пользователь
- Admin - администратор

### Разграничение прав
----------------------
| Действие                          | User      | Admin     |
|-----------------------------------|-----------|-----------|
| Регистрация                       | ✅        | ✅        |
| Вход в систему                    | ✅        | ✅        |
| Просмотр мероприятий              | ✅        | ✅        |
| Создание мероприятия              | ❌        | ✅        |
| Редактирование мероприятия        | ❌        | ✅        |
| Удаление мероприятия              | ❌        | ✅        |
| Создание брони                    | ✅        | ✅        |
| Отмена своей брони                | ✅        | ✅        |
| Отмена чужой брони                | ❌        | ✅        |
| Просмотр своих броней             | ✅        | ✅        |
| Просмотр всех броней              | ❌        | ✅        |

### Получение JWT-токена через Swagger
---------------------------------------
1. Откройте Swagger UI (/swagger)
2. Нажмите на кнопку "Authorize" в правом верхнем углу
3. В поле "Value" введите: Bearer <ваш_токен>
4. Нажмите "Authorize"

### Получение токена через API
------------------------------
```bash
POST /auth/register
{
  "login": "user",
  "password": "password123",
  "role": "User"  // или "Admin"
}
```

```bash
POST /auth/login
{
  "login": "user",
  "password": "password123"
}
```

Ответ: { "token": "eyJhbGciOiJIUzI1NiIs..." }

### Настройка JWT в конфигурации
---------------------------------
В файле appsettings.json:
```bash
{
  "JwtSettings": {
    "Secret": "YOUR_SUPER_SECRET_KEY_MINIMUM_32_CHARS",
    "Issuer": "EventManagementAPI",
    "Audience": "EventManagementClient",
    "ExpiryMinutes": 60
  }
}
```

Важно: В продакшне используйте безопасный секретный ключ, храните его в защищённом месте (например, в Azure Key Vault или переменных окружения). Никогда не храните реальные секретные ключи в репозитории.


## Установка и запуск проекта

### Предварительные требования
- Наличие установленного [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 
- Наличие установленной Docker (для запуска интеграционных тестов с использованием Testcontainers.PostgreSql и разворачивания PostgreSQL).
- 

### Инструкция по публикации и запуску

1. **Склонируйте репозиторий:**
   ```bash
   git clone https://github.com/ploshkaSharp/EventManagement
   ```

2. **Настройте строку подключения к БД:**
   В файле `appsettings.json` в разделе ConnectionStrings замените параметры подключения указанные по умолчанию (хост, порт, имя БД, логин, пароль), если они не совпадают.
   ```bash
   "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=111111"
   ```

3. **Переключитесь в папку с клонированным репозиторием:**
   ```bash
   cd EventManagement
   ```

4. **Опубликуйте решение:**
   ```bash
   dotnet build
   ```
   При первом запуске приложения миграции применяются автоматически (Program.cs):

   ```bash
   using (var scope = app.Services.CreateScope())
   {
     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
     db.Database.Migrate();
   }
   ```   

5. **Запустите решение:**
   ```bash
   dotnet run
   ```
6. **Для запуска тестов переключитесь в папку проекта тестов и запустите их:**

   Для запуска всех тестов:

   ```bash
   cd Tests
   dotnet test
   ```   

   Для запуска только тестов миграции:
   
   ```bash
   cd Tests
   dotnet test --filter "FullyQualifiedName~DatabaseSchemaTests"
   ```   

   Для запуска только интеграционных тестов мероприятий:
   
   ```bash
   cd Tests
   dotnet test --filter "FullyQualifiedName~EventRepositoryTests"
   ```
      
   Для запуска только интеграционных тестов бронирования:
   
   ```bash
   cd Tests
   dotnet test --filter "FullyQualifiedName~BookingRepositoryTests"
   ```   

7. **Для тестирования решения в swagger:**

   в браузере напишите адрес:
   http://localhost:5000/swagger

   Примечание. Порт swagger'а может отличаться от указанного здесь. Актальный порт указан в консоли запустившегося решения (см.п.5).

## Управление миграциями
   
### Создание новой миграции

```bash
dotnet ef migrations add <MigrationName> --context AppDbContext --output-dir EventManagement\Infrastructure\Migrations
```
### Применение миграций к базе данных
```bash
dotnet ef database update --context AppDbContext  --project EventManagement\Infrastructure --startup-project EventManagement\Presentation  
```

### Откат к предыдущей миграции
```bash
dotnet ef database update <PreviousMigrationName> --context AppDbContext
```

### Удаление последней миграции
```bash
dotnet ef migrations remove --context AppDbContext
```


## Реализованные методы

```bash

 Метод  │ URL               │ Описание                         │ HTTP Ответы                                                  |
 -------|-------------------|----------------------------------|--------------------------------------------------------------|
 GET    │ /events           │ Получить список всех мероприятий │ 200 OK                                                       |
        │                   │ с возможносью фильтрации по      │                                                              |
        │                   │ названию, дате старта, дате      │                                                              |
        │                   │ окончания и пагинации            │                                                              |
 GET    │ /events/{id}      │ Получить мероприятие по ID       │ 200 OK / 404 Not Found                                       |
 POST   │ /events           │ Создать мероприятие              │ 201 Created / 400 Bad Request                                |
 PUT    │ /events/{id}      │ Обновить мероприятие             │ 200 Ok / 404 Not Found / 400 Bad Request                     |
 DELETE │ /events/{id}      │ Удалить мероприятие              │ 204 No Content / 404 Not Found                               |
 GET    │ /bookings/{id}    │ Получить бронирование по ID      │ 200 OK / 404 Not Found                                       |
 POST   │ /events/{id}/book │ Создать бронь на мероприятие     │ 202 Accepted / 404 Not Found / 400 Bad Request / 409 Conflict|
 ```

### Примеры запросов
   **Создание мероприятия:**
   ```bash
   curl -X 'POST' \
     'http://localhost:5000/Events' \
     -H 'accept: application/json' \
     -H 'Content-Type: application/json' \
     -d '{
          "title": "Cобрание коллектива",
          "description": "По повестке дня",
          "startAt": "2026-04-01T10:00:00+04:00",
          "endAt": "2026-04-01T11:00:00+04:00",
          "totalSeats" : 50
        }'     
   ```

   **Вывод списка всех мероприятий:**
    Возможна фильтрация по названию (title), даты старта (from), даты окончания (to).
    Вывод результата осуществляется по страницам. Необходимо указать номер страницы (page), размер страницы (pageSize).
    
    Параметр title - проверяется на вхождение строки в наименование мероприятия.
    Параметр from - проводится поиск мероприятий начинающихся с этой даты. Формат даты в виде 2026-06-15T10:00:00+04:00
    Параметр to - проводится поиск мероприятий заканчивающихся в эту дату. Формат даты в виде 2026-06-17T10:00:00+04:00

    Параметр page - в параметр передается номер страницы получаемого результата. По умолчанию передается 1.
    Параметр pageSize - в параметр передается количество мероприятий на одной странице. По умолчанию передается 10.

   ```bash
   curl -X GET 'https://localhost:5000/Events?title=title&from=2026-06-15T10%3A00%3A00Z&to=2026-06-20T10%3A00%3A00Z&page=1&pageSize=10'  \
      -H 'accept: application/json'
   ```

   **Вывод мероприятия по ID (Guid):**
   ```bash
   curl -X GET 'https://localhost:5000/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
      -H 'accept: application/json'
   ```

  **Обновить мероприятие:**
   ```bash
   curl -X 'PUT' \
     'http://localhost:5000/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
     -H 'accept: application/json' \
     -H 'Content-Type: application/json' \
     -d '{
          "title": "Собрание актива",
          "startAt": "2026-04-01T10:00:00+04:00",
          "endAt": "2026-04-01T18:00:00+04:00"
        }'
   ```

   **Удалить мероприятия по ID (Guid):**
   ```bash
   curl -X 'DELETE' \
     'http://localhost:5000/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
     -H 'accept: */*'    
   ```

   **Создание брони:**
   ```bash
   curl -X 'POST' \
     'http://localhost:5000/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book' \
     -H 'accept: application/json' \
     -H 'Content-Type: application/json'    
   ```

   **Вывод бронирования по ID (Guid):**
   ```bash
   curl -X GET 'https://localhost:5000/Bookings/fd1c1927-dd18-4e08-bc6f-a5517290d729' \
      -H 'accept: application/json'
   ```   

## Формат ответа об ошибках:
   В случае ошибок ответ выводится в json-виде, в формате Problem Details ([RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807)).

**Пример ответа об ошибке:**

  ```bash
  {
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "Title is required.",
  "instance": "/Events",
  "errors": {},
  "traceId": "0HNKQ29I9RQNL:00000003"
}
 ``` 
