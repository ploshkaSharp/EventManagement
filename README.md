# Event Management
REST API для управления мероприятиями и их бронированием с CRUD-операциями (создание, просмотр, обновление, удаление).

Хранение мероприятий в памяти (in-memory).

Валидация входных данных (обязательность заполнения, дата окончания должна быть позже даты начала).

Интерфейсы для изоляции бизнес-логики (`IEventService`, `IBookingService`).

Swagger для упрощения тестирования и документирования.


## Cтек разработки

- **.NET 10 (ASP.NET Core Web API, C#)**
- **Swashbuckle.AspNetCore** (Swagger)
- **xUnit**

## Структура проекта

```bash
EventManagement/
├── Controllers/
│ ├── BookingController.cs        #Эндпоинты API (бронирование)
│ └── EventsController.cs         #Эндпоинты API (мероприятия)
├── DTO/
│ ├── BookingDTO.cs               #DTO объекты (бронирование)
│ ├── EventDTO.cs                 #DTO объекты (мероприятия)
│ ├── EventFilterDTO.cs           #DTO для параметров фильтрации
│ └── PaginateResultDTO.cs        #DTO для пагинированного результата
├── Exceptions/
│ ├── BadRequestException.cs      #Исключение - Некорректный запрос
│ ├── NotFoundException.cs        #Исключение - Ресурс не найден
│ └── ValidationException.cs      #Исключение - Ошибка валидации
├── Mappers/
│ ├── BookingMapper.cs            #Маппинг DTO объектов (бронирование)
│ └── EventMapper.cs              #Маппинг DTO объектов (мероприятия)
├── Middleware/
│ └── GlobalExceptionHandlingMiddleware.cs  #Глобальная обработка исключений (middleware)
├── Models/
│ ├── Booking.cs                  #Модель бронирования мероприятия
│ ├── BookingStatus.cs            #Перечисление статусов бронирования 
│ ├── ErrorResponse.cs            #Модель ответа об ошибке в формате Problem Details (RFC 7807)
│ └── Event.cs                    #Доменная модель (сущность)
├── Services/
│ ├── BookingBackgroundService.cs #Фоновый сервис для обработки бронирований
│ ├── BookingService.cs           #Реализация бизнес-логики управления бронированиями
│ ├── EventService.cs             #Реализация бизнес-логики управления мероприятиями
│ ├── IBookingService.cs          #Интерфейс сервиса управления бронированием
│ └── IEventService.cs            #Интерфейс сервиса управления мероприятиями
├── Tests/
│ ├── Data/
│ │   └── DataGenerator.cs        #Генератор тестовых данных
│ └── Services/
│ │   ├── BookingServiceTest.cs   #Тестовые сценарии для бронирования
│ │   └── EventServiceTest.cs     #Тестовые сценарии (успешные, неуспешные, пограничные)
│ └── Tests.csproj                #Проект с тестами
├── Program.cs                    #Точка входа в приложение с конфигурацией DI
├── appsettings.json              #Настройки приложения
└── appsettings.Development.json  #Настройки приложения (окружение разработчика)
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

  /// Дата и время создания брони (UTC)
  CreatedAt

  /// Дата и время обработки брони (UTC) (необязательное)
  ProcessedAt
```
### Логика фоновой обработки бронирования
После успешного создания брони для мероприятия, брони присваивается уникальный ИД. Статус бронирования устанвливается равным Ожидание (`Pending`). Устанавлвается текущее время создания брони.

Фоновый сервис с периодом 5 секунд собирает вновь созданные брони. Для каждой новой брони имитируется обработка бронирования (задержка 2 сек). После чего брони устанавливается статус равный Подтверждено (`Confirmed`), устанавливается время последней обработки брони (`ProcessedAt`) равное текущему.


## Установка и запуск проекта

### Предварительные требования
- Необходимо наличие установленного [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 

### Инструкция по публикации и запуску

1. **Склонируйте репозиторий:**
   ```bash
   git clone https://github.com/ploshkaSharp/EventManagement
   ```
   
2. **Переключитесь в папку с клонированным репозиторием:**
   ```bash
   cd EventManagement
   ```

3. **Опубликуйте решение:**
   ```bash
   dotnet build
   ```

4. **Запустите решение:**
   ```bash
   dotnet run
   ```

5. **Для запуска тестов переключитесь в папку проекта тестов и запустите тесты:**

   ```bash
   cd Tests
   dotnet test
   ```   

6. **Для тестирования решения в swagger:**

   в браузере напишите адрес:
   http://localhost:5000/swagger

   Примечание. Порт swagger'а может отличаться от указанного здесь. Актальный порт указан в консоли запустившегося решения (см.п.4).

## Реализованные методы

```bash

 Метод  │ URL               │ Описание                         │ HTTP Ответы                                    |
 -------|-------------------|----------------------------------|------------------------------------------------|
 GET    │ /events           │ Получить список всех мероприятий │ 200 OK                                         |
        │                   │ с возможносью фильтрации по      │                                                |
        │                   │ названию, дате старта, дате      │                                                |
        │                   │ окончания и пагинации            │                                                |
 GET    │ /events/{id}      │ Получить мероприятие по ID       │ 200 OK / 404 Not Found                         |
 POST   │ /events           │ Создать мероприятие              │ 201 Created / 400 Bad Request                  |
 PUT    │ /events/{id}      │ Обновить мероприятие             │ 200 Ok / 404 Not Found / 400 Bad Request       |
 DELETE │ /events/{id}      │ Удалить мероприятие              │ 204 No Content / 404 Not Found                 |
 GET    │ /bookings/{id}    │ Получить бронирование по ID      │ 200 OK / 404 Not Found                         |
 POST   │ /events/{id}/book │ Создать бронь на мероприятие     │ 202 Accepted / 404 Not Found / 400 Bad Request |
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
          "startAt": "2026-04-01T10:00:00Z",
          "endAt": "2026-04-01T11:00:00Z"
        }'     
   ```

   **Вывод списка всех мероприятий:**
    Возможна фильтрация по названию (title), даты старта (from), даты окончания (to).
    Вывод результата осуществляется по страницам. Необходимо указать номер страницы (page), размер страницы (pageSize).
    
    Параметр title - проверяется на вхождение строки в наименование мероприятия.
    Параметр from - проводится поиск мероприятий начинающихся с этой даты. Формат даты UTC, напр. 2026-06-15T10:00:00Z
    Параметр to - проводится поиск мероприятий заканчивающихся в эту дату. Формат даты UTC, напр. 2026-06-17T10:00:00Z

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
          "startAt": "2026-04-01T10:00:00Z",
          "endAt": "2026-04-01T18:00:00Z"
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
