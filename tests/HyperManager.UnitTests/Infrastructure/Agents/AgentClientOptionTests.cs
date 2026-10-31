namespace HyperManager.Infrastructure.Agents;

using System.ComponentModel.DataAnnotations;

public sealed class AgentClientOptionTests
{
    [Fact]
    public void ValidateValidOptionReturnsValid()
    {
        // Arrange
        var option = new AgentClientOption
        {
            Hosts =
            [
                new AgentHostOption { Name = "A", Url = new Uri("http://host-a:5100"), ApiKey = "key-a" },
                new AgentHostOption { Name = "B", Url = new Uri("https://host-b/agent/"), ApiKey = "key-b" }
            ]
        };

        // Act
        var results = Validate(option);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ValidateDuplicateNameReturnsInvalid()
    {
        // Arrange
        var option = new AgentClientOption
        {
            Hosts =
            [
                new AgentHostOption { Name = "A", Url = new Uri("http://host-a:5100"), ApiKey = "key" },
                new AgentHostOption { Name = "A", Url = new Uri("http://host-b:5100"), ApiKey = "key" }
            ]
        };

        // Act
        var results = Validate(option);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void ValidateRelativeUrlReturnsInvalid()
    {
        // Arrange
        var option = new AgentClientOption
        {
            Hosts = [new AgentHostOption { Name = "A", Url = new Uri("host-a", UriKind.Relative), ApiKey = "key" }]
        };

        // Act
        var results = Validate(option);

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public void ValidateEmptyApiKeyReturnsInvalid()
    {
        // Arrange
        var option = new AgentClientOption
        {
            Hosts = [new AgentHostOption { Name = "A", Url = new Uri("http://host-a:5100"), ApiKey = string.Empty }]
        };

        // Act
        var results = Validate(option);

        // Assert
        Assert.Single(results);
    }

    private static List<ValidationResult> Validate(AgentClientOption option)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(option, new ValidationContext(option), results, true);
        return results;
    }
}
