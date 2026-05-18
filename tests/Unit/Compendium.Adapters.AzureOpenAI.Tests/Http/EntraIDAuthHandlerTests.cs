// -----------------------------------------------------------------------
// <copyright file="EntraIDAuthHandlerTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Azure.Core;
using Compendium.Adapters.AzureOpenAI.Http;

namespace Compendium.Adapters.AzureOpenAI.Tests.Http;

public class EntraIDAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_StampsBearerTokenFromCredential()
    {
        // Arrange
        var credential = new FakeTokenCredential(new AccessToken("token-abc", DateTimeOffset.UtcNow.AddHours(1)));
        var options = new AzureOpenAIOptions { EntraIDScope = "https://cognitiveservices.azure.com/.default" };
        var capturing = new CapturingHandler();
        var sut = new EntraIDAuthHandler(credential, options) { InnerHandler = capturing };
        var invoker = new HttpMessageInvoker(sut);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example/");

        // Act
        await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        capturing.LastRequest.Should().NotBeNull();
        capturing.LastRequest!.Headers.Authorization.Should().NotBeNull();
        capturing.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturing.LastRequest.Headers.Authorization.Parameter.Should().Be("token-abc");
        credential.Calls.Should().Be(1);
        credential.LastScopes.Should().BeEquivalentTo(new[] { "https://cognitiveservices.azure.com/.default" });
    }

    [Fact]
    public async Task SendAsync_HonoursCustomScope()
    {
        // Arrange
        var credential = new FakeTokenCredential(new AccessToken("t", DateTimeOffset.UtcNow.AddHours(1)));
        var options = new AzureOpenAIOptions { EntraIDScope = "https://custom-audience/.default" };
        var sut = new EntraIDAuthHandler(credential, options) { InnerHandler = new CapturingHandler() };
        var invoker = new HttpMessageInvoker(sut);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example/"), CancellationToken.None);

        // Assert
        credential.LastScopes.Should().BeEquivalentTo(new[] { "https://custom-audience/.default" });
    }

    [Fact]
    public async Task SendAsync_WithWhitespaceScope_FallsBackToDefault()
    {
        // Arrange
        var credential = new FakeTokenCredential(new AccessToken("t", DateTimeOffset.UtcNow.AddHours(1)));
        var options = new AzureOpenAIOptions { EntraIDScope = "   " };
        var sut = new EntraIDAuthHandler(credential, options) { InnerHandler = new CapturingHandler() };
        var invoker = new HttpMessageInvoker(sut);

        // Act
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example/"), CancellationToken.None);

        // Assert
        credential.LastScopes.Should().BeEquivalentTo(new[] { "https://cognitiveservices.azure.com/.default" });
    }

    [Fact]
    public async Task SendAsync_NullRequest_Throws()
    {
        // Arrange
        var credential = new FakeTokenCredential(new AccessToken("t", DateTimeOffset.UtcNow.AddHours(1)));
        var options = new AzureOpenAIOptions();
        var sut = new EntraIDAuthHandler(credential, options) { InnerHandler = new CapturingHandler() };
        var invoker = new HttpMessageInvoker(sut);

        // Act
        var act = async () => await invoker.SendAsync(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullCredential_Throws()
    {
        // Arrange
        var options = new AzureOpenAIOptions();

        // Act
        var act = () => new EntraIDAuthHandler(null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // Arrange
        var credential = new FakeTokenCredential(new AccessToken("t", DateTimeOffset.UtcNow.AddHours(1)));

        // Act
        var act = () => new EntraIDAuthHandler(credential, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakeTokenCredential : TokenCredential
    {
        private readonly AccessToken _token;
        public int Calls { get; private set; }
        public string[] LastScopes { get; private set; } = Array.Empty<string>();

        public FakeTokenCredential(AccessToken token) => _token = token;

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Calls++;
            LastScopes = requestContext.Scopes;
            return _token;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Calls++;
            LastScopes = requestContext.Scopes;
            return new ValueTask<AccessToken>(_token);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
