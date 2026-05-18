// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIToolCallingExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Abstractions.AI.Agents.Models;
using Compendium.Adapters.AzureOpenAI.Tools;

namespace Compendium.Adapters.AzureOpenAI.Tests.Tools;

public class AzureOpenAIToolCallingExtensionsTests
{
    [Fact]
    public void WithTools_AppendsToolsKey()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        var rewritten = request.WithTools(new List<AgentTool> { new("foo", "bar") }, "auto");

        // Assert
        rewritten.AdditionalParameters.Should().NotBeNull();
        rewritten.AdditionalParameters!.Should().ContainKey(AzureOpenAIToolCallingExtensions.ToolsKey);
        rewritten.AdditionalParameters!.Should().ContainKey(AzureOpenAIToolCallingExtensions.ToolChoiceKey);
        rewritten.AdditionalParameters![AzureOpenAIToolCallingExtensions.ToolChoiceKey].Should().Be("auto");
    }

    [Fact]
    public void WithTools_PreservesExistingAdditionalParameters()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") },
            AdditionalParameters = new Dictionary<string, object> { ["existing"] = "value" }
        };

        // Act
        var rewritten = request.WithTools(new List<AgentTool> { new("foo", "bar") });

        // Assert
        rewritten.AdditionalParameters!.Should().ContainKey("existing");
    }

    [Fact]
    public void WithTools_WithoutToolChoice_DoesNotSetToolChoiceKey()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        var rewritten = request.WithTools(new List<AgentTool> { new("foo", "bar") });

        // Assert
        rewritten.AdditionalParameters!.Should().NotContainKey(AzureOpenAIToolCallingExtensions.ToolChoiceKey);
    }

    [Fact]
    public void GetToolCalls_WhenMetadataPresent_ReturnsInvocations()
    {
        // Arrange
        var invocations = new List<AgentToolInvocation>
        {
            new("foo", "{}", string.Empty, false, TimeSpan.Zero)
        };
        var response = new CompletionResponse
        {
            Id = "x",
            Model = "m",
            Content = string.Empty,
            FinishReason = FinishReason.ToolCall,
            Usage = new UsageStats { PromptTokens = 0, CompletionTokens = 0 },
            Metadata = new Dictionary<string, object>
            {
                [AzureOpenAIToolCallingExtensions.ToolCallsMetadataKey] = (IReadOnlyList<AgentToolInvocation>)invocations
            }
        };

        // Act
        var calls = response.GetToolCalls();

        // Assert
        calls.Should().BeEquivalentTo(invocations);
    }

    [Fact]
    public void GetToolCalls_NullResponse_Throws()
    {
        // Arrange
        CompletionResponse? response = null;

        // Act
        var act = () => response!.GetToolCalls();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
