using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(Guid UserId, string Otp) : IRequest<Response<VerifyOtpResponse>>;
