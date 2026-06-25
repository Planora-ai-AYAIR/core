using FluentValidation;

namespace Planora.Application.Features.Parcels.Commands.DeleteParcel;

public sealed class DeleteParcelCommandValidator : AbstractValidator<DeleteParcelCommand>
{
    public DeleteParcelCommandValidator()
    {
        RuleFor(x => x.ParcelId)
            .NotEmpty()
            .WithMessage("Parcel ID is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}