using CollegeLMS.MaxBot.Services;

namespace CollegeLMS.MaxBot.Tests;

public class CallbackPayloadTests
{
    [Fact]
    public void Parse_DayPayload_SplitsParts()
    {
        var p = CallbackPayload.Parse("day:2026-09-08");

        p.Should().NotBeNull();
        p!.Action.Should().Be("day");
        p.Param1.Should().Be("2026-09-08");
        p.Param2.Should().BeNull();
    }

    [Fact]
    public void Parse_ThreePartPayload_SplitsAll()
    {
        var p = CallbackPayload.Parse("page:groups:2");

        p.Should().NotBeNull();
        p!.Action.Should().Be("page");
        p.Param1.Should().Be("groups");
        p.Param2.Should().Be("2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_Blank_ReturnsNull(string? payload)
    {
        CallbackPayload.Parse(payload).Should().BeNull();
    }
}
