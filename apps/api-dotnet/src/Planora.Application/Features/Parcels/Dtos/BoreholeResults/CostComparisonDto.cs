namespace Planora.Application.Features.Parcels.Dtos.BoreholeResults;

public sealed record CostComparisonDto(
    int TraditionalBoreholeCount,
    decimal TraditionalEstimatedCost,
    int OptimizedBoreholeCount,
    decimal OptimizedEstimatedCost,
    decimal SavingsAmount,
    double SavingsPercentage,
    string Currency
);
