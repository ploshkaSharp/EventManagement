# Event Management
REST API для управления мероприятиями с CRUD-операциями (создание, просмотр, обновление, удаление).

Хранение мерприятий в памяти (in-memory).

Валидация входных данных (обязательность заполнения, дата окончания должна быть позже даты начала).

Интерфейс для изоляции бизнес-логики (`IEventService`).

Swagger для упрощения тестирования и документирования.


## Cтек разработки

- **.NET 10 (ASP.NET Core Web API, C#)**
- **Swashbuckle.AspNetCore** (Swagger)

## Структура проекта
EventManagement/

├── Controllers/

│ └── EventsController.cs  *#Эндпоинты API*

├── DTO/

│ └── EventDTO.cs  *#DTO объекты с валидацией*

├── Mappers/

│ └── EventMapper.cs  *#Маппинг DTO объктов*

├── Models/

│ └── Event.cs  *#Доменная модель (сущность)*

├── Services/

│ ├── EventService.cs  *#Реализация бизнес-логики*

│ └── IEventService.cs  *#Интерфейс сервиса*

├── Program.cs  *#Точка входа в приложение с конфигурацией DI*

└── appsettings.json  *#Настройки приложения*

└── appsettings.Development.json  *#Настройки приложения (окружение разработчика)*

## Установка и запуск проекта

### Предварительные требования
- Необходимо наличие установленного [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 

### Инструкция по публикации и запуску

1. **Склонируйте репозиторий:**
   ```bash
   git clone https://github.com/ploshkaSharp/EventManagement
   
2. **Переключитесь в папку с клонированным репозиторием:**
   ```bash
   cd EventManagement

3. **Опубликуйте решение:**
   ```bash
   dotnet build

4. **Запустите решение:**
   ```bash
   dotnet run

5. **Для тестирования решения откройте swagger:**

   в браузере напишите адрес:
   http://localhost:5000/swagger

   Примечание. Порт swagger'а может отличаться от указанного здесь. Актальный порт указан в консоли запустившегося решения (см.п.4).

## Реализованные методы

 Метод  │ URL          │ Описание                         │ HTTP Ответы
───────────────────────────────────────────────────────────────────────────────────────

 GET    │ /events      │ Получить список всех мероприятий │ 200 OK
 
 GET    │ /events/{id} │ Получить мероприятие по ID       │ 200 OK / 404 Not Found
 
 POST   │ /events      │ Создать мероприятие              │ 201 Created / 400 Bad Request
 
 PUT    │ /events/{id} │ Обновить мероприятие             │ 204 No Content / 404 Not Found / 400 Bad Request
 
 DELETE │ /events/{id} │ Удалить мероприятие              │ 204 No Content / 404 Not Found

### Примеры запросов
   **Создание мероприятия:**
   ```bash
   curl -X 'POST' \
     'http://localhost:5000/api/Events' \
     -H 'accept: text/plain' \
     -H 'Content-Type: application/json' \
     -d '{
          "title": "Cобрание коллектива",
          "description": "По повестке дня",
          "startAt": "2026-04-01T10:00:00Z",
          "endAt": "2026-04-01T11:00:00Z"
        }'     
   ```

   **Вывод списка всех мероприятий:**
   ```bash
   curl -X GET 'https://localhost:5000/api/Events'  \
      -H 'accept: text/plain'
   ```

   **Вывод мероприятия по ID (Guid):**
   ```bash
   curl -X GET 'https://localhost:5000/api/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
      -H 'accept: text/plain'
   ```

  **Обновить мероприятие:**
   ```bash
   curl -X 'PUT' \
     'http://localhost:5000/api/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
     -H 'accept: text/plain' \
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
     'http://localhost:5000/api/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6' \
     -H 'accept: */*'    
   ```   
