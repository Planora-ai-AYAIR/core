using FluentValidation;
using Planora.Application.Features.Analysis.Dtos.StartAnalysis;

namespace Planora.Application.Features.Analysis.Commands.StartAnalysis;

public sealed class StartAnalysisValidator : AbstractValidator<StartAnalysisCommand>
{
    public StartAnalysisValidator()
    {
        RuleFor(x => x.ParcelId)
            .NotEmpty()
            .WithErrorCode("Parcel.Id.INVALID")
            .WithMessage("Parcel Id is required.");

        RuleFor(x => x.Options)
            .Must(o => o.IncludeTopography || o.IncludeSoil || o.IncludeBearing || o.IncludeRisk || o.IncludeBorehole)
            .WithErrorCode("Analysis.Options.NO_MODULES")
            .WithMessage("At least one analysis module must be enabled.");
    }
}
