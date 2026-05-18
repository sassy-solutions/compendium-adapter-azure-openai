// -----------------------------------------------------------------------
// <copyright file="EntraIDAuthHandler.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Azure.Core;
using Compendium.Adapters.AzureOpenAI.Configuration;

namespace Compendium.Adapters.AzureOpenAI.Http;

/// <summary>
/// Delegating handler that stamps every outgoing request with a fresh Entra ID bearer token
/// acquired from an Azure <see cref="TokenCredential"/>.
/// </summary>
/// <remarks>
/// The <see cref="TokenCredential"/> abstraction takes care of token caching and refresh; we
/// always call <see cref="TokenCredential.GetTokenAsync(TokenRequestContext, CancellationToken)"/>
/// per request to honour expiry, which is cheap on the SDK's cached fast path.
/// </remarks>
internal sealed class EntraIDAuthHandler : DelegatingHandler
{
    private readonly TokenCredential _credential;
    private readonly string _scope;

    public EntraIDAuthHandler(TokenCredential credential, AzureOpenAIOptions options)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(options);
        _credential = credential;
        _scope = string.IsNullOrWhiteSpace(options.EntraIDScope)
            ? "https://cognitiveservices.azure.com/.default"
            : options.EntraIDScope;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { _scope }),
            cancellationToken).ConfigureAwait(false);

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
