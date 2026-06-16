
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Planora.Infrastructure.Options;

namespace Planora.Api.Middlewares;

public sealed class WebhookSignatureMiddleware(
    RequestDelegate next,
    IOptions<AiOptions> options,
    ILogger<WebhookSignatureMiddleware> logger)
{
    private const string SignatureHeader = "X-Webhook-Signature";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        if (!context.Request.Headers.TryGetValue(SignatureHeader, out var signatureHeader))
        {
            logger.LogWarning("Webhook request missing {Header} header", SignatureHeader);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var receivedSignature = signatureHeader.ToString();

        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var secret = options.Value.SharedSecret;
        var expectedSignature = ComputeHmac(body, secret);

        if (!SignaturesMatch(receivedSignature, expectedSignature))
        {
            logger.LogWarning("Webhook signature mismatch");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static bool SignaturesMatch(string received, string expected)
    {
        var receivedBytes = Encoding.UTF8.GetBytes(received);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        if (receivedBytes.Length != expectedBytes.Length) return false;

        return CryptographicOperations.FixedTimeEquals(receivedBytes, expectedBytes);
    }
}