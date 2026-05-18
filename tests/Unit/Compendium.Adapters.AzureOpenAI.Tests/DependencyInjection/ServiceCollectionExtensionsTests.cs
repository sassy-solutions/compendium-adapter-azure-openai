// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Azure.Core;
using Compendium.Adapters.AzureOpenAI.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.AzureOpenAI.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumAzureOpenAI_WithConfiguration_RegistersProviderAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureOpenAI:Endpoint"] = "https://test-resource.openai.azure.com",
                ["AzureOpenAI:AuthMode"] = "ApiKey",
                ["AzureOpenAI:ApiKey"] = "test-key"
            })
            .Build();

        // Act
        services.AddCompendiumAzureOpenAI(configuration);
        var sp = services.BuildServiceProvider();

        // Assert
        var provider = sp.GetRequiredService<IAIProvider>();
        provider.Should().BeOfType<AzureOpenAIProvider>();
        provider.ProviderId.Should().Be("azure-openai");
        sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value.Endpoint
            .Should().Be("https://test-resource.openai.azure.com");
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_WithCallback_RegistersProviderAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumAzureOpenAI(o =>
        {
            o.Endpoint = "https://test-resource.openai.azure.com";
            o.AuthMode = AzureOpenAIAuthMode.ApiKey;
            o.ApiKey = "test-key";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<IAIProvider>().Should().BeOfType<AzureOpenAIProvider>();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_DefaultsToDefaultAzureCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumAzureOpenAI(o =>
        {
            o.Endpoint = "https://test-resource.openai.azure.com";
        });
        var sp = services.BuildServiceProvider();

        // Assert — TokenCredential is registered for DI (DefaultAzureCredential).
        var credential = sp.GetRequiredService<TokenCredential>();
        credential.Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_HonoursCallerProvidedTokenCredential()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customCredential = Substitute.For<TokenCredential>();
        services.AddSingleton(customCredential);

        // Act
        services.AddCompendiumAzureOpenAI(o =>
        {
            o.Endpoint = "https://test-resource.openai.azure.com";
        });
        var sp = services.BuildServiceProvider();

        // Assert — TryAddSingleton must not overwrite the user's credential.
        sp.GetRequiredService<TokenCredential>().Should().BeSameAs(customCredential);
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumAzureOpenAI(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_NullServices_WithConfiguration_Throws()
    {
        // Arrange
        IServiceCollection? services = null;
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services!.AddCompendiumAzureOpenAI(configuration);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_NullConfiguration_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumAzureOpenAI((IConfiguration)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_NullConfigureAction_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddCompendiumAzureOpenAI((Action<AzureOpenAIOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCompendiumAzureOpenAI_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumAzureOpenAI(o =>
        {
            o.Endpoint = "https://test-resource.openai.azure.com";
            o.AuthMode = AzureOpenAIAuthMode.ApiKey;
            o.ApiKey = "k";
        });
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();

        // Assert
        factory.Should().NotBeNull();
        // Resolving the typed AzureOpenAIHttpClient via IAIProvider exercises the full pipeline registration.
        var provider = sp.GetRequiredService<IAIProvider>();
        provider.Should().NotBeNull();
    }
}
