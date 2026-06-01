using MediatR;
using Planora.Application.Common.Helpers;
using Planora.Application.Features.Auth;
using Planora.Application.Interfaces.Repositories;
using Planora.Application.Interfaces.Repositories.DTOs;
using Planora.Application.Interfaces.Services;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Auth.Commands.Register;

public sealed class RegisterHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IOtpService otpService,
    IEmailService emailService) : IRequestHandler<RegisterCommand, Response<RegisterResponse>>
{
    public async Task<Response<RegisterResponse>> Handle(RegisterCommand request, CancellationToken ct)
    {
        var handler = new ResponseHandler();

        var existing = await userRepository.FindByEmailAsync(request.Email, ct);
        if (existing is not null)
        {
            return handler.BadRequest<RegisterResponse>("Email already exists.");
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneExists = await userRepository.FindByPhoneAsync(request.PhoneNumber, ct);
            if (phoneExists is not null)
            {
                return handler.BadRequest<RegisterResponse>("Phone number already exists.");
            }
        }

        var createRequest = new CreateUserRequest(
            request.Email,
            request.PhoneNumber,
            request.FirstName,
            request.LastName);

        var result = await userRepository.CreateAsync(createRequest, request.Password, ct);
        if (!result.Succeeded || result.User is null)
        {
            return handler.BadRequest<RegisterResponse>(string.Join(" ", result.Errors));
        }

        await roleRepository.EnsureRoleExistsAsync(AuthRoles.Client, ct);
        await userRepository.AddToRoleAsync(result.User.Id, AuthRoles.Client, ct);

        var otp = await otpService.GenerateAsync(result.User.Id, OtpPurposes.EmailVerification, TimeSpan.FromMinutes(10), ct);
        var displayName = EmailDisplayNameHelper.GetDisplayName(
            result.User.FirstName,
            result.User.LastName,
            result.User.Email);
        await emailService.SendOtpAsync(result.User.Email, displayName, otp, "Verify your email", ct);

        var response = new RegisterResponse(
            result.User.Id,
            result.User.Email,
            result.User.PhoneNumber,
            AuthRoles.Client,
            false);

        return handler.Created(response, "Registration successful. OTP sent.");
    }

}
