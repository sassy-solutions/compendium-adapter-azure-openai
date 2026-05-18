# Compendium.Adapters.AzureOpenAI

Azure OpenAI Service adapter for the **Compendium** event-sourcing framework. Implements
[`IAIProvider`](https://www.nuget.org/packages/Compendium.Abstractions.AI) against the Azure
OpenAI REST API: chat completions, streaming, embeddings, tool calling, structured outputs,
vision inputs, and first-class Azure content-filter handling.

## What's different vs `Compendium.Adapters.OpenAI`

| | Vanilla OpenAI | **Azure OpenAI (this adapter)** |
|---|---|---|
| Auth | API key (`Authorization: Bearer …`) | **Entra ID by default** (`DefaultAzureCredential`) or `api-key` header |
| URL shape | `/v1/chat/completions` | `/openai/deployments/{deployment}/chat/completions?api-version=…` |
| Routing | by model id | by **deployment name** — the adapter maps logical model ids to deployments |
| Content filter | not surfaced specially | `Result.Failure(AIErrors.ContentFiltered)` — both at request time and mid-stream |
| Models endpoint | `GET /models` | synthesised from `DeploymentMapping` (Azure has no per-resource discovery endpoint that matches) |

## Quick start

### 1. Create the deployment in Azure

This adapter does not provision deployments — that's a portal/Bicep/Terraform step. Once a
deployment exists, you only need two things to use it:

- the resource **endpoint**, e.g. `https://my-resource.openai.azure.com`
- the **deployment name** (an alias you chose, *not* the model id)

### 2. Install

```xml
<PackageReference Include="Compendium.Adapters.AzureOpenAI" Version="..." />
```

### 3. Register

```csharp
using Compendium.Adapters.AzureOpenAI.Configuration;
using Compendium.Adapters.AzureOpenAI.DependencyInjection;

services.AddCompendiumAzureOpenAI(o =>
{
    o.Endpoint = "https://my-resource.openai.azure.com";
    o.AuthMode = AzureOpenAIAuthMode.EntraID; // default — uses DefaultAzureCredential
    o.DefaultModel = "gpt-4o-mini";
    o.DeploymentMapping["gpt-4o-mini"]            = "prod-gpt-4o-mini";
    o.DeploymentMapping["text-embedding-3-small"] = "prod-embeddings";
});
```

…or bind from configuration:

```jsonc
// appsettings.json
{
  "AzureOpenAI": {
    "Endpoint": "https://my-resource.openai.azure.com",
    "ApiVersion": "2024-10-21",
    "AuthMode": "EntraID",
    "DefaultModel": "gpt-4o-mini",
    "DeploymentMapping": {
      "gpt-4o-mini": "prod-gpt-4o-mini",
      "text-embedding-3-small": "prod-embeddings"
    }
  }
}
```

```csharp
services.AddCompendiumAzureOpenAI(builder.Configuration);
```

### 4. Use

```csharp
var ai = sp.GetRequiredService<IAIProvider>();
var result = await ai.CompleteAsync(new CompletionRequest
{
    Model = "gpt-4o-mini",
    Messages = new() { Message.User("Say hi.") }
}, ct);

if (result.IsFailure) { /* AIErrors.ContentFiltered, .RateLimitExceeded, .InvalidApiKey, ... */ }
else                  { Console.WriteLine(result.Value.Content); }
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `Endpoint` | (required) | `https://<resource>.openai.azure.com` |
| `ApiVersion` | `2024-10-21` | Sent as the `api-version` query string |
| `AuthMode` | `EntraID` | `EntraID` or `ApiKey` |
| `ApiKey` | `null` | Only used when `AuthMode = ApiKey` |
| `EntraIDScope` | `https://cognitiveservices.azure.com/.default` | OAuth scope for token acquisition |
| `DefaultModel` | `gpt-4o-mini` | Used when a request omits `Model` |
| `DefaultEmbeddingModel` | `text-embedding-3-small` | Used when an embedding request omits `Model` |
| `DefaultMaxTokens` | `4096` | Applied when `CompletionRequest.MaxTokens` is null |
| `TimeoutSeconds` | `120` | HTTP timeout per request |
| `EnableLogging` | `false` | Verbose debug logs of payloads |
| `UseStructuredOutputsByDefault` | `false` | Force `response_format=json_object` on every chat |
| `MaxEmbeddingsBatchSize` | `16` | Conservative — embedding deployments cap varies |
| `DeploymentMapping` | `{}` | logical-model → Azure-deployment alias |

## Entra ID vs API key trade-offs

|  | Entra ID (default) | API key |
|---|---|---|
| Setup | Assign `Cognitive Services OpenAI User` role to your identity | Generate a key in the Azure Portal |
| Rotation | Automatic (token cache + refresh) | Manual — rotate often |
| Audit | Per-principal in Azure activity log | Per-key (less granular) |
| Local dev | `az login` or `azd auth login` | Same key everywhere |
| Production | Recommended (managed identity) | Discouraged |

In production, the adapter uses `DefaultAzureCredential`, which automatically tries:
managed identity → workload identity → environment vars (service principal) → Visual Studio →
`az login`. Override by registering your own `TokenCredential` *before* `AddCompendiumAzureOpenAI`:

```csharp
services.AddSingleton<TokenCredential>(new ManagedIdentityCredential(clientId));
services.AddCompendiumAzureOpenAI(opts);
```

## Deployment-mapping pattern

Azure routes by *deployment name*, which is per-resource. Compendium-side, requests still
specify a model id. The adapter rewrites:

```
caller asks for: "gpt-4o-mini"
                       ↓ DeploymentMapping
URL becomes:      …/openai/deployments/prod-gpt-4o-mini/chat/completions?api-version=…
```

When a model has no mapping entry, the adapter uses the model name verbatim as the deployment
name. That's handy when your deployment is *literally* named after its model (e.g. you created
a deployment named `gpt-4o-mini` for the `gpt-4o-mini` model).

