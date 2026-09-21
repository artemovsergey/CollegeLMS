using CollegeLMS.MaxBot.Services;
using FluentAssertions;
using Xunit;

namespace CollegeLMS.MaxBot.Tests;

public class WebhookSecretValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("anything")]
    public void IsValid_ExpectedSecretEmpty_AllowsAnyRequest(string? provided)
    {
        WebhookSecretValidator.IsValid(null, provided).Should().BeTrue();
        WebhookSecretValidator.IsValid("", provided).Should().BeTrue();
    }

    [Fact]
    public void IsValid_MatchingSecret_ReturnsTrue()
    {
        WebhookSecretValidator.IsValid("topsecret", "topsecret").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData("topsecret2")]
    public void IsValid_NotMatchingSecret_ReturnsFalse(string? provided)
    {
        WebhookSecretValidator.IsValid("topsecret", provided).Should().BeFalse();
    }
}
