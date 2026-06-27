namespace Planora.Application.Interfaces.Services;

public interface IEmailService
{
	Task SendAsync(
		string to,
		string subject,
		string body,
		bool isHtml = true,
		CancellationToken ct = default);

	Task SendOtpAsync(
		string to,
		string username,
		string otp,
		string subject,
		CancellationToken ct = default);
}
