// Planora.Infrastructure/Services/Reporting/QuestPdfReportGenerator.cs
using Microsoft.Extensions.Configuration;
using Planora.Application.Features.Reports.Dtos;
using Planora.Application.Features.Reports.Dtos.SubmitPdfJob;
using Planora.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Planora.Infrastructure.Services.Reporting;

public sealed class QuestPdfReportGenerator : IPdfGeneratorService
{
    // ----- Brand palette (taken from the Planora logo) -----
    private const string Primary = "#C96E4B";    // terracotta / orange
    private const string PrimaryDark = "#8A4A35"; // darker accent
    private const string Secondary = "#8A6660";   // muted brown
    private const string TextDark = "#222222";
    private const string TextMuted = "#6B6B6B";
    private const string Light = "#F6F1EE";
    private const string BorderLight = "#E3DAD5";

    private readonly string? _logoPath;

    public QuestPdfReportGenerator(IConfiguration? configuration = null)
    {
        // Resolve the logo from disk (Assets/planora-logo.jpg) instead of an
        // embedded resource, since the file in the project is a .jpg, not a .png,
        // and is not currently marked as an Embedded Resource.
        var configuredPath = configuration?["Assets:LogoPath"];

        _logoPath = !string.IsNullOrWhiteSpace(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, "Assets", "planora-logo.jpg");
    }

