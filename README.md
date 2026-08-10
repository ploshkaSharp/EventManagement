# Event Management
Система управления мероприятиями, построенная на микросервисной архитектуре с асинхронным обменом через Apache Kafka.

Swagger для упрощения тестирования и документирования.

Используется Entity Framework Core Migrations для управления схемой БД. Все изменения структуры базы данных выполняются через миграции.

## Архитектура

### Сервисы

1. **Users/Auth** (порт 5001)
   - Регистрация и аутентификация пользователей
   - Выдача JWT-токенов
   - Управление ролями (User/Admin)
   - База данных: users_db (PostgreSQL)

2. **Events** (порт 5002)
   - CRUD операции с мероприятиями
   - Учёт доступных мест
   - Подписка на события бронирования через Kafka
   - База данных: events_db (PostgreSQL)

3. **Bookings** (порт 5003)
   - Создание и отмена броней
   - Публикация событий подтверждения брони
   - Проверка лимитов активных броней
   - База данных: bookings_db (PostgreSQL)

### Обмен сообщениями

- **Топик**: `booking-confirmed`
- **Издатель**: Сервис Bookings (при подтверждении брони)
- **Подписчик**: Сервис Events (уменьшает доступные места)
- **Ключ сообщения**: EventId (обеспечивает порядок обработки)


## Cтек разработки

- **.NET 10 (ASP.NET Core Web API, EF Core, C#)**
- **PostgreSQL**
- **Swashbuckle.AspNetCore** (Swagger)
- **xUnit**

## Архитектура и структура проекта

Проект построен на принципах Clean Architecture с разделением на четыре слоя в каждом сервисе:

1. Domain 
   - Доменные сущности (Event, Booking, User)
   - Перечисления 
   - Доменные исключения
   - Не зависит от внешних библиотек

2. Application 
   - Use Cases 
   - DTO
   - Интерфейсы портов (репозитории)
   - Фоновые сервисы
   - Зависит только от Domain

3. Infrastructure 
   - Реализации репозиториев
   - DbContext и конфигурации
   - Миграции
   - Зависит от Application и Domain

4. Presentation 
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
- Наличие установленной Docker и Docker Compose.

### Инструкция по публикации и запуску

1. **Склонируйте репозиторий:**
   ```bash
   git clone https://github.com/ploshkaSharp/EventManagement
   ```

2. **Запуск всей системы**
  ```bash
  docker-compose up -d
  ```

3. **Остановка системы**
  ```bash
  docker-compose down
  ```

4. **Остановка с удалением томов**
  ```bash
  docker-compose down -v
  ```
5. **Для тестирования решения в swagger:**

   в браузере напишите адрес:
   http://localhost:5001/swagger - для сервиса Users
   http://localhost:5002/swagger - для сервиса Events
   http://localhost:5003/swagger - для сервиса Bookings


## Управление миграциями
   
### Создание новой миграции

```bash
dotnet ef migrations add <MigrationName> --context AppDbContext --output-dir EventManagement\Infrastructure\Migrations
dotnet ef migrations add Users --context AppDbContext --startup-project D:\Teach\EventManagement\Services\Users\Presentation --project D:\Teach\EventManagement\Services\Users\Infrastructure --output-dir D:\Teach\EventManagement\Services\Users\Infrastructure\Migrations  
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
