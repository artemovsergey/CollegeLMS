using CollegeLMS.API.Dtos;
using CollegeLMS.API.Entities.Enums;
using CollegeLMS.API.Response;

namespace CollegeLMS.API.SwaggerExamples;

/// <summary>Пример успешного ответа вида «День» (view=day): две пары, событие и бейдж «Добавлено».</summary>
public static class ScheduleDayViewExample
{
    public static Result<ScheduleDayViewResponse> Create() =>
        Result<ScheduleDayViewResponse>.Ok(
            new ScheduleDayViewResponse
            {
                Date = new DateTime(2026, 9, 1),
                Week = 1,
                DayOfWeek = (int)DayOfWeek.Tuesday,
                IsSunday = false,
                IsNonWorking = false,
                Practices = [],
                Inserts =
                [
                    new ScheduleInsertResponse
                    {
                        Id = Guid.NewGuid(),
                        Title = "Разговор о важном",
                        DayOfWeek = (int)DayOfWeek.Tuesday,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(8, 30, 0),
                        IsActive = true,
                    },
                ],
                Entries =
                [
                    new ScheduleResponse
                    {
                        Id = Guid.NewGuid(),
                        GroupId = Guid.NewGuid(),
                        GroupName = "ИСП-31",
                        TeacherId = Guid.NewGuid(),
                        TeacherName = "Иванов Иван Иванович",
                        Subject = "Основы программирования",
                        Room = "301",
                        DayOfWeek = (int)DayOfWeek.Tuesday,
                        NumberPair = 1,
                        StartTime = new TimeSpan(8, 30, 0),
                        EndTime = new TimeSpan(10, 0, 0),
                        Weeks = [1],
                        LessonType = "Lecture",
                        ChangeTags =
                        [
                            new ChangeTag { ChangeType = ScheduleChangeType.Add, Week = 1 },
                        ],
                    },
                    new ScheduleResponse
                    {
                        Id = Guid.NewGuid(),
                        GroupId = Guid.NewGuid(),
                        GroupName = "ИСП-31",
                        TeacherId = Guid.NewGuid(),
                        TeacherName = "Петрова Мария Сергеевна",
                        Subject = "Математика",
                        Room = "302",
                        DayOfWeek = (int)DayOfWeek.Tuesday,
                        NumberPair = 2,
                        StartTime = new TimeSpan(10, 10, 0),
                        EndTime = new TimeSpan(11, 40, 0),
                        Weeks = [1],
                        LessonType = "Practice",
                    },
                ],
            }
        );
}
