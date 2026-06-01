using System.IO;
using FluentEmail.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Planora.Application.Interfaces.Services;

namespace Planora.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<EmailService> _logger;
    private readonly string _otpTemplate;

    public EmailService(IFluentEmail fluentEmail, ILogger<EmailService> logger, IHostEnvironment hostEnvironment)
    {
        _fluentEmail = fluentEmail;
        _logger = logger;
        _otpTemplate = LoadOtpTemplate(hostEnvironment);
    }

    public async Task SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new ArgumentException("Recipient email is required.", nameof(to));
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Email subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Email body is required.", nameof(body));
        }

        await SendInternalAsync(to, subject, body, isHtml, ct);
    }

    public Task SendOtpAsync(
        string to,
        string username,
        string otp,
        string subject,
        CancellationToken ct = default)
    {
        var body = _otpTemplate
            .Replace("{Username}", username)
            .Replace("{OtpCode}", otp)
            .Replace("{CurrentYear}", DateTime.UtcNow.Year.ToString());

        return SendInternalAsync(to, subject, body, true, ct);
    }

    private async Task SendInternalAsync(
        string to,
        string subject,
        string body,
        bool isHtml,
        CancellationToken ct)
    {
        var sendResult = await _fluentEmail
            .To(to)
            .Subject(subject)
            .Body(body, isHtml)
            .SendAsync(ct);

        if (!sendResult.Successful)
        {
            _logger.LogError(
                "Failed to send email to {Recipient}. Errors: {Errors}",
                to,
                string.Join(", ", sendResult.ErrorMessages));
            throw new Exception("Failed to send email.");
        }
    }

    private string LoadOtpTemplate(IHostEnvironment hostEnvironment)
    {
        var templatePath = Path.GetFullPath(Path.Combine(
            hostEnvironment.ContentRootPath,
            "..",
            "Planora.Infrastructure",
            "Templates",
            "OtpVerficationEmail.html"));

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("OTP email template not found at {Path}. Using fallback template.", templatePath);
            return "<p>Hi {Username},</p><p>Your OTP is: <strong>{OtpCode}</strong></p>";
        }

        return File.ReadAllText(templatePath);
    }
}
