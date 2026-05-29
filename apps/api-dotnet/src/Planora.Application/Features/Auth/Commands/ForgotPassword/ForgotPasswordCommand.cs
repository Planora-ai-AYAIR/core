using MediatR;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string? Email, string? PhoneNumber)
    : IRequest<Response<ForgotPasswordResponse>>;
