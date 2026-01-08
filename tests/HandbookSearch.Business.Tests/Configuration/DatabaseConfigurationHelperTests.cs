using Microsoft.Extensions.Configuration;
using Olbrasoft.HandbookSearch.Business.Configuration;

namespace HandbookSearch.Business.Tests.Configuration;

public class DatabaseConfigurationHelperTests
{
    [Fact]
    public void BuildConnectionString_WithAllValues_ReturnsCompleteConnectionString()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "myhost",
                ["Database:Name"] = "mydb",
                ["Database:Username"] = "myuser",
                ["Database:Password"] = "mypassword"
            })
            .Build();

        // Act
        var result = DatabaseConfigurationHelper.BuildConnectionString(config.GetSection("Database"));

        // Assert
        Assert.Equal("Host=myhost;Database=mydb;Username=myuser;Password=mypassword", result);
    }

    [Fact]
    public void BuildConnectionString_WithoutPassword_ReturnsConnectionStringWithoutPassword()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "localhost",
                ["Database:Name"] = "testdb",
                ["Database:Username"] = "testuser"
            })
            .Build();

        // Act
        var result = DatabaseConfigurationHelper.BuildConnectionString(config.GetSection("Database"));

        // Assert
        Assert.Equal("Host=localhost;Database=testdb;Username=testuser", result);
        Assert.DoesNotContain("Password", result);
    }

    [Fact]
    public void BuildConnectionString_WithEmptyPassword_ReturnsConnectionStringWithoutPassword()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "localhost",
                ["Database:Name"] = "testdb",
                ["Database:Username"] = "testuser",
                ["Database:Password"] = ""
            })
            .Build();

        // Act
        var result = DatabaseConfigurationHelper.BuildConnectionString(config.GetSection("Database"));

        // Assert
        Assert.Equal("Host=localhost;Database=testdb;Username=testuser", result);
        Assert.DoesNotContain("Password", result);
    }

    [Fact]
    public void BuildConnectionString_WithDefaults_ReturnsDefaultValues()
    {
        // Arrange - empty configuration
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var result = DatabaseConfigurationHelper.BuildConnectionString(config.GetSection("Database"));

        // Assert
        Assert.Equal("Host=localhost;Database=handbook_search;Username=postgres", result);
    }

    [Fact]
    public void BuildConnectionString_NullDbConfig_ThrowsArgumentNullException()
    {
        // Arrange
        IConfigurationSection? dbConfig = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DatabaseConfigurationHelper.BuildConnectionString(dbConfig!));
    }

    [Fact]
    public void BuildConnectionString_PartialConfig_UsesDefaults()
    {
        // Arrange - only host specified
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Host"] = "custom-host"
            })
            .Build();

        // Act
        var result = DatabaseConfigurationHelper.BuildConnectionString(config.GetSection("Database"));

        // Assert
        Assert.Contains("Host=custom-host", result);
        Assert.Contains("Database=handbook_search", result);
        Assert.Contains("Username=postgres", result);
    }
}
