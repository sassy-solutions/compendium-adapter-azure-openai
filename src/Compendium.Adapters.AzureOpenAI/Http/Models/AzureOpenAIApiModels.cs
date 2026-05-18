// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIApiModels.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AzureOpenAI.Http.Models;

/// <summary>
/// Azure OpenAI chat completion request body.
/// </summary>
internal sealed class AzureOpenAIChatCompletionRequest
{
    // Note: Azure routes to a deployment via the URL path, not the body. The "model" property is
    // optional and ignored by Azure but useful when proxying through gateways — we omit it.

    [JsonPropertyName("messages")]
    public required List<AzureOpenAIChatMessage> Messages { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("stream_options")]
    public AzureOpenAIStreamOptions? StreamOptions { get; set; }

    [JsonPropertyName("tools")]
    public List<AzureOpenAIToolDefinition>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("response_format")]
    public AzureOpenAIResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }
}

internal sealed class AzureOpenAIStreamOptions
{
    [JsonPropertyName("include_usage")]
    public bool IncludeUsage { get; set; }
}

/// <summary>
/// Azure OpenAI chat message. Content may be either a string (text-only) or an array of content
/// parts (vision and other modalities).
/// </summary>
internal sealed class AzureOpenAIChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// Either a string or a list of <see cref="AzureOpenAIContentPart"/>. Serialised as-is.
    /// </summary>
    [JsonPropertyName("content")]
    public object? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<AzureOpenAIToolCall>? ToolCalls { get; set; }
}

/// <summary>
/// Vision-style content part (text + image_url).
/// </summary>
internal sealed class AzureOpenAIContentPart
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    public AzureOpenAIImageUrl? ImageUrl { get; set; }
}

internal sealed class AzureOpenAIImageUrl
{
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

internal sealed class AzureOpenAIToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public required AzureOpenAIFunctionDefinition Function { get; set; }
}

internal sealed class AzureOpenAIFunctionDefinition
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("parameters")]
    public JsonElement? Parameters { get; set; }
}

internal sealed class AzureOpenAIToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public AzureOpenAIToolCallFunction? Function { get; set; }
}

internal sealed class AzureOpenAIToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

internal sealed class AzureOpenAIResponseFormat
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("json_schema")]
    public AzureOpenAIJsonSchemaFormat? JsonSchema { get; set; }
}

internal sealed class AzureOpenAIJsonSchemaFormat
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("schema")]
    public JsonElement Schema { get; set; }

    [JsonPropertyName("strict")]
    public bool? Strict { get; set; }
}

/// <summary>
/// Azure OpenAI chat completion response.
/// </summary>
internal sealed class AzureOpenAIChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("choices")]
    public List<AzureOpenAIChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public AzureOpenAIUsage? Usage { get; set; }

    /// <summary>
    /// Top-level content filter results from Azure's prompt-side filter. When all categories
    /// return <c>filtered: false</c> this object is harmless; we surface it only when the request
    /// is rejected outright (in which case Azure returns <c>400</c> with <c>content_filter</c> code).
    /// </summary>
    [JsonPropertyName("prompt_filter_results")]
    public List<AzureOpenAIPromptFilterResult>? PromptFilterResults { get; set; }
}

internal sealed class AzureOpenAIChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public AzureOpenAIChatMessage? Message { get; set; }

    [JsonPropertyName("delta")]
    public AzureOpenAIChatDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// Azure-specific completion-side content filter outcome.
    /// </summary>
    [JsonPropertyName("content_filter_results")]
    public AzureOpenAIContentFilterResults? ContentFilterResults { get; set; }
}

internal sealed class AzureOpenAIChatDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<AzureOpenAIToolCall>? ToolCalls { get; set; }
}

internal sealed class AzureOpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Azure OpenAI streaming SSE chunk.
/// </summary>
internal sealed class AzureOpenAIStreamChunk
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<AzureOpenAIChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")]
    public AzureOpenAIUsage? Usage { get; set; }
}

/// <summary>
/// Azure OpenAI embeddings request body.
/// </summary>
internal sealed class AzureOpenAIEmbeddingsRequest
{
    [JsonPropertyName("input")]
    public required List<string> Input { get; set; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonPropertyName("encoding_format")]
    public string EncodingFormat { get; set; } = "float";
}

internal sealed class AzureOpenAIEmbeddingsResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<AzureOpenAIEmbeddingData> Data { get; set; } = new();

    [JsonPropertyName("usage")]
    public AzureOpenAIEmbeddingsUsage? Usage { get; set; }
}

internal sealed class AzureOpenAIEmbeddingData
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

internal sealed class AzureOpenAIEmbeddingsUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Azure OpenAI error envelope.
/// </summary>
internal sealed class AzureOpenAIErrorResponse
{
    [JsonPropertyName("error")]
    public AzureOpenAIError? Error { get; set; }
}

internal sealed class AzureOpenAIError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Inner content-filter detail. Azure attaches the <c>content_filter_result</c>
    /// object on the error when a prompt or completion is blocked.
    /// </summary>
    [JsonPropertyName("innererror")]
    public AzureOpenAIInnerError? InnerError { get; set; }
}

internal sealed class AzureOpenAIInnerError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("content_filter_result")]
    public AzureOpenAIContentFilterResults? ContentFilterResult { get; set; }
}

/// <summary>
/// Per-request prompt content-filter result.
/// </summary>
internal sealed class AzureOpenAIPromptFilterResult
{
    [JsonPropertyName("prompt_index")]
    public int PromptIndex { get; set; }

    [JsonPropertyName("content_filter_results")]
    public AzureOpenAIContentFilterResults? ContentFilterResults { get; set; }
}

/// <summary>
/// Aggregate Azure content-filter outcome — one entry per category. Any sub-category whose
/// <see cref="AzureOpenAIContentFilterCategory.Filtered"/> is <c>true</c> indicates a block.
/// </summary>
internal sealed class AzureOpenAIContentFilterResults
{
    [JsonPropertyName("hate")]
    public AzureOpenAIContentFilterCategory? Hate { get; set; }

    [JsonPropertyName("self_harm")]
    public AzureOpenAIContentFilterCategory? SelfHarm { get; set; }

    [JsonPropertyName("sexual")]
    public AzureOpenAIContentFilterCategory? Sexual { get; set; }

    [JsonPropertyName("violence")]
    public AzureOpenAIContentFilterCategory? Violence { get; set; }

    [JsonPropertyName("jailbreak")]
    public AzureOpenAIContentFilterCategory? Jailbreak { get; set; }

    [JsonPropertyName("profanity")]
    public AzureOpenAIContentFilterCategory? Profanity { get; set; }

    /// <summary>
    /// Returns the category names whose <see cref="AzureOpenAIContentFilterCategory.Filtered"/> flag is set.
    /// </summary>
    public List<string> FilteredCategories()
    {
        var blocked = new List<string>();
        if (Hate?.Filtered == true) { blocked.Add("hate"); }
        if (SelfHarm?.Filtered == true) { blocked.Add("self_harm"); }
        if (Sexual?.Filtered == true) { blocked.Add("sexual"); }
        if (Violence?.Filtered == true) { blocked.Add("violence"); }
        if (Jailbreak?.Filtered == true) { blocked.Add("jailbreak"); }
        if (Profanity?.Filtered == true) { blocked.Add("profanity"); }
        return blocked;
    }
}

internal sealed class AzureOpenAIContentFilterCategory
{
    [JsonPropertyName("filtered")]
    public bool Filtered { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("detected")]
    public bool? Detected { get; set; }
}
