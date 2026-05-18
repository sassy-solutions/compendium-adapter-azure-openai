// -----------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Azure.Core;
using Azure.Identity;
using Compendium.Adapters.AzureOpenAI.Configuration;
using Compendium.Adapters.AzureOpenAI.Http;
using Compendium.Adapters.AzureOpenAI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Compendium.Adapters.AzureOpenAI.DependencyInjection;

/// <summary>
/// DI extensions for the Azure OpenAI Compendium adapter.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure OpenAI as the <see cref="IAIProvider"/> with options bound from
    /// <paramref name="configuration"/> at section <see cref="AzureOpenAIOptions.SectionName"/>.
    /// </summary>
    public static IServiceCollection AddCompendiumAzureOpenAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        return services.AddCompendiumAzureOpenAICore();
    }

    /// <summary>
    /// Registers Azure OpenAI as the <see cref="IAIProvider"/> with options configured inline.
    /// </summary>
    public static IServiceCollection AddCompendiumAzureOpenAI(
        this IServiceCollection services,
        Action<AzureOpenAIOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services.AddCompendiumAzureOpenAICore();
    }

    private static IServiceCollection AddCompendiumAzureOpenAICore(this IServiceCollection services)
    {
        // Default credential is `DefaultAzureCredential`; consumers may override with
        // `services.AddSingleton<TokenCredential>(my-credential)` *before* this call to inject a
        // ChainedTokenCredential, ManagedIdentityCredential, ClientSecretCredential, etc.
        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        var builder = services.AddHttpClient<AzureOpenAIHttpClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(options.Endpoint))
            {
                client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
            }
        });

        builder.AddHttpMessageHandler(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            // The handler is created per-pipeline; it is a no-op when ApiKey auth is selected.
            if (options.AuthMode == AzureOpenAIAuthMode.EntraID)
            {
                var credential = sp.GetRequiredService<TokenCredential>();
                return new EntraIDAuthHandler(credential, options);
            }
            return new PassthroughHandler();
        });

        builder.AddStandardResilienceHandler();

        services.AddSingleton<AzureOpenAIProvider>();
        services.AddSingleton<IAIProvider>(sp => sp.GetRequiredService<AzureOpenAIProvider>());

        return services;
    }

    /// <summary>
    /// Inert delegating handler used when API-key auth is selected — the key is stamped on the
    /// HttpClient default headers instead, so we don't need a token-fetching handler.
    /// </summary>
    private sealed class PassthroughHandler : DelegatingHandler
    {
    }
}
