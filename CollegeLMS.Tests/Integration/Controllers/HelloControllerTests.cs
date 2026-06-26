using System.Net;
using System.Net.Http.Json;
using CollegeLMS.API.Response;
using CollegeLMS.Tests.Integration;

namespace CollegeLMS.Tests.Integration.Controllers;

public class HelloControllerTests : BaseIntegrationTest
{
    [Fact]
    public async Task SayHello_ReturnsOk_WithHelloWorld()
    {
        var response = await Client.GetAsync("/api/hello");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeAsync<Result<string>>(response);
        Assert.NotNull(result);
        Assert.True(result!.IsSuccess, $"Expected IsSuccess=true, got body: {body}");
        Assert.Equal("Hello, World!", result.Data);
    }
}
