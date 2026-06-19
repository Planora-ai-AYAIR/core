using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Planora.Application.Features.Parcels.Commands.SubmitTopographyJob
{
    public sealed class SubmitTopographyJobValidator : AbstractValidator<SubmitTopographyJobCommand>
    {
        public SubmitTopographyJobValidator()
        {
            RuleFor(x => x.ParcelId)
                .NotEmpty()
                .WithErrorCode("Parcel.Id.INVALID")
                .WithMessage("Parcel Id is required.");
        }
    }
}
