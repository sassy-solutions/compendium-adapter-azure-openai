// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIOptionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AzureOpenAI.Tests.Configuration;

public class AzureOpenAIOptionsTests
{
    [Fact]
    public void Defaults_AreSensible()
    {
        // Arrange / Act
        var options = new AzureOpenAIOptions();

        // Assert
        options.Endpoint.Should().BeEmpty();
        options.ApiVersion.Should().Be("2024-10-21");
        options.AuthMode.Should().Be(AzureOpenAIAuthMode.EntraID);
        options.ApiKey.Should().BeNull();
        options.EntraIDScope.Should().Be("https://cognitiveservices.azure.com/.default");
        options.DefaultModel.Should().Be("gpt-4o-mini");
        options.DefaultEmbeddingModel.Should().Be("text-embedding-3-small");
        options.DefaultMaxTokens.Should().Be(4096);
        options.TimeoutSeconds.Should().Be(120);
        options.EnableLogging.Should().BeFalse();
        options.UseStructuredOutputsByDefault.Should().BeFalse();
        options.MaxEmbeddingsBatchSize.Should().Be(16);
        options.DeploymentMapping.Should().BeEmpty();
    }

    [Fact]
    public void SectionName_IsCanonical()
    {
        // Assert
        AzureOpenAIOptions.SectionName.Should().Be("AzureOpenAI");
    }

    [Fact]
    public void DefaultApiVersion_IsCanonical()
    {
        // Assert
        AzureOpenAIOptions.DefaultApiVersion.Should().Be("2024-10-21");
    }

    [Fact]
    public void ResolveDeployment_WithKnownMapping_ReturnsDeploymentName()
    {
        // Arrange
        var options = new AzureOpenAIOptions
        {
            DeploymentMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-4o"] = "prod-gpt-4o"
            }
        };

        // Act
        var deployment = options.ResolveDeployment("gpt-4o");

        // Assert
        deployment.Should().Be("prod-gpt-4o");
    }

    [Fact]
    public void ResolveDeployment_WithUnknownModel_FallsBackToModelName()
    {
        // Arrange
        var options = new AzureOpenAIOptions();

        // Act
        var deployment = options.ResolveDeployment("gpt-4o");

        // Assert
        deployment.Should().Be("gpt-4o");
    }

    [Fact]
    public void ResolveDeployment_WithEmptyModel_ReturnsEmpty()
    {
        // Arrange
        var options = new AzureOpenAIOptions();

        // Act
        var deployment = options.ResolveDeployment(string.Empty);

        // Assert
        deployment.Should().BeEmpty();
    }

    [Fact]
    public void ResolveDeployment_WithCaseInsensitiveMapping_MatchesAcrossCases()
    {
        // Arrange
        var options = new AzureOpenAIOptions
        {
            DeploymentMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["GPT-4o"] = "prod-gpt-4o"
            }
        };

        // Act
        var deployment = options.ResolveDeployment("gpt-4O");

        // Assert
        deployment.Should().Be("prod-gpt-4o");
    }

    [Fact]
    public void ResolveDeployment_WithWhitespaceMapping_FallsBackToModelName()
    {
        // Arrange — a misconfigured mapping value should not silently emit empty deployment names.
        var options = new AzureOpenAIOptions
        {
            DeploymentMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-4o"] = "   "
            }
        };

        // Act
        var deployment = options.ResolveDeployment("gpt-4o");

        // Assert
        deployment.Should().Be("gpt-4o");
    }
}
