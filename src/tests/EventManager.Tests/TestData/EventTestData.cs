using EventManager.Models;

namespace EventManager.Tests.TestData;

/// <summary>
/// Класс, предоставляющий тестовые данные для проверки функциональности EventService
/// </summary>
public static class EventTestData
{
    public static readonly DateTime FixedDate = new DateTime(2024, 12, 20, 0, 0, 0);
    
    /// <summary>
    /// Метод, возвращающий набор валидных событий для тестирования успешных сценариев
    /// </summary>
    public static IEnumerable<object[]> ValidEventsData()
    {
        return new List<object[]>
        {
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = "Конференция по IT", 
                    Description = "IT конференция",
                    StartDate = new DateTime(2024, 12, 25, 10, 0, 0),
                    EndDate = new DateTime(2024, 12, 25, 18, 0, 0)
                },
                "Конференция по IT",
                "IT конференция"
            },
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = "Встреча команды", 
                    Description = null,
                    StartDate = new DateTime(2024, 12, 26, 9, 0, 0),
                    EndDate = new DateTime(2024, 12, 26, 12, 0, 0)
                },
                "Встреча команды",
                null
            },
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = "Семинар по тестированию", 
                    Description = "Обучающий семинар",
                    StartDate = new DateTime(2024, 12, 27, 14, 0, 0),
                    EndDate = new DateTime(2024, 12, 27, 17, 0, 0)
                },
                "Семинар по тестированию",
                "Обучающий семинар"
            }
        };
    }

    /// <summary>
    /// Метод, возвращающий набор невалидных событий для тестирования сценариев с ошибками
    /// </summary>
    public static IEnumerable<object[]> InvalidEventsData()
    {
        return new List<object[]>
        {
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = "Некорректное событие",
                    StartDate = new DateTime(2024, 12, 25, 18, 0, 0),
                    EndDate = new DateTime(2024, 12, 25, 10, 0, 0)
                }
            },
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = "",
                    StartDate = FixedDate.AddDays(1),
                    EndDate = FixedDate.AddDays(2)
                }
            },
            new object[] 
            { 
                new EventSaveDto 
                { 
                    Title = null!,
                    StartDate = FixedDate.AddDays(1),
                    EndDate = FixedDate.AddDays(2)
                }
            }
        };
    }

    /// <summary>
    /// Метод, возвращающий список событий для тестирования фильтрации и пагинации
    /// </summary>
    public static List<EventSaveDto> GetTestEventsList()
    {
        return new List<EventSaveDto>
        {
            new EventSaveDto 
            { 
                Title = "Конференция по IT", 
                Description = "Крупная IT конференция",
                StartDate = FixedDate.AddDays(2),
                EndDate = FixedDate.AddDays(3)
            },
            new EventSaveDto 
            { 
                Title = "Конференция по дизайну", 
                Description = "Конференция для дизайнеров",
                StartDate = FixedDate.AddDays(3),
                EndDate = FixedDate.AddDays(4)
            },
            new EventSaveDto 
            { 
                Title = "Встреча команды", 
                Description = "Еженедельная встреча",
                StartDate = FixedDate.AddDays(1),
                EndDate = FixedDate.AddDays(1)
            },
            new EventSaveDto 
            { 
                Title = "Семинар по тестированию", 
                Description = "Практический семинар",
                StartDate = FixedDate.AddDays(4),
                EndDate = FixedDate.AddDays(5)
            },
            new EventSaveDto 
            { 
                Title = "Хакатон", 
                Description = "Соревнование разработчиков",
                StartDate = FixedDate.AddDays(5),
                EndDate = FixedDate.AddDays(6)
            }
        };
    }
}