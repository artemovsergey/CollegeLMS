using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>
/// Уведомления PracticeNotifier о начале/окончании практик: получатели — группа и
/// преподаватели, один чат — одно сообщение, окно NotifyTime, отправка и в нерабочий день,
/// идемпотентность через practice_notifications, терпимость к legacy-контракту API.
/// </summary>
public class PracticeNotifierTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(respond(request));
    }

    private static CollegeLmsApiClient BuildApiClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.unit.test") },
            NullLogger<CollegeLmsApiClient>.Instance
        );

    private static MaxApiClient BuildMaxClient(Action onSend, Action<string>? onBody = null) =>
        new(
            new HttpClient(
                new StubHandler(request =>
                {
                    onSend();
                    if (onBody is not null)
                        onBody(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                    return Json("{}");
                })
            )
            {
                BaseAddress = new Uri("https://max.unit.test"),
            },
            NullLogger<MaxApiClient>.Instance
        );

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    /// <summary>Извлекает поле text из JSON-тела POST /messages.</summary>
    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("text").GetString() ?? "";
    }

    private static readonly DateTime Now = new(2026, 9, 10, 7, 35, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private const string EmptyNonWorkingDays =
        """{"isSuccess":true,"data":{"items":[],"totalCount":0,"page":1,"pageSize":100}}""";

    private const string NonWorkingDay =
        """{"isSuccess":true,"data":{"items":[{"id":"11111111-1111-1111-1111-111111111111","dateFrom":"2026-09-10T00:00:00","dateTo":"2026-09-10T00:00:00","title":"День города"}],"totalCount":1,"page":1,"pageSize":100}}""";

    /// <summary>Практика нового контракта: name + teacherIds + teachers.</summary>
    private static string PracticesJson(
        Guid practiceId,
        string name,
        Guid groupId,
        Guid teacherId,
        DateTime dateFrom,
        DateTime dateTo
    ) =>
        $$$"""{"isSuccess":true,"data":{"items":[{"id":"{{{practiceId}}}","kind":"Up","name":"{{{name}}}","groupId":"{{{groupId}}}","groupName":"ПО-262","teacherIds":["{{{teacherId}}}"],"teachers":[{"id":"{{{teacherId}}}","fullName":"Марченко И.А."}],"dateFrom":"{{{dateFrom:yyyy-MM-dd}}}T00:00:00","dateTo":"{{{dateTo:yyyy-MM-dd}}}T00:00:00","note":null}],"totalCount":1,"page":1,"pageSize":100}}""";

    /// <summary>Практика legacy-контракта: teacherId/teacherName/organization, без name/teachers.</summary>
    private static string LegacyPracticeJson(
        Guid practiceId,
        Guid groupId,
        Guid teacherId,
        DateTime dateFrom,
        DateTime dateTo
    ) =>
        $$$"""{"isSuccess":true,"data":{"items":[{"id":"{{{practiceId}}}","kind":"Pp","groupId":"{{{groupId}}}","groupName":"ПО-262","teacherId":"{{{teacherId}}}","teacherName":"Петренко В.Б.","dateFrom":"{{{dateFrom:yyyy-MM-dd}}}T00:00:00","dateTo":"{{{dateTo:yyyy-MM-dd}}}T00:00:00","organization":"ООО Ромашка"}],"totalCount":1,"page":1,"pageSize":100}}""";

    private static UserSettings StudentSubscriber(Guid groupId, long chatId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = chatId + 1,
            MaxChatId = chatId,
            Role = "student",
            GroupId = groupId,
            NotifyEnabled = true,
            NotifyTime = new TimeSpan(7, 30, 0),
        };

    private static UserSettings TeacherSubscriber(Guid teacherId, long chatId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = chatId + 1,
            MaxChatId = chatId,
            Role = "teacher",
            TeacherId = teacherId,
            NotifyEnabled = true,
            NotifyTime = new TimeSpan(7, 30, 0),
        };

    private static ServiceProvider BuildProvider(
        CollegeLmsApiClient api,
        MaxApiClient max,
        params UserSettings[] subscribers
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<MaxBotDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddSingleton(api);
        services.AddSingleton(max);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        db.UserSettings.AddRange(subscribers);
        db.SaveChanges();
        return provider;
    }

    private static PracticeNotifier BuildNotifier(ServiceProvider provider) =>
        new(provider, TimeZoneInfo.Utc, NullLogger<PracticeNotifier>.Instance);

    private static async Task InvokeSendDueAsync(
        PracticeNotifier notifier,
        List<UserSettings> subscribers
    )
    {
        var method = typeof(PracticeNotifier).GetMethod(
            "SendDuePracticesAsync",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        method.Should().NotBeNull();
        var task = (Task)
            method!.Invoke(notifier, [subscribers, Now, Today, CancellationToken.None])!;
        await task;
    }

    private static List<PracticeNotification> LoadNotifications(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope
            .ServiceProvider.GetRequiredService<MaxBotDbContext>()
            .PracticeNotifications.AsNoTracking()
            .ToList();
    }

    [Fact]
    public async Task SendDuePractices_StartDay_NotifiesGroupAndTeacher()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var texts = new List<string>();
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++, body => texts.Add(ExtractText(body)));
        var student = StudentSubscriber(groupId, 100);
        var teacher = TeacherSubscriber(teacherId, 200);
        using var provider = BuildProvider(api, max, student, teacher);

        await InvokeSendDueAsync(BuildNotifier(provider), [student, teacher]);

        sends.Should().Be(2);
        var notifications = LoadNotifications(provider);
        notifications.Should().ContainSingle();
        notifications[0].PracticeId.Should().Be(practiceId);
        notifications[0].Event.Should().Be(PracticeEvent.Started);
        notifications[0].SentOn.Should().Be(Today);
        texts
            .Should()
            .AllSatisfy(t => t.Should().Contain("Началась практика").And.Contain("УП 01"));
    }

    [Fact]
    public async Task SendDuePractices_FinishDay_NotifiesFinished()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var texts = new List<string>();
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "ПП 09",
                            groupId,
                            teacherId,
                            Today.AddDays(-4).ToDateTime(TimeOnly.MinValue),
                            Today.ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++, body => texts.Add(ExtractText(body)));
        var student = StudentSubscriber(groupId, 100);
        using var provider = BuildProvider(api, max, student);

        await InvokeSendDueAsync(BuildNotifier(provider), [student]);

        sends.Should().Be(1);
        var notifications = LoadNotifications(provider);
        notifications.Should().ContainSingle();
        notifications[0].Event.Should().Be(PracticeEvent.Finished);
        texts[0].Should().Contain("Закончилась практика").And.Contain("ПП 09");
    }

    [Fact]
    public async Task SendDuePractices_SingleDayPractice_SendsBothEvents()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 02",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var student = StudentSubscriber(groupId, 100);
        using var provider = BuildProvider(api, max, student);

        await InvokeSendDueAsync(BuildNotifier(provider), [student]);

        sends.Should().Be(2);
        LoadNotifications(provider).Should().HaveCount(2);
    }

    [Fact]
    public async Task SendDuePractices_SecondRun_DoesNotDuplicate()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var student = StudentSubscriber(groupId, 100);
        var teacher = TeacherSubscriber(teacherId, 200);
        using var provider = BuildProvider(api, max, student, teacher);
        var notifier = BuildNotifier(provider);

        await InvokeSendDueAsync(notifier, [student, teacher]);
        await InvokeSendDueAsync(notifier, [student, teacher]);

        sends.Should().Be(2);
        LoadNotifications(provider).Should().ContainSingle();
    }

    [Fact]
    public async Task SendDuePractices_NotifyDisabled_SendsNothing()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var student = StudentSubscriber(groupId, 100);
        student.NotifyEnabled = false;
        using var provider = BuildProvider(api, max, student);

        await InvokeSendDueAsync(BuildNotifier(provider), [student]);

        sends.Should().Be(0);
        LoadNotifications(provider).Should().BeEmpty();
    }

    [Fact]
    public async Task SendDuePractices_NonWorkingDay_StillNotifies()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? NonWorkingDay
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var student = StudentSubscriber(groupId, 100);
        using var provider = BuildProvider(api, max, student);

        await InvokeSendDueAsync(BuildNotifier(provider), [student]);

        sends.Should().Be(1);
        LoadNotifications(provider).Should().ContainSingle();
    }

    [Fact]
    public async Task SendDuePractices_NotDueWindow_SendsNothing()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var student = StudentSubscriber(groupId, 100);
        student.NotifyTime = new TimeSpan(9, 0, 0);
        using var provider = BuildProvider(api, max, student);

        await InvokeSendDueAsync(BuildNotifier(provider), [student]);

        sends.Should().Be(0);
        LoadNotifications(provider).Should().BeEmpty();
    }

    [Fact]
    public async Task SendDuePractices_DeduplicatesSameChat()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var first = StudentSubscriber(groupId, 100);
        var second = StudentSubscriber(groupId, 100);
        var teacher = TeacherSubscriber(teacherId, 200);
        using var provider = BuildProvider(api, max, first, second, teacher);

        await InvokeSendDueAsync(BuildNotifier(provider), [first, second, teacher]);

        // chat 100 (два подписчика) + chat 200 (преподаватель) = 2 сообщения.
        sends.Should().Be(2);
    }

    [Fact]
    public async Task SendDuePractices_OtherGroupAndTeacher_Ignored()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : PracticesJson(
                            practiceId,
                            "УП 01",
                            groupId,
                            teacherId,
                            Today.ToDateTime(TimeOnly.MinValue),
                            Today.AddDays(4).ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var stranger = StudentSubscriber(Guid.NewGuid(), 100);
        var otherTeacher = TeacherSubscriber(Guid.NewGuid(), 200);
        using var provider = BuildProvider(api, max, stranger, otherTeacher);

        await InvokeSendDueAsync(BuildNotifier(provider), [stranger, otherTeacher]);

        sends.Should().Be(0);
        LoadNotifications(provider).Should().BeEmpty();
    }

    [Fact]
    public async Task SendDuePractices_LegacyContract_StillNotifiesTeachers()
    {
        var groupId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var sends = 0;
        var texts = new List<string>();
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : LegacyPracticeJson(
                            practiceId,
                            groupId,
                            teacherId,
                            Today.AddDays(-4).ToDateTime(TimeOnly.MinValue),
                            Today.ToDateTime(TimeOnly.MinValue)
                        )
                )
            )
        );
        var max = BuildMaxClient(() => sends++, body => texts.Add(ExtractText(body)));
        var student = StudentSubscriber(groupId, 100);
        var teacher = TeacherSubscriber(teacherId, 200);
        using var provider = BuildProvider(api, max, student, teacher);

        await InvokeSendDueAsync(BuildNotifier(provider), [student, teacher]);

        sends.Should().Be(2);
        texts[0].Should().Contain("Петренко В.Б.");
        // Организация в уведомлении не выводится, даже если пришла от API.
        texts.Should().AllSatisfy(t => t.Should().NotContain("ООО Ромашка"));
    }
}
