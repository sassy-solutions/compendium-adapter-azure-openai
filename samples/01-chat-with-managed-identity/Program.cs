// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

// Sample: chat completion against Azure OpenAI using Entra ID (DefaultAzureCredential).
//
// Prerequisites
// -------------
// 1. An Azure OpenAI resource with a chat deployment (e.g. gpt-4o-mini).
// 2. The principal you're running as (managed identity, service principal, az login user, etc.)
//    must have the `Cognitive Services OpenAI User` role on the resource.
// 3. Environment:
//      export AZURE_OPENAI_ENDPOINT=https://my-resource.openai.azure.com
//      export AZURE_OPENAI_DEPLOYMENT=my-gpt-4o-mini-deployment
//
// Run
// ---
//   dotnet run --project samples/01-chat-with-managed-identity

using Compendium.Abstractions.AI;
using Compendium.Abstractions.AI.Models;
using Compendium.Adapters.AzureOpenAI.Configuration;
using Compendium.Adapters.AzureOpenAI.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required.");
var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
    ?? "gpt-4o-mini";

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddCompendiumAzureOpenAI(o =>
{
    o.Endpoint = endpoint;
    o.AuthMode = AzureOpenAIAuthMode.EntraID;
    o.DefaultModel = "gpt-4o-mini";
    o.DeploymentMapping["gpt-4o-mini"] = deployment;
});

await using var sp = services.BuildServiceProvider();
var provider = sp.GetRequiredService<IAIProvider>();

Console.WriteLine($"Using provider: {provider.ProviderId}");
Console.WriteLine($"Endpoint:        {endpoint}");
Console.WriteLine($"Deployment:      {deployment}");
Console.WriteLine();

var request = new CompletionRequest
{
    Model = "gpt-4o-mini",
    SystemPrompt = "You are a terse software engineer.",
    Messages = new List<Message> { Message.User("In one sentence: why is event sourcing useful?") }
};

var result = await provider.CompleteAsync(request, CancellationToken.None);
if (result.IsFailure)
{
    Console.Error.WriteLine($"Failed: {result.Error.Code} — {result.Error.Message}");
    return 1;
}

Console.WriteLine($"Assistant: {result.Value.Content}");
Console.WriteLine($"Tokens:    prompt={result.Value.Usage.PromptTokens} completion={result.Value.Usage.CompletionTokens}");
return 0;
