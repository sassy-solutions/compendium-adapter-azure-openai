// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIOptions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AzureOpenAI.Configuration;

/// <summary>
/// Authentication mode for the Azure OpenAI adapter.
/// </summary>
public enum AzureOpenAIAuthMode
{
    /// <summary>
    /// Use Entra ID (managed identity, service principal, or developer credentials) via
    /// <c>Azure.Identity.DefaultAzureCredential</c>. This is the recommended production setting.
    /// </summary>
    EntraID = 0,

    /// <summary>
    /// Use a static <c>api-key</c> header. Convenient for local development or CI; rotate
    /// frequently and store in a secret manager (never in source).
    /// </summary>
    ApiKey = 1
}

/// <summary>
/// Configuration options for the Azure OpenAI Service adapter.
/// </summary>
/// <remarks>
/// Unlike vanilla OpenAI, Azure routes requests to a per-resource *deployment name* rather
/// than a global model id. Compendium callers still speak in logical model names (e.g.
/// <c>gpt-4o</c>); the adapter rewrites those to your tenant's deployment name via
/// <see cref="DeploymentMapping"/>. If no mapping is provided, the model name is used verbatim
/// as the deployment name — handy when you intentionally name deployments after their model.
/// </remarks>
public sealed class AzureOpenAIOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// Default REST API version used when <see cref="ApiVersion"/> is unset.
    /// </summary>
    public const string DefaultApiVersion = "2024-10-21";

    /// <summary>
    /// Gets or sets the Azure OpenAI resource endpoint
    /// (e.g. <c>https://my-resource.openai.azure.com</c>). Required.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the REST API version (sent as the <c>api-version</c> query string).
    /// Default is <see cref="DefaultApiVersion"/>.
    /// </summary>
    public string ApiVersion { get; set; } = DefaultApiVersion;

    /// <summary>
    /// Gets or sets the authentication mode. Default is <see cref="AzureOpenAIAuthMode.EntraID"/>.
    /// </summary>
    public AzureOpenAIAuthMode AuthMode { get; set; } = AzureOpenAIAuthMode.EntraID;

    /// <summary>
    /// Gets or sets the API key. Only used when <see cref="AuthMode"/> is
    /// <see cref="AzureOpenAIAuthMode.ApiKey"/>. Ignored otherwise.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the OAuth scope used when acquiring Entra ID tokens. Default is
    /// <c>https://cognitiveservices.azure.com/.default</c> (the Azure Cognitive Services audience).
    /// </summary>
    public string EntraIDScope { get; set; } = "https://cognitiveservices.azure.com/.default";

    /// <summary>
    /// Gets or sets the default logical chat model. Used when a completion request omits
    /// <see cref="CompletionRequest.Model"/>. The value is run through <see cref="DeploymentMapping"/>
    /// before being placed on the wire.
    /// </summary>
    public string DefaultModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Gets or sets the default logical embedding model. Used when an embedding request omits
    /// <see cref="EmbeddingRequest.Model"/>.
    /// </summary>
    public string DefaultEmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Gets or sets the default sampling temperature.
    /// </summary>
    public float DefaultTemperature { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the default maximum tokens for chat completions.
    /// </summary>
    public int DefaultMaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the HTTP timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets whether to enable verbose request/response logging at debug level.
    /// </summary>
    public bool EnableLogging { get; set; }

    /// <summary>
    /// Gets or sets whether structured outputs (<c>response_format</c> JSON mode) is enabled by
    /// default for every completion request.
    /// </summary>
    public bool UseStructuredOutputsByDefault { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of inputs to send per embeddings request. Azure's per-deployment
    /// cap varies; the conservative default below works for every model family.
    /// </summary>
    public int MaxEmbeddingsBatchSize { get; set; } = 16;

    /// <summary>
    /// Gets or sets the model-to-deployment mapping. Keys are *logical model names* exposed to
    /// Compendium callers (e.g. <c>gpt-4o</c>); values are the Azure-side deployment names
    /// (e.g. <c>prod-gpt-4o</c>). Lookups are case-insensitive. When a model has no mapping the
    /// caller's value is used verbatim as the deployment name.
    /// </summary>
    public Dictionary<string, string> DeploymentMapping { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves the Azure deployment name for a given logical model id.
    /// </summary>
    /// <param name="model">Logical model id from the request.</param>
    /// <returns>The mapped deployment name, or <paramref name="model"/> when no mapping is set.</returns>
    public string ResolveDeployment(string model)
    {
        if (string.IsNullOrEmpty(model))
        {
            return string.Empty;
        }

        return DeploymentMapping.TryGetValue(model, out var deployment) && !string.IsNullOrWhiteSpace(deployment)
            ? deployment
            : model;
    }
}
