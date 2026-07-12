using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Dtos;
using CollegeLMS.API.Response;
using FluentAssertions;

namespace CollegeLMS.Tests.Integration.Controllers;

public class FeedbackControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task Create_Returns200_WithValidRequest()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван Иванов",
            Email = "ivan@test.ru",
            Message = "Отличный сайт!",
        };

        var response = await Client.PostAsJsonAsync("/api/feedback", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FeedbackResponse>>();
        result!.IsSuccess.Should().BeTrue();
        result.Data!.Message.Should().Be("Сообщение отправлено");
    }

    [Fact]
    public async Task Create_Returns400_WhenEmailInvalid()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "не-email",
            Message = "Тест",
        };

        var response = await Client.PostAsJsonAsync("/api/feedback", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Returns429_WhenTooFrequent()
    {
        var request = new FeedbackRequest
        {
            Name = "Иван",
            Email = "ivan@test.ru",
            Message = "Первое сообщение",
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/feedback", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await Client.PostAsJsonAsync("/api/feedback", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
