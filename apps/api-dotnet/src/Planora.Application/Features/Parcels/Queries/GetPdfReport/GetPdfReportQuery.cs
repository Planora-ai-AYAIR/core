using MediatR;
using Planora.Application.Features.Parcels.Dtos.PdfReport;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Queries.GetPdfReport;

public sealed record GetPdfReportQuery(
    Guid ParcelId
) : IRequest<Result<PdfReportResponse>>;
