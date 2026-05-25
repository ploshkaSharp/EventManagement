using Xunit;
using EventManagement.DTOs;
using EventManagement.Exceptions;

namespace EventManagement.Tests;

/// <summary>
/// Тесты для EventService 
/// </summary>
public class EventServiceTests
{
    #region Успешные сценарии

    /// <summary>
    /// Создание мероприятия с валидными данными
    /// </summary>
    [Fact]
    public void Create_WithValidData_ShouldCreateEvent()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();

        // Act
        var result = service.Create(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id.ToString());
        Assert.Equal(createDto.Title, result.Title);
        Assert.Equal(createDto.Description, result.Description);
        Assert.Equal(createDto.StartAt, result.StartAt);
        Assert.Equal(createDto.EndAt, result.EndAt);

        var created = service.GetById(result.Id);
        Assert.NotNull(created);
        Assert.Equal(createDto.Title, created?.Title);
    }

    /// <summary>
    /// Получить все мероприятия без фильтров
    /// </summary>
    [Fact]
    public void GetAll_WithoutFilters_ShouldReturnAllEvents()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();

        // Act
        var result = service.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count());
        var list = result.ToList();
        for (int i = 0; i < list.Count - 1; i++)
        {
            Assert.True(list[i].StartAt <= list[i + 1].StartAt);
        }
    }

    /// <summary>
    /// Получить существующее мероприятие по Guid
    /// </summary>
    [Fact]
    public void GetById_WithExistingId_ShouldReturnEvent()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = service.Create(createDto);

        // Act
        var result = service.GetById(createdEvent.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(createDto.Title, result.Title);
    }

    /// <summary>
    /// Обновить существующее мероприятие валидными данными
    /// </summary>
    [Fact]
    public void Update_WithValidData_ShouldUpdateEvent()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = service.Create(createDto);
        var updateDto = TestDataGenerator.GetValidUpdateEventDto();

        // Act
        var result = service.Update(createdEvent.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdEvent.Id, result.Id);
        Assert.Equal(updateDto.Title, result.Title);
        Assert.Equal(updateDto.Description, result.Description);
        Assert.Equal(updateDto.StartAt, result.StartAt);
        Assert.Equal(updateDto.EndAt, result.EndAt);

        var created = service.GetById(createdEvent.Id);
        Assert.NotNull(created);
        Assert.Equal(updateDto.Title, created?.Title);
    }


    /// <summary>
    /// Удалить существующее мероприятие по Guid
    /// </summary>
    [Fact]
    public void Delete_WithExistingId_ShouldDeleteEvent()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = service.Create(createDto);

        // Act
        var result = service.Delete(createdEvent.Id);

        // Assert
        Assert.True(result);

        var exception = Assert.Throws<NotFoundException>(() => service.GetById(createdEvent.Id));
        Assert.Contains(createdEvent.Id.ToString(), exception.Message);
    }

    /// <summary>
    /// Получить мероприятия по фильтру (названию)
    /// </summary>
    [Fact]
    public void GetAll_WithTitleFilter_ShouldReturnMatchingEvents()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            Title = "expo"
        };

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, e => e.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получить мероприятия по фильтру (дате начала)
    /// </summary>
    [Fact]
    public void GetAll_WithFromDateFilter_ShouldReturnEventsStartingAfterDate()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var fromDate = new DateTime(2030, 6, 1, 0, 0, 0, new TimeSpan(+4, 0, 0));
        var filter = new EventFilterDto
        {
            From = fromDate
        };

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e => Assert.True(e.StartAt >= fromDate));
    }

    /// <summary>
    /// Получить мероприятия по фильтру (дате окончания)
    /// </summary>
    [Fact]
    public void GetAll_WithToDateFilter_ShouldReturnEventsEndingBeforeDate()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var toDate = new DateTime(2030, 9, 1, 0, 0, 0, new TimeSpan(+4, 0, 0));
        var filter = new EventFilterDto
        {
            To = toDate
        };

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e => Assert.True(e.EndAt <= toDate));
    }

    /// <summary>
    /// Получить мероприятия по фильтрам (диапазон дата начала и дата конца)
    /// </summary>
    [Fact]
    public void GetAll_WithDateRangeFilter_ShouldReturnEventsInRange()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var localOffset = new TimeSpan(+4, 0, 0);
        var fromDate = new DateTime(2030, 1, 1, 0, 0, 0, localOffset);
        var toDate = new DateTime(2030, 7, 1, 0, 0, 0, localOffset);
        var filter = new EventFilterDto
        {
            From = fromDate,
            To = toDate
        };

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, e =>
        {
            Assert.True(e.StartAt >= fromDate);
            Assert.True(e.EndAt <= toDate);
        });

        Assert.Contains(result, e => e.Title == "HouseHold Expo 2030");
        Assert.Contains(result, e => e.Title == "Composit expo 2030");
        Assert.DoesNotContain(result, e => e.Title == "Wild Siberia Extreme Triathlon");
    }

    /// <summary>
    /// Получить мероприятия по страницам (первая страница из двух мероприятий)
    /// </summary>
    [Fact]
    public void GetPaginated_WithFirstPage_ShouldReturnFirstPage()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 2
        };

        // Act
        var result = service.GetPaginated(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    /// <summary>
    /// Получить мероприятия по страницам (вторая страница из двух мероприятий)
    /// </summary>
    [Fact]
    public void GetPaginated_WithSecondPage_ShouldReturnCorrectItems()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            PageNumber = 2,
            PageSize = 2
        };

        // Act
        var result = service.GetPaginated(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    /// <summary>
    /// Получить мероприятия по страницам (последняя страница из одного мероприятия)
    /// </summary>
    [Fact]
    public void GetPaginated_WithLastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            PageNumber = 3,
            PageSize = 2
        };

        // Act
        var result = service.GetPaginated(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(3, result.PageNumber);
    }

    /// <summary>
    /// Получить мероприятия с комбо фильтром (диапазон дат, наименование)
    /// </summary>
    [Fact]
    public void GetAll_WithCombinedFilters_ShouldReturnEventsMatchingAllConditions()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var localOffset = new TimeSpan(+4, 0, 0);
        var fromDate = new DateTime(2030, 1, 1, 0, 0, 0, localOffset);
        var toDate = new DateTime(2030, 6, 1, 0, 0, 0, localOffset);
        var filter = new EventFilterDto
        {
            Title = "expo",
            From = fromDate,
            To = toDate
        };

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, e =>
        {
            Assert.Contains(filter.Title, e.Title, StringComparison.OrdinalIgnoreCase);
            Assert.True(e.StartAt >= fromDate);
            Assert.True(e.EndAt <= toDate);
        });
        Assert.Equal("HouseHold Expo 2030", result.First().Title);
    }

    #endregion

    #region Неуспешные сценарии
    /// <summary>
    /// Получить мероприятие с несуществующим Guid
    /// </summary>
    [Fact]
    public void GetById_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var nonExistentId = Guid.NewGuid();

        // Act
        Action act = () => service.GetById(nonExistentId);

        // Assert
        var exception = Assert.Throws<NotFoundException>(act);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Обновить мероприятие c неуществующим Guid
    /// </summary>
    [Fact]
    public void Update_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var nonExistentId = Guid.NewGuid();
        var updateDto = TestDataGenerator.GetValidUpdateEventDto();

        // Act
        Action act = () => service.Update(nonExistentId, updateDto);

        // Assert
        var exception = Assert.Throws<NotFoundException>(act);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Удалить мероприятие c неуществующим Guid
    /// </summary>
    [Fact]
    public void Delete_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var nonExistentId = Guid.NewGuid();

        // Act
        Action act = () => service.Delete(nonExistentId);

        // Assert
        var exception = Assert.Throws<NotFoundException>(act);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    /// <summary>
    /// Создать мероприятие c пустым наименованием
    /// </summary>
    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = new CreateEventDTO
        {
            Title = string.Empty,
            Description = "Test",
            StartAt = DateTime.Now.AddDays(10),
            EndAt = DateTime.Now.AddDays(15)
        };

        // Act
        Action act = () => service.Create(createDto);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Title is required", exception.Message);

    }

    /// <summary>
    /// Создать мероприятие c null-наименованием
    /// </summary>
    [Fact]
    public void Create_WithNullTitle_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = new CreateEventDTO
        {
            Title = null!,
            Description = "Test",
            StartAt = DateTime.Now.AddDays(10),
            EndAt = DateTime.Now.AddDays(15)
        };

        // Act
        Action act = () => service.Create(createDto);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Title is required", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие c уже существующим наименованием
    /// </summary>
    [Fact]
    public void Create_WithDuplicateTitle_ShouldThrowValidationException()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var createDto = new CreateEventDTO
        {
            Title = "Composit expo 2030",
            Description = "Duplicate event",
            StartAt = DateTime.Now.AddDays(10),
            EndAt = DateTime.Now.AddDays(15)
        };

        // Act
        Action act = () => service.Create(createDto);

        // Assert
        var exception = Assert.Throws<ValidationException>(act);
        Assert.Contains("already exists", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие с датой начала в прошлом
    /// </summary>
    [Fact]
    public void Create_WithStartDateInPast_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = new CreateEventDTO
        {
            Title = "Past Event",
            Description = "Test",
            StartAt = DateTime.Now.AddDays(-1),
            EndAt = DateTime.Now.AddDays(1)
        };

        // Act
        Action act = () => service.Create(createDto);

        // Assert
        var exception = Assert.Throws<ValidationException>(act);
        Assert.Contains("StartAt must be more than now", exception.Message);
    }

    /// <summary>
    /// Создать мероприятие с датой начала старше, чем дата окончания
    /// </summary>
    [Fact]
    public void Create_WithEndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = new CreateEventDTO
        {
            Title = "Invalid Dates Event",
            Description = "Test",
            StartAt = DateTime.Now.AddDays(10),
            EndAt = DateTime.Now.AddDays(8)
        };

        // Act
        Action act = () => service.Create(createDto);

        // Assert
        var exception = Assert.Throws<ValidationException>(act);
        Assert.Contains("StartAt must be less than EndAt", exception.Message);
    }

    /// <summary>
    /// Обновить мероприятие датой начала старше, чем дата окончания
    /// </summary>
    [Fact]
    public void Update_WithEndDateBeforeStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = service.Create(createDto);

        var updateDto = new UpdateEventDTO
        {
            Title = "Updated Event",
            Description = "Test",
            StartAt = DateTime.Now.AddDays(12),
            EndAt = DateTime.Now.AddDays(10)
        };

        // Act
        Action act = () => service.Update(createdEvent.Id, updateDto);

        // Assert
        var exception = Assert.Throws<ValidationException>(act);
        Assert.Contains("StartAt must be less than EndAt", exception.Message);
    }

    /// <summary>
    /// Обновить мероприятие датой начала старше, чем дата окончания
    /// </summary>
    [Fact]
    public void Update_WithStartDateInPast_ShouldThrowArgumentException()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var createDto = TestDataGenerator.GetValidCreateEventDto();
        var createdEvent = service.Create(createDto);

        var updateDto = new UpdateEventDTO
        {
            Title = "Updated Event",
            Description = "Test",
            StartAt = DateTime.Now.AddDays(-1),
            EndAt = DateTime.Now.AddDays(1)
        };

        // Act
        Action act = () => service.Update(createdEvent.Id, updateDto);

        // Assert
        var exception = Assert.Throws<ValidationException>(act);
        Assert.Contains("StartAt must be more than now", exception.Message);
    }
    #endregion

    #region Пограничные сценарии

    /// <summary>
    /// Получить мероприятия с размером страницы равной "0"
    /// </summary>
    [Fact]
    public void GetPaginated_WithPageSizeLessThanOne_ShouldValidateToMinimum()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 0
        };

        // Act
        Action act = () => service.GetPaginated(filter);

        // Assert
        var exception = Assert.Throws<BadRequestException>(act);
        Assert.Contains("Page size must be greater than or equal to 1", exception.Message);
    }

    /// <summary>
    /// Получить страницу с мероприятиями меньшую чем "1"
    /// </summary>
    [Fact]
    public void GetPaginated_WithPageNumberLessThanOne_ShouldValidateToMinimum()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            PageNumber = 0,
            PageSize = 10
        };

        // Act
        Action act = () => service.GetPaginated(filter);

        // Assert
        var exception = Assert.Throws<BadRequestException>(act);
        Assert.Contains("Page number must be greater than or equal to 1", exception.Message);
    }

    /// <summary>
    /// Получить мероприятия с пустым фильтром
    /// </summary>
    [Fact]
    public void GetAll_WithEmptyFilter_ShouldReturnAllEvents()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto();

        // Act
        var result = service.GetAll(filter);

        // Assert
        Assert.Equal(5, result.Count());
    }

    /// <summary>
    /// Получить мероприятия где в наименовании все буквы в верхем регистре
    /// </summary>
    [Fact]
    public void GetAll_WithTitleFilterCaseInsensitive_ShouldReturnMatchingEvents()
    {
        // Arrange
        var service = TestDataGenerator.CreateEventServiceWithSeedData();
        var filter = new EventFilterDto
        {
            Title = "EXPO"
        };

        // Act
        var result = service.GetAll(filter);

        // Assert        
        Assert.Contains(result, e => e.Title == "HouseHold Expo 2030");
    }

    /// <summary>
    /// Получить пагинированный пустой список мероприятий
    /// </summary>
    [Fact]
    public void GetPaginated_WithZeroEvents_ShouldReturnEmptyResult()
    {
        // Arrange
        var service = TestDataGenerator.CreateFreshEventService();
        var filter = new EventFilterDto
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = service.GetPaginated(filter);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    #endregion
}