// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIStructuredOutputExtensionsTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.AzureOpenAI.StructuredOutputs;

namespace Compendium.Adapters.AzureOpenAI.Tests.StructuredOutputs;

public class AzureOpenAIStructuredOutputExtensionsTests
{
    [Fact]
    public void WithStructuredOutput_PopulatesAllKeys()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };
        var schema = """{"type":"object"}""";

        // Act
        var rewritten = request.WithStructuredOutput(schema, "MyName", strict: false);

        // Assert
        rewritten.AdditionalParameters![AzureOpenAIStructuredOutputExtensions.SchemaKey].Should().Be(schema);
        rewritten.AdditionalParameters![AzureOpenAIStructuredOutputExtensions.SchemaNameKey].Should().Be("MyName");
        rewritten.AdditionalParameters![AzureOpenAIStructuredOutputExtensions.StrictKey].Should().Be(false);
    }

    [Fact]
    public void WithStructuredOutput_DefaultStrictIsTrue()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        var rewritten = request.WithStructuredOutput("""{"type":"object"}""");

        // Assert
        rewritten.AdditionalParameters![AzureOpenAIStructuredOutputExtensions.StrictKey].Should().Be(true);
    }

    [Fact]
    public void WithJsonMode_SetsJsonModeKey()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        var rewritten = request.WithJsonMode();

        // Assert
        rewritten.AdditionalParameters![AzureOpenAIStructuredOutputExtensions.JsonModeKey].Should().Be(true);
    }

    [Fact]
    public void WithStructuredOutput_EmptySchemaName_Throws()
    {
        // Arrange
        var request = new CompletionRequest
        {
            Model = "gpt-4o-mini",
            Messages = new List<Message> { Message.User("hi") }
        };

        // Act
        var act = () => request.WithStructuredOutput("""{"type":"object"}""", "   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
