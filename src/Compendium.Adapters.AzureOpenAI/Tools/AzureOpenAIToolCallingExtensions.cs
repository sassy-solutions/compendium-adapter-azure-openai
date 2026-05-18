// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIToolCallingExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;

namespace Compendium.Adapters.AzureOpenAI.Tools;

/// <summary>
/// Ergonomic helpers for attaching tool definitions and reading back tool invocations when
/// round-tripping through the abstractions' provider-agnostic
/// <see cref="CompletionRequest"/> and <see cref="CompletionResponse"/>.
/// </summary>
/// <remarks>
/// The abstractions don't carry first-class tool metadata on <see cref="CompletionRequest"/>;
/// this adapter uses well-known keys inside <see cref="CompletionRequest.AdditionalParameters"/>
/// and surfaces tool calls back through <see cref="CompletionResponse.Metadata"/>. Keys live
/// inside this adapter — always go through the helpers below rather than the raw strings.
/// </remarks>
public static class AzureOpenAIToolCallingExtensions
{
    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying the tool list.</summary>
    public const string ToolsKey = "azure_openai.tools";

    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying the tool_choice value.</summary>
    public const string ToolChoiceKey = "azure_openai.tool_choice";

    /// <summary>Key inside <see cref="CompletionResponse.Metadata"/> carrying the assistant's tool_calls.</summary>
    public const string ToolCallsMetadataKey = "azure_openai.tool_calls";

    /// <summary>
    /// Attaches a tool catalog to a completion request. The model receives the tool list and may
    /// emit one or more <see cref="AgentToolInvocation"/> entries in
    /// <see cref="CompletionResponse.Metadata"/> under <see cref="ToolCallsMetadataKey"/>.
    /// </summary>
    /// <param name="request">The request to clone.</param>
    /// <param name="tools">The tools to expose; ignored when empty.</param>
    /// <param name="toolChoice">Optional choice strategy ("auto", "required", "none", or a tool name).</param>
    public static CompletionRequest WithTools(
        this CompletionRequest request,
        IReadOnlyList<AgentTool> tools,
        string? toolChoice = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(tools);

        var dict = new Dictionary<string, object>(request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [ToolsKey] = tools
        };
        if (!string.IsNullOrEmpty(toolChoice))
        {
            dict[ToolChoiceKey] = toolChoice;
        }

        return request with { AdditionalParameters = dict };
    }

    /// <summary>
    /// Reads back tool calls the model requested, if any. Returns an empty list when the model
    /// did not call a tool.
    /// </summary>
    public static IReadOnlyList<AgentToolInvocation> GetToolCalls(this CompletionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if (response.Metadata != null
            && response.Metadata.TryGetValue(ToolCallsMetadataKey, out var raw)
            && raw is IReadOnlyList<AgentToolInvocation> invocations)
        {
            return invocations;
        }
        return Array.Empty<AgentToolInvocation>();
    }
}
