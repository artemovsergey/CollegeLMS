using System.Net;
using System.Reflection;
using System.Text;
using CollegeLMS.MaxBot.Clients;
using CollegeLMS.MaxBot.Data;
using CollegeLMS.MaxBot.Models;
using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CollegeLMS.MaxBot.Tests;

/// <summary>Поведение ScheduleNotifier на нерабочий день.</summary>
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

    private static readonly DateTime Now = new(2026, 9, 10, 7, 35, 0);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

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
        services.AddDbContext<MaxBotDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );
        services.AddSingleton(api);
        services.AddSingleton(max);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MaxBotDbContext>();
        db.UserSettings.Add(subscriber);
        db.SaveChanges();
        return provider;
    }

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

    [Fact]
    public async Task SendDueNotifications_NonWorkingDay_SendsNothingAndMarksSent()
    {
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(_ =>
            {
                var body =
                    """{"isSuccess":true,"data":{"items":[{"id":"11111111-1111-1111-1111-111111111111","dateFrom":"2026-09-10T00:00:00","dateTo":"2026-09-10T00:00:00","title":"День города"}],"totalCount":1,"page":1,"pageSize":100}}""";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
            })
        );
        var max = new MaxApiClient(
            new HttpClient(
                new StubHandler(_ =>
                {
                    sends++;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                    };
                })
            )
            {
                BaseAddress = new Uri("https://max.unit.test"),
            },
            NullLogger<MaxApiClient>.Instance
        );
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);
        var notifier = new ScheduleNotifier(
            provider,
            TimeZoneInfo.Utc,
            NullLogger<ScheduleNotifier>.Instance
        );

        await InvokeSendDueAsync(notifier, [subscriber]);

        sends.Should().Be(0);
    }

    [Fact]
    public async Task SendDueNotifications_NoNonWorkingDay_SendsDigest()
    {
        var sends = 0;
        var api = BuildApiClient(
            new StubHandler(request =>
            {
                string body;
                if (request.RequestUri!.AbsolutePath.Contains("non-working-days"))
                {
                    body =
                        """{"isSuccess":true,"data":{"items":[],"totalCount":0,"page":1,"pageSize":100}}""";
                }
                else
                {
                    body =
                        """{"isSuccess":true,"data":{"items":[{"id":"22222222-2222-2222-2222-222222222222","groupId":"33333333-3333-3333-3333-333333333333","groupName":"ПО-262","subject":"Математика","room":"301","dayOfWeek":4,"numberPair":2,"startTime":"10:00:00","endTime":"11:30:00","weeks":[2],"lessonType":"Lecture","changeTags":[]}],"totalCount":1,"page":1,"pageSize":20}}""";
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
            })
        );
        var max = new MaxApiClient(
            new HttpClient(
                new StubHandler(_ =>
                {
                    sends++;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                    };
                })
            )
            {
                BaseAddress = new Uri("https://max.unit.test"),
            },
            NullLogger<MaxApiClient>.Instance
        );
        var subscriber = Subscriber();
        using var provider = BuildProvider(api, max, subscriber);
        var notifier = new ScheduleNotifier(
            provider,
            TimeZoneInfo.Utc,
            NullLogger<ScheduleNotifier>.Instance
        );

        await InvokeSendDueAsync(notifier, [subscriber]);

        sends.Should().Be(1);
    }
}
