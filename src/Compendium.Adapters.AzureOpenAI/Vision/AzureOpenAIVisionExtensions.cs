// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIVisionExtensions.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

namespace Compendium.Adapters.AzureOpenAI.Vision;

/// <summary>
/// A single image attachment to be sent with the next user message, plus an optional
/// <c>detail</c> hint (<c>"low"</c> / <c>"high"</c> / <c>"auto"</c>).
/// </summary>
/// <param name="Url">A publicly fetchable URL or a <c>data:image/...;base64,...</c> URI.</param>
/// <param name="Detail">Optional Azure detail hint; <c>null</c> defers to the service default.</param>
public sealed record AzureOpenAIImageInput(string Url, string? Detail = null);

/// <summary>
/// Ergonomic helpers for attaching image content to vision-capable deployments
/// (e.g. <c>gpt-4o</c>, <c>gpt-4o-mini</c>).
/// </summary>
public static class AzureOpenAIVisionExtensions
{
    /// <summary>Key inside <see cref="CompletionRequest.AdditionalParameters"/> carrying image attachments.</summary>
    public const string ImagesKey = "azure_openai.images";

    /// <summary>
    /// Attaches one or more images to a completion request. The adapter rewrites the final user
    /// message into Azure's multimodal content-part shape (text + image_url[]) when the request
    /// is dispatched.
    /// </summary>
    /// <param name="request">The request to clone.</param>
    /// <param name="images">Image attachments to include.</param>
    public static CompletionRequest WithImages(
        this CompletionRequest request,
        IReadOnlyList<AzureOpenAIImageInput> images)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(images);

        var dict = new Dictionary<string, object>(request.AdditionalParameters ?? new Dictionary<string, object>())
        {
            [ImagesKey] = images
        };
        return request with { AdditionalParameters = dict };
    }
}
