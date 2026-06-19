using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Planora.Infrastructure.Options;

namespace Planora.Api.Filters;

public sealed class HmacSignatureFilter(
    IOptions<WebhookOptions> options,
    ILogger<HmacSignatureFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var opts = options.Value;

        if (string.IsNullOrWhiteSpace(opts.HmacSecret))
        {
            logger.LogError("Webhook HMAC secret is not configured.");
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var request = context.HttpContext.Request;
        request.EnableBuffering();

        byte[] body;
        using (var ms = new MemoryStream())
        {
            await request.Body.CopyToAsync(ms, context.HttpContext.RequestAborted);
            body = ms.ToArray();
        }
        request.Body.Position = 0;

        if (!request.Headers.TryGetValue(opts.SignatureHeader, out var signatureHeader) ||
            string.IsNullOrWhiteSpace(signatureHeader))
        {
            logger.LogWarning("Webhook request rejected: missing {Header}.", opts.SignatureHeader);
            context.Result = new UnauthorizedResult();
            return;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(opts.HmacSecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(body));
        var provided = signatureHeader.ToString().Trim();

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
        {
            logger.LogWarning("Webhook request rejected: signature mismatch.");
            context.Result = new UnauthorizedResult();
            return;
        }

        await next();
    }
}
