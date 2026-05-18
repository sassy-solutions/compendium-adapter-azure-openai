// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIStructuredOutputExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AzureOpenAI.StructuredOutputs;

/// <summary>
/// Ergonomic helpers for opting a completion request into Azure OpenAI's
/// <c>response_format</c> JSON-schema mode.
/// </summary>
/// <remarks>
/// JSON-schema response format is supported on deployments backed by recent
/// <c>gpt-4o</c> / <c>gpt-4o-mini</c> models and requires <c>api-version</c> &gt;= 2024-08-01-preview.
/// </remarks>
public static class AzureOpenAIStructuredOutputExtensions
{
    /// <summary>Key for the JSON-schema payload.</summary>
    public const string SchemaKey = "azure_openai.response_format.json_schema";

    /// <summary>Key for the schema name.</summary>
    public const string SchemaNameKey = "azure_openai.response_format.name";

    /// <summary>Key for strict mode (defaults to true when a schema is supplied).</summary>
    public const string StrictKey = "azure_openai.response_format.strict";

    /// <summary>Marker key requesting plain <c>json_object</c> mode (no schema).</summary>
    public const string JsonModeKey = "azure_openai.response_format.json_object";

    /// <summary>
    /// Forces the model to emit JSON conforming to <paramref name="schemaJson"/>.
    /// </summary>
    /// <param name="request">The request to clone.</param>
    /// <param name="schemaJson">JSON-schema document.</param>
    /// <param name="schemaName">Human-readable schema identifier surfaced to Azure.</param>
    /// <param name="strict">Whether Azure should reject non-conforming outputs.</param>
    public static CompletionRequest WithStructuredOutput(
        this CompletionRequest request,
        string schemaJson,
        string schemaName = "response",
        bool strict = true)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var dict = new Dictionary<string, object>(request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [SchemaKey] = schemaJson,
            [SchemaNameKey] = schemaName,
            [StrictKey] = strict
        };
        return request with { AdditionalParameters = dict };
    }

    /// <summary>
    /// Forces the model to emit valid JSON (without a schema constraint).
    /// </summary>
    public static CompletionRequest WithJsonMode(this CompletionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dict = new Dictionary<string, object>(request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [JsonModeKey] = true
        };
        return request with { AdditionalParameters = dict };
    }
}
