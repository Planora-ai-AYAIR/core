using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Auth.Errors;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Repositories.DTOs;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IOtpService otpService,
    IEmailService emailService) : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var existing = await userRepository.FindByEmailAsync(request.Email, ct);
        if (existing is not null)
            return AuthErrors.EmailAlreadyExists;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneExists = await userRepository.FindByPhoneAsync(request.PhoneNumber, ct);
            if (phoneExists is not null)
                return AuthErrors.PhoneAlreadyExists;
        }

        var createRequest = new CreateUserRequest(request.Email, request.PhoneNumber, request.FirstName, request.LastName);
        var result = await userRepository.CreateAsync(createRequest, request.Password, ct);

        if (!result.Succeeded || result.User is null)
            return Error.Validation("Auth.RegistrationFailed", string.Join(" ", result.Errors));

        await roleRepository.EnsureRoleExistsAsync(AuthRoles.Client, ct);
        await userRepository.AddToRoleAsync(result.User.Id, AuthRoles.Client, ct);

        var otp = await otpService.GenerateAsync(result.User.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
        var displayName = EmailDisplayNameHelper.GetDisplayName(result.User.FirstName, result.User.LastName, result.User.Email);

        await emailService.SendOtpAsync(result.User.Email, displayName, otp, "Verify your email", ct);

        return new RegisterResponse(result.User.Id, result.User.Email, result.User.PhoneNumber, AuthRoles.Client, false);
    }
}