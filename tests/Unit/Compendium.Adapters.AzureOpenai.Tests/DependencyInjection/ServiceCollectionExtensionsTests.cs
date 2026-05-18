// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.AzureOpenai.DependencyInjection;
using Compendium.Adapters.AzureOpenai.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Compendium.Adapters.AzureOpenai.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCompendiumAzureOpenaiAdapter_WithConfiguration_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Compendium:Adapters:AzureOpenai:BaseUrl"] = "https://api.example.com",
                ["Compendium:Adapters:AzureOpenai:ApiKey"] = "k1",
            })
            .Build();

        // Act
        var actual = services.AddCompendiumAzureOpenaiAdapter(configuration);
        var sp = actual.BuildServiceProvider();

        // Assert
        actual.Should().BeSameAs(services);
        sp.GetRequiredService<AzureOpenaiAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumAzureOpenaiAdapter_WithCallback_RegistersAdapterAndOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddCompendiumAzureOpenaiAdapter(o =>
        {
            o.BaseUrl = "https://api.example.com";
            o.ApiKey = "k1";
        });
        var sp = services.BuildServiceProvider();

        // Assert
        sp.GetRequiredService<AzureOpenaiAdapter>().Should().NotBeNull();
    }

    [Fact]
    public void AddCompendiumAzureOpenaiAdapter_NullServices_Throws()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => services!.AddCompendiumAzureOpenaiAdapter(_ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
