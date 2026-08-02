using CollegeLMS.API.Interfaces;
using CollegeLMS.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CollegeLMS.Tests.Unit.Services;

public class StvccHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_ReturnsAvailable_WhenSiteResponds()
    {
        var sut = CreateService((_, _) => Task.FromResult(new HttpResponseMessage()));
        var result = await sut.CheckAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Available.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ReturnsUnavailable_WhenConnectionFails()
    {
        var sut = CreateService((_, _) => throw new HttpRequestException("Сайт недоступен"));
        var result = await sut.CheckAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Available.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_ReturnsUnavailable_OnTimeout()
    {
        var sut = CreateService((_, _) => throw new TaskCanceledException());
        var result = await sut.CheckAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Available.Should().BeFalse();
    }

    private static StvccHealthService CreateService(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler
    )
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient("stvcc")
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpMessageHandler(handler));
        var provider = services.BuildServiceProvider();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["WordPress:BaseUrl"] = "http://stvcc.ru" }
            )
            .Build();

        return new StvccHealthService(provider.GetRequiredService<IHttpClientFactory>(), config);
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler
    ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => handler(request, cancellationToken);
    }
}
