using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ResendOtp;

public sealed record ResendOtpCommand(Guid UserId) : IRequest<Response<ResendOtpResponse>>;
