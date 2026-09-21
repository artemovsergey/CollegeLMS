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
/// Идемпотентный ежедневный дайджест ScheduleNotifier: гард выбора группы/преподавателя,
/// пропуск нерабочего дня, повторный прогон не дублирует отправку (last_notified_on в БД),
/// сбой API не помечает день отправленным.
/// </summary>
public class ScheduleNotifierTests
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

    private static readonly DateTime Now = new(2026, 9, 10, 7, 35, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private const string EmptyNonWorkingDays =
        """{"isSuccess":true,"data":{"items":[],"totalCount":0,"page":1,"pageSize":100}}""";

    private const string NonWorkingDay =
        """{"isSuccess":true,"data":{"items":[{"id":"11111111-1111-1111-1111-111111111111","dateFrom":"2026-09-10T00:00:00","dateTo":"2026-09-10T00:00:00","title":"День города"}],"totalCount":1,"page":1,"pageSize":100}}""";

    private const string DayWithPair =
        """{"isSuccess":true,"data":{"date":"2026-09-10T00:00:00","week":2,"dayOfWeek":4,"isSunday":false,"isNonWorking":false,"nonWorkingTitle":null,"practices":[],"inserts":[],"entries":[{"id":"22222222-2222-2222-2222-222222222222","groupId":"33333333-3333-3333-3333-333333333333","groupName":"ПО-262","teacherId":null,"teacherName":"Иванов И.И.","subject":"Математика","room":"301","dayOfWeek":4,"numberPair":2,"startTime":"10:00:00","endTime":"11:30:00","weeks":[2],"lessonType":"Lecture","changeTags":[]}]}}""";

    private const string DayWithPractice =
        """{"isSuccess":true,"data":{"date":"2026-09-10T00:00:00","week":2,"dayOfWeek":4,"isSunday":false,"isNonWorking":false,"nonWorkingTitle":null,"practices":[{"id":"44444444-4444-4444-4444-444444444444","kind":"Up","groupId":"33333333-3333-3333-3333-333333333333","groupName":"ПО-262","teacherId":"55555555-5555-5555-5555-555555555555","teacherName":"Марченко И.А.","dateFrom":"2026-09-07T00:00:00","dateTo":"2026-09-11T00:00:00","organization":"ООО Ромашка","note":null}],"inserts":[],"entries":[]}}""";

    private static UserSettings Subscriber() =>
        new()
        {
            Id = Guid.NewGuid(),
            MaxUserId = 42,
            MaxChatId = 4242,
            Role = "student",
            GroupId = Guid.NewGuid(),
            NotifyEnabled = true,
            NotifyDays = [4],
            NotifyTime = new TimeSpan(7, 30, 0),
        };

    private static ServiceProvider BuildProvider(
        CollegeLmsApiClient api,
        MaxApiClient max,
        UserSettings subscriber
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
        db.UserSettings.Add(subscriber);
        db.SaveChanges();
        return provider;
    }

    private static ScheduleNotifier BuildNotifier(ServiceProvider provider) =>
        new(provider, TimeZoneInfo.Utc, NullLogger<ScheduleNotifier>.Instance);

    private static async Task InvokeSendDueAsync(
        ScheduleNotifier notifier,
        List<UserSettings> subscribers
    )
    {
        var method = typeof(ScheduleNotifier).GetMethod(
            "SendDueNotificationsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        method.Should().NotBeNull();
        var task = (Task)
            method!.Invoke(notifier, [subscribers, Now, Today, CancellationToken.None])!;
        await task;
    }

    private static UserSettings? LoadSubscriber(ServiceProvider provider, Guid id)
    {
        using var scope = provider.CreateScope();
        return scope
            .ServiceProvider.GetRequiredService<MaxBotDbContext>()
            .UserSettings.AsNoTracking()
            .SingleOrDefault(x => x.Id == id);
    }

    private static List<UserSettings> LoadSubscribers(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        return scope
            .ServiceProvider.GetRequiredService<MaxBotDbContext>()
            .UserSettings.AsNoTracking()
            .ToList();
    }

    [Fact]
    public async Task SendDueNotifications_NoEntity_SkipsUser()
    {
        var sends = 0;
        var dayViewCalls = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
            {
                if (request.RequestUri!.PathAndQuery.Contains("view=day"))
                    dayViewCalls++;
                return Json(EmptyNonWorkingDays);
            })
        );
        var max = BuildMaxClient(() => sends++);
        var subscriber = Subscriber();
        subscriber.GroupId = null;
        subscriber.TeacherId = null;
        using var provider = BuildProvider(api, max, subscriber);

        await InvokeSendDueAsync(BuildNotifier(provider), [subscriber]);

        sends.Should().Be(0);
        dayViewCalls.Should().Be(0);
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().BeNull();
    }

    [Fact]
    public async Task SendDueNotifications_SendsOncePerDay()
    {
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
                Json(
                    request.RequestUri!.PathAndQuery.Contains("non-working-days")
                        ? EmptyNonWorkingDays
                        : DayWithPair
                )
            )
        );
        var max = BuildMaxClient(() => sends++);
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);
        var notifier = BuildNotifier(provider);

        await InvokeSendDueAsync(notifier, [subscriber]);

        sends.Should().Be(1);
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().Be(Today);

        await InvokeSendDueAsync(notifier, LoadSubscribers(provider));

        sends.Should().Be(1);
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().Be(Today);
    }

    [Fact]
    public async Task SendDueNotifications_NonWorkingDay_SendsNothing()
    {
        var sends = 0;
        var api = BuildApiClient(new StubHandler(_ => Json(NonWorkingDay)));
        var max = BuildMaxClient(() => sends++);
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);

        await InvokeSendDueAsync(BuildNotifier(provider), [subscriber]);

        sends.Should().Be(0);
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().BeNull();
    }

    [Fact]
    public async Task SendDueNotifications_ApiFailure_DoesNotMark()
    {
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError))
        );
        var max = BuildMaxClient(() => sends++);
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);

        await InvokeSendDueAsync(BuildNotifier(provider), [subscriber]);

        sends.Should().Be(0);
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().BeNull();
    }

    [Fact]
    public async Task SendDueNotifications_UsesDayViewLayers()
    {
        var sends = 0;
        string? sentText = null;
        var urls = new List<string>();
        var api = BuildApiClient(
            new StubHandler(request =>
            {
                var url = request.RequestUri!.PathAndQuery;
                urls.Add(url);
                return Json(
                    url.Contains("non-working-days") ? EmptyNonWorkingDays : DayWithPractice
                );
            })
        );
        var max = BuildMaxClient(() => sends++, body => sentText = body);
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);

        await InvokeSendDueAsync(BuildNotifier(provider), [subscriber]);

        sends.Should().Be(1);
        var dayViewUrl = urls.Single(u => u.Contains("view=day"));
        dayViewUrl.Should().Contain("view=day");
        dayViewUrl.Should().Contain($"date={Today:yyyy-MM-dd}");
        dayViewUrl.Should().Contain($"groupId={subscriber.GroupId}");
        sentText.Should().NotBeNull();
        using var sentJson = JsonDocument.Parse(sentText!);
        var text = sentJson.RootElement.GetProperty("text").GetString();
        text.Should().Contain("Практика").And.Contain("ПО-262").And.Contain("Марченко");
        LoadSubscriber(provider, subscriber.Id)!.LastNotifiedOn.Should().Be(Today);
    }
}