    public Task<byte[]> GenerateAsync(ReportPdfData data, CancellationToken ct)
    {
        var document = Document.Create(container =>
        {
            // ---------- Cover page ----------
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextDark));

                page.Content().Element(c => ComposeCoverPage(c, data));
            });

            // ---------- Content pages ----------
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextDark));

                page.Header().Element(header => ComposeHeader(header, data));

                page.Content().PaddingVertical(0.6f, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Element(c => ComposeExecutiveSummary(c, data));

                    if (data.Analysis.Topography is not null)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeTopography(c, data.Analysis.Topography, data.IncludeMaps));
                    }

                    if (data.Analysis.Soil is not null)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeSoil(c, data.Analysis.Soil, data.Analysis.Bearing));
                    }

                    if (data.Analysis.Risk is not null && data.IncludeRiskMatrix)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeRisk(c, data.Analysis.Risk));
                    }

                    if (data.Analysis.Borehole is not null && data.IncludeBoreholePlan)
                    {
                        column.Item().PageBreak();
                        column.Item().Element(c => ComposeBorehole(c, data.Analysis.Borehole));
                    }

                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeRecommendations(c, data));

                    column.Item().PageBreak();
                    column.Item().Element(c => ComposeDisclaimer(c, data));
                });

                page.Footer().Element(footer => ComposeFooter(footer));
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return Task.FromResult(stream.ToArray());
    }

    // ===================================================================
    // COVER PAGE
    // ===================================================================
    private void ComposeCoverPage(IContainer container, ReportPdfData data)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().PaddingTop(120);

            if (HasLogo())
            {
                column.Item().AlignCenter().Height(110).Image(_logoPath!);
            }

            column.Item().PaddingTop(30).AlignCenter()
                .Text("GEOTECHNICAL SITE ANALYSIS REPORT")
                .FontSize(24).Bold().FontColor(PrimaryDark);

            column.Item().PaddingTop(8).AlignCenter()
                .Text("Site Intelligence for Africa")
                .FontSize(12).Italic().FontColor(TextMuted);

            column.Item().PaddingTop(60).AlignCenter().Width(280).Element(box =>
            {
                box.Border(1).BorderColor(BorderLight).Background(Light).Padding(16).Column(col =>
                {
                    if (!string.IsNullOrWhiteSpace(data.CompanyName))
                    {
                        col.Item().AlignCenter().Text(data.CompanyName).FontSize(14).Bold().FontColor(TextDark);
                    }

                    if (!string.IsNullOrWhiteSpace(data.ProjectName))
                    {
                        col.Item().PaddingTop(4).AlignCenter().Text(data.ProjectName).FontSize(11).FontColor(TextMuted);
                    }

                    col.Item().PaddingTop(12).AlignCenter()
                        .Text($"Report Reference: {data.ReportId.ToString()[..8].ToUpperInvariant()}")
                        .FontSize(9).FontColor(TextMuted);

                    col.Item().AlignCenter()
                        .Text($"Date Issued: {DateTime.UtcNow:dd MMMM yyyy}")
                        .FontSize(9).FontColor(TextMuted);
                });
            });

            column.Item().PaddingTop(200).AlignCenter()
                .Text("Confidential — Prepared exclusively for the named client")
                .FontSize(9).Italic().FontColor(TextMuted);
        });
    }

    private bool HasLogo() => !string.IsNullOrWhiteSpace(_logoPath) && File.Exists(_logoPath);

    // ===================================================================
    // HEADER (content pages)
    // ===================================================================
    private void ComposeHeader(IContainer container, ReportPdfData data)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (HasLogo())
                {
                    row.ConstantItem(110).Height(40).Image(_logoPath!);
                }
                else
                {
                    row.ConstantItem(110);
                }

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("Geotechnical Site Analysis Report")
                        .FontSize(10).SemiBold().FontColor(PrimaryDark);

                    col.Item().Text($"Reference {data.ReportId.ToString()[..8].ToUpperInvariant()}  •  {DateTime.UtcNow:dd MMM yyyy}")
                        .FontSize(8).FontColor(TextMuted);
                });
            });

            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Primary);
        });
    }

    // ===================================================================
    // EXECUTIVE SUMMARY
    // ===================================================================
    private void ComposeExecutiveSummary(IContainer container, ReportPdfData data)
    {
        container.Column(column =>
        {
            SectionTitle(column.Item(), "Executive Summary");

            column.Item().PaddingTop(15).Row(row =>
            {
                row.Spacing(10);

                row.RelativeItem().Element(c => MetricCard(
                    c,
                    "Overall Risk",
                    data.Analysis.Risk?.OverallRiskLevel ?? "N/A",
                    data.Analysis.Risk is not null ? GetRiskColor(data.Analysis.Risk.OverallRiskScore) : TextMuted));

                row.RelativeItem().Element(c => MetricCard(
                    c,
                    "Soil Type",
                    data.Analysis.Soil?.PrimaryType ?? "N/A",
                    Primary));

                row.RelativeItem().Element(c => MetricCard(
                    c,
                    "Elevation Range",
                    data.Analysis.Topography is not null
                        ? $"{data.Analysis.Topography.ElevationMin:F0}–{data.Analysis.Topography.ElevationMax:F0} m"
                        : "N/A",
                    Primary));

                row.RelativeItem().Element(c => MetricCard(
                    c,
                    "Recommended Boreholes",
                    data.Analysis.Borehole?.MinimumRequired.ToString() ?? "N/A",
                    Primary));
            });

            column.Item().PaddingTop(20).Text(
                "This report summarises the findings of the automated geospatial and geotechnical " +
                "pre-construction analysis carried out for the parcel referenced above. It is intended " +
                "to support early-stage planning decisions and should be read alongside the detailed " +
                "sections that follow.")
                .FontSize(10).FontColor(TextMuted).LineHeight(1.4f);
        });
    }

    private void MetricCard(IContainer container, string title, string value, string accentColor)
    {
        container
            .Border(1)
            .BorderColor(BorderLight)
            .Background(Light)
            .Padding(12)
            .Column(col =>
            {
                col.Item().Text(title).FontSize(9).FontColor(TextMuted);
                col.Item().PaddingTop(4).Text(value).FontSize(15).Bold().FontColor(accentColor);
            });
    }

    // ===================================================================
    // TOPOGRAPHY
    // ===================================================================
    private void ComposeTopography(IContainer container, PdfTopographyData topo, bool includeMaps)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "1. Topography Analysis");

            col.Item().PaddingTop(15).Text("Elevation Statistics").FontSize(12).SemiBold().FontColor(TextDark);

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Minimum");
                    HeaderCell(header.Cell(), "Maximum");
                    HeaderCell(header.Cell(), "Average");
                });

                BodyCell(table.Cell(), $"{topo.ElevationMin:F1} m");
                BodyCell(table.Cell(), $"{topo.ElevationMax:F1} m");
                BodyCell(table.Cell(), $"{topo.ElevationMean:F1} m");
            });

            col.Item().PaddingTop(18).Text("Cut & Fill Analysis").FontSize(12).SemiBold().FontColor(TextDark);

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Cut Volume");
                    HeaderCell(header.Cell(), "Fill Volume");
                    HeaderCell(header.Cell(), "Net Volume");
                });

                BodyCell(table.Cell(), $"{topo.CutVolume:N0} m³");
                BodyCell(table.Cell(), $"{topo.FillVolume:N0} m³");
                BodyCell(
                    table.Cell(),
                    $"{topo.NetVolume:N0} m³",
                    topo.NetVolume < 0 ? "#2E7D32" : "#C62828");
            });

            if (topo.PondingZonesCount.HasValue && topo.PondingZonesCount.Value > 0)
            {
                col.Item().PaddingTop(15).Element(c => Callout(
                    c,
                    "Drainage Notice",
                    $"{topo.PondingZonesCount} potential ponding zone(s) detected, covering approximately {topo.PondingTotalArea:F0} m² of the site.",
                    "#B45309"));
            }

            if (includeMaps && !string.IsNullOrEmpty(topo.ElevationTileUrl))
            {
                col.Item().PaddingTop(18).Text("Elevation Map").FontSize(12).SemiBold().FontColor(TextDark);
                col.Item().PaddingTop(6).Height(190).Background(Light).Border(1).BorderColor(BorderLight)
                    .AlignCenter().AlignMiddle()
                    .Text("Elevation map available in the online dashboard")
                    .FontSize(9).FontColor(TextMuted);
            }
        });
    }

    // ===================================================================
    // SOIL
    // ===================================================================
    private void ComposeSoil(IContainer container, PdfSoilData soil, PdfBearingData? bearing)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "2. Soil Analysis");

            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Primary Soil Type: ").FontSize(11).FontColor(TextMuted);
                    text.Span(soil.PrimaryType).FontSize(11).Bold().FontColor(PrimaryDark);
                });

                row.RelativeItem().AlignRight().Text(
                    $"AI Confidence: {soil.AiConfidence:P0}")
                    .FontSize(10).FontColor("#2E7D32");
            });

            col.Item().Text($"USDA Classification: {soil.UsdaClass}").FontSize(9).FontColor(TextMuted);

            col.Item().PaddingTop(18).Text("Particle Composition").FontSize(12).SemiBold().FontColor(TextDark);
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Sand");
                    HeaderCell(header.Cell(), "Silt");
                    HeaderCell(header.Cell(), "Clay");
                });

                BodyCell(table.Cell(), $"{soil.SandPercent:F1}%");
                BodyCell(table.Cell(), $"{soil.SiltPercent:F1}%");
                BodyCell(table.Cell(), $"{soil.ClayPercent:F1}%");
            });

            col.Item().PaddingTop(18).Text("Soil Properties").FontSize(12).SemiBold().FontColor(TextDark);
            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                LabelCell(table.Cell(), "Bulk Density");
                BodyCell(table.Cell(), $"{soil.BulkDensity:F2} g/cm³");

                LabelCell(table.Cell(), "Organic Carbon");
                BodyCell(table.Cell(), $"{soil.OrganicCarbon:F2}%");

                LabelCell(table.Cell(), "pH Level");
                BodyCell(table.Cell(), $"{soil.Ph:F1}");

                if (bearing is not null)
                {
                    LabelCell(table.Cell(), "Bearing Capacity");
                    BodyCell(table.Cell(), $"{bearing.BearingCapacityKpa:F0} kPa ({bearing.Classification})");
                }
            });

            // NOTE: raw JSON fields (e.g. MultiDepthProfileJson) are intentionally
            // NOT printed in the client report — they are internal data only.
            if (!string.IsNullOrEmpty(soil.MultiDepthProfileJson))
            {
                col.Item().PaddingTop(15).Element(c => Callout(
                    c,
                    "Additional Data",
                    "A detailed multi-depth soil profile was generated for this site and is available on request.",
                    Secondary));
            }
        });
    }

    // ===================================================================
    // RISK
    // ===================================================================
    private void ComposeRisk(IContainer container, PdfRiskData risk)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "3. Risk Assessment");

            col.Item().PaddingTop(15).Border(1).BorderColor(GetRiskColor(risk.OverallRiskScore))
                .Background(Light).Padding(16).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Overall Risk Score").FontSize(9).FontColor(TextMuted);
                        c.Item().PaddingTop(2).Text($"{risk.OverallRiskScore}/100")
                            .FontSize(26).Bold().FontColor(GetRiskColor(risk.OverallRiskScore));
                    });

                    row.RelativeItem().AlignRight().AlignMiddle()
                        .Text(risk.OverallRiskLevel ?? "Unknown")
                        .FontSize(16).Bold().FontColor(GetRiskColor(risk.OverallRiskScore));
                });

            col.Item().PaddingTop(18).Text("Risk Breakdown").FontSize(12).SemiBold().FontColor(TextDark);

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(140);
                    c.RelativeColumn();
                    c.ConstantColumn(70);
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Risk Type");
                    HeaderCell(header.Cell(), "Level");
                    HeaderCell(header.Cell(), "Score");
                });

                // PdfRiskData has no explicit "FloodLevel" field (only a score
                // and a GeoJSON url), so we derive a label from the score itself.
                AddRiskRow(table, "Flood", GetRiskLabel(risk.FloodRiskScore), risk.FloodRiskScore);
                AddRiskRow(table, "Seismic", risk.SeismicLevel, risk.SeismicRiskScore);
                AddRiskRow(table, "Expansive Soil", risk.ExpansiveSoilLevel, risk.ExpansiveSoilRisk);
                AddRiskRow(table, "Liquefaction", risk.LiquefactionLevel, risk.LiquefactionRisk);
            });

            if (risk.ReplacementDepth.HasValue)
            {
                col.Item().PaddingTop(15).Element(c => Callout(
                    c,
                    "Recommended Mitigation",
                    $"Replace the top {risk.ReplacementDepth:F1} m of soil with non-expansive granular fill prior to foundation works.",
                    "#B45309"));
            }
        });
    }

    private void AddRiskRow(TableDescriptor table, string label, string? level, int score)
    {
        LabelCell(table.Cell(), label);
        BodyCell(table.Cell(), level ?? "N/A");
        BodyCell(table.Cell(), $"{score}/100", GetRiskColor(score));
    }

    // ===================================================================
    // BOREHOLE
    // ===================================================================
    private void ComposeBorehole(IContainer container, PdfBoreholeData borehole)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "4. Borehole Plan");

            col.Item().PaddingTop(15).Row(row =>
            {
                row.Spacing(10);
                row.RelativeItem().Element(c => MetricCard(c, "Minimum Required", $"{borehole.MinimumRequired}", Primary));
                row.RelativeItem().Element(c => MetricCard(c, "Optimal Count", $"{borehole.OptimalCount}", Primary));
                row.RelativeItem().Element(c => MetricCard(c, "Coverage", $"{borehole.CoveragePercentage:F0}%", Primary));
                row.RelativeItem().Element(c => MetricCard(c, "Grid Size", borehole.GridSize ?? "N/A", Primary));
            });

            col.Item().PaddingTop(20).Text("Cost Comparison").FontSize(12).SemiBold().FontColor(TextDark);

            col.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Approach");
                    HeaderCell(header.Cell(), "Boreholes");
                    HeaderCell(header.Cell(), "Estimated Cost");
                });

                // PdfBoreholeData has no Currency property — amounts are shown
                // as plain figures. If a currency code becomes available later,
                // append it here instead of hardcoding one.
                BodyCell(table.Cell(), "Traditional");
                BodyCell(table.Cell(), borehole.TraditionalBoreholeCount.ToString());
                BodyCell(table.Cell(), $"{borehole.TraditionalEstimatedCost:N0}");

                BodyCell(table.Cell(), "Planora-Optimised", bold: true);
                BodyCell(table.Cell(), borehole.OptimizedBoreholeCount.ToString(), bold: true);
                BodyCell(table.Cell(), $"{borehole.OptimizedEstimatedCost:N0}", "#2E7D32", bold: true);

                BodyCell(table.Cell(), "Savings", bold: true);
                BodyCell(table.Cell(), $"{borehole.SavingsPercentage:F0}%", bold: true);
                BodyCell(table.Cell(), $"{borehole.SavingsAmount:N0}", "#2E7D32", bold: true);
            });

            // Raw coordinate JSON is internal only — summarised instead.
            if (!string.IsNullOrEmpty(borehole.PlacementPointsJson))
            {
                col.Item().PaddingTop(15).Element(c => Callout(
                    c,
                    "Placement Plan",
                    "Detailed borehole coordinates have been generated and are available in the project dashboard for the site team.",
                    Secondary));
            }
        });
    }

    // ===================================================================
    // RECOMMENDATIONS
    // ===================================================================
    private void ComposeRecommendations(IContainer container, ReportPdfData data)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "Recommendations");

            col.Item().PaddingTop(15).Column(list =>
            {
                list.Spacing(8);

                if (data.Analysis.Risk?.OverallRiskScore > 60)
                {
                    list.Item().Element(c => BulletPoint(c, "Commission a detailed on-site geotechnical investigation before finalising design."));
                }

                if (data.Analysis.Borehole is not null)
                {
                    list.Item().Element(c => BulletPoint(c, $"Plan for a minimum of {data.Analysis.Borehole.MinimumRequired} boreholes across the parcel."));
                }

                if (data.Analysis.Soil is not null)
                {
                    list.Item().Element(c => BulletPoint(c, $"Base foundation design assumptions on {data.Analysis.Soil.PrimaryType} soil conditions."));
                }

                if (data.Analysis.Topography?.PondingZonesCount is > 0)
                {
                    list.Item().Element(c => BulletPoint(c, "Incorporate surface drainage improvements to mitigate identified ponding risk."));
                }

                list.Item().Element(c => BulletPoint(c, "Treat all findings as preliminary; confirm with licensed geotechnical professionals before construction."));
            });
        });
    }

    private void BulletPoint(IContainer container, string text)
    {
        container.Row(row =>
        {
            row.ConstantItem(14).Text("•").FontColor(Primary).Bold();
            row.RelativeItem().Text(text).FontSize(10).FontColor(TextDark).LineHeight(1.4f);
        });
    }

    // ===================================================================
    // DISCLAIMER
    // ===================================================================
    private void ComposeDisclaimer(IContainer container, ReportPdfData data)
    {
        container.Column(col =>
        {
            SectionTitle(col.Item(), "Disclaimer", "#9C3B25");

            var disclaimerText = data.DisclaimerLevel switch
            {
                "full" =>
                    "This report is generated by an automated AI system for preliminary planning purposes only. " +
                    "All findings should be verified by qualified geotechnical engineers before construction. " +
                    "Planora AI and its affiliates assume no liability for decisions made based on this report. " +
                    "Site-specific conditions may vary significantly from AI predictions, and physical borehole " +
                    "verification is mandatory before foundation design.",
                "brief" =>
                    "This is an AI-generated report intended for preliminary planning. Findings must be verified " +
                    "by a qualified professional before use in construction decisions.",
                _ =>
                    "This report reflects an AI-generated estimate for preliminary planning only."
            };

            col.Item().PaddingTop(12).Background(Light).Border(1).BorderColor(BorderLight).Padding(14)
                .Text(disclaimerText).FontSize(9).Italic().FontColor(TextMuted).LineHeight(1.5f);
        });
    }

    // ===================================================================
    // FOOTER
    // ===================================================================
    private void ComposeFooter(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(BorderLight);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("Confidential — Planora Site Intelligence")
                    .FontSize(8).FontColor(TextMuted);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
    }

    // ===================================================================
    // SHARED HELPERS
    // ===================================================================

    /// <summary>
    /// Renders a section heading with an underline. Padding/border MUST be applied
    /// on the container BEFORE calling .Text(), because PaddingTop/BorderBottom etc.
    /// are extension methods on IContainer, not on TextBlockDescriptor — chaining
    /// them directly after .Text(...) is what produced the original compiler errors.
    /// </summary>
    private void SectionTitle(IContainer container, string title, string? color = null)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(17).Bold().FontColor(color ?? PrimaryDark);
            col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(color ?? Primary);
        });
    }

    private void HeaderCell(IContainer container, string text)
    {
        container.Background(Primary).Padding(6)
            .Text(text).FontSize(9).Bold().FontColor("#FFFFFF");
    }

    private void BodyCell(IContainer container, string text, string? color = null, bool bold = false)
    {
        var cell = container.BorderBottom(1).BorderColor(BorderLight).Padding(6);
        var t = cell.Text(text).FontSize(9.5f).FontColor(color ?? TextDark);
        if (bold) t.Bold();
    }

    private void LabelCell(IContainer container, string text)
    {
        container.BorderBottom(1).BorderColor(BorderLight).Padding(6)
            .Text(text).FontSize(9.5f).FontColor(TextMuted);
    }

    private void Callout(IContainer container, string title, string message, string accentColor)
    {
        container.Border(1).BorderColor(accentColor).Background(Light).Padding(12).Column(col =>
        {
            col.Item().Text(title).FontSize(10).Bold().FontColor(accentColor);
            col.Item().PaddingTop(4).Text(message).FontSize(9.5f).FontColor(TextDark).LineHeight(1.4f);
        });
    }

    private static string GetRiskColor(int score) => score switch
    {
        < 30 => "#2E7D32",
        < 60 => "#B45309",
        _ => "#C62828"
    };

    /// <summary>
    /// Used for risk dimensions (e.g. Flood) that only carry a numeric score
    /// with no separate "level" string in the DTO.
    /// </summary>
    private static string GetRiskLabel(int score) => score switch
    {
        < 30 => "Low",
        < 60 => "Moderate",
        _ => "High"
    };
}