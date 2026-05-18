# Changelog

All notable changes to `Compendium.Adapters.AzureOpenAI` are documented here.
The project follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial implementation of `Compendium.Adapters.AzureOpenAI`, a direct adapter against the Azure OpenAI Service REST API for [`Compendium.Abstractions.AI`](https://www.nuget.org/packages/Compendium.Abstractions.AI) 1.0.1.
- `AzureOpenAIProvider` implementing `IAIProvider` with:
  - Chat completions (`POST /openai/deployments/{deployment}/chat/completions?api-version=…`).
  - Streaming completions via SSE — including mid-stream content-filter blocks surfaced as `Result.Failure(AIErrors.ContentFiltered)`.
  - Embeddings (`POST /openai/deployments/{deployment}/embeddings?api-version=…`) — automatically batched (configurable cap, conservative default 16) with index re-mapping.
  - Tool / function calling round-trip — request via `WithTools(...)`; response surfaced as `AgentToolInvocation` list on `CompletionResponse.Metadata` and read with `GetToolCalls()`.
  - Structured outputs (`response_format` JSON-schema mode) — opt-in per request via `WithStructuredOutput(...)` or per-options via `AzureOpenAIOptions.UseStructuredOutputsByDefault`.
  - Vision inputs — opt-in per request via `WithImages(...)`, which rewrites the last user message into Azure's multimodal `content` shape.
- **Authentication**:
  - **Entra ID** (default, production-grade) — `Azure.Identity.DefaultAzureCredential` acquires bearer tokens via a `DelegatingHandler`. Honours managed identity, service principals, dev tooling, etc. Users can register a custom `TokenCredential` to override.
  - **API key** (dev / CI) — static `api-key` header. Selected via `AzureOpenAIOptions.AuthMode = ApiKey`.
- **Deployment-name routing**: Compendium callers request logical model names (e.g. `gpt-4o`); the adapter rewrites those to Azure deployment names via `AzureOpenAIOptions.DeploymentMapping`. When a model has no mapping, the model name is used verbatim as the deployment name.
- **Content-filter handling**: Azure-specific `code: "content_filter"` errors and 200-OK responses with `finish_reason: "content_filter"` are translated to `Result.Failure(AIErrors.ContentFiltered(...))`; the canonical error code is `AI.ContentFiltered`.
- `AzureOpenAIHttpClient` typed `HttpClient` with API-version query-string injection, JSON serialisation (snake_case wire format, null-omit), SSE stream reader, and resilience via `Microsoft.Extensions.Http.Resilience`'s standard pipeline.
- `EntraIDAuthHandler` `DelegatingHandler` for bearer-token stamping per request.
- Error mapping for HTTP 400 / 401 / 403 / 404 / 429 / 5xx into `AIErrors.*` codes; caller cancellation is rethrown, other `TaskCanceledException`s map to `AIErrors.Timeout`.
- `AzureOpenAIOptions` with sensible defaults (`api-version: 2024-10-21`, `cognitiveservices.azure.com/.default` scope, `gpt-4o-mini` chat, `text-embedding-3-small` embeddings, 120s timeout, 16-input embedding batch).
- Sample [`samples/01-chat-with-managed-identity`](samples/01-chat-with-managed-identity) demonstrating end-to-end Entra ID auth.

### Notes

- Unit test suite: 128 tests, **96.7 %** line coverage on the unit-testable surface (see CI summary).
- **Testability strategy**: this adapter is hand-rolled on top of `HttpClient` rather than wrapping the `Azure.AI.OpenAI` SDK. Reasons: (a) easy and complete mocking via `MockHttpMessageHandler` at the transport boundary, (b) full control over the deployment-name routing and API-version query injection, (c) no leaky abstractions over Azure's sealed SDK client types. `Azure.Identity` is still used for `TokenCredential` token acquisition — that part is correctly delegated to the official SDK.
- No NuGet release tagged yet — release tagging is performed by the orchestrator after merge.