## Content-filter handling

Azure can block content in two places:

1. **Prompt-side** — Azure returns `400` with `error.code = "content_filter"` plus a detailed
   `innererror.content_filter_result` describing which categories tripped.
2. **Completion-side** — Azure returns `200 OK` with `finish_reason = "content_filter"` and a
   `content_filter_results` block on the choice.

Both surface as `Result.Failure` with `Error.Code = "AI.ContentFiltered"`. The message lists the
blocked categories when available (e.g. `Azure content filter blocked categories: violence, sexual`).
Mid-stream blocks during `StreamCompleteAsync` are also detected and surfaced as a single
`Result.Failure` chunk before the stream closes.

## Production checklist

- [ ] Resource has the deployments you reference in `DeploymentMapping` (typo'd names return 404).
- [ ] The principal you run as has the `Cognitive Services OpenAI User` role on the resource.
- [ ] `ApiVersion` matches a version your deployment supports (`2024-10-21` is GA-safe).
- [ ] `MaxEmbeddingsBatchSize` is tuned for your embedding model's cap (default 16 is conservative).
- [ ] Logging at `Debug` is **off** in production (`EnableLogging = false`) — request bodies may contain user PII.
- [ ] Caller code handles `AIErrors.ContentFiltered` — it's not the same as a transient error and **must not** be retried with the same prompt.

## Testability strategy

This adapter is hand-rolled on `HttpClient` rather than wrapping the `Azure.AI.OpenAI` SDK.
We use the SDK only for `TokenCredential` token acquisition (the part that is genuinely
non-trivial to hand-roll). For the rest:

- **Pro**: full mocking via `MockHttpMessageHandler` at the transport boundary — every test
  in this suite drives requests through a fake `HttpClient`.
- **Pro**: deployment-name routing and API-version query injection are expressed directly
  in URL strings, which are easy to assert on.
- **Con**: we re-implement (~150 lines of) request/response DTOs and SSE parsing. That code
  is straightforward and well-covered.

When the `Azure.AI.OpenAI` SDK adds capabilities we'd rather not re-implement (e.g. the Realtime
API, Assistants), we'll revisit this trade-off per-feature.

## License

MIT — same as Compendium itself.
