using Microsoft.Extensions.Options;
using Planora.Infrastructure.Options;
using System.Net.Http;

namespace Planora.Infrastructure.Http;

public class AiApiKeyHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public AiApiKeyHandler(IOptions<AiOptions> options)
    {
        _apiKey = options.Value.ApiKey;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Add("X-Api-Key", _apiKey);
        return base.SendAsync(request, ct);
    }
}