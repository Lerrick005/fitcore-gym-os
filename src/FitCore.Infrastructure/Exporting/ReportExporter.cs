using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FitCore.Application.Abstractions;
using FitCore.Application.Common;
using FitCore.Application.Exporting;
using FitCore.Application.Formatting;
using FitCore.Application.Members;
using FitCore.Application.Payments;
using FitCore.Application.Reports;
using FitCore.Application.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FitCore.Infrastructure.Exporting;

public sealed class ReportExporter : IReportExporter
{
    private readonly ISettingsService _settings;
    private readonly ICurrencyFormatter _money;
    private readonly IDateTimeFormatter _dates;

    static ReportExporter()
    {
        // QuestPDF Community licence — required for free use.
        // For a commercial product, verify your usage qualifies at https://www.questpdf.com/license/
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportExporter(ISettingsService settings, ICurrencyFormatter money, IDateTimeFormatter dates)
    {
        _settings = settings;
        _money = money;
        _dates = dates;
    }

    public async Task<Result<string>> ExportReportsToCsvAsync(ReportsData data, string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var sb = new StringBuilder();
            var locale = await _settings.GetAsync<LocaleSettings>(ct);
            var currency = data.RevenueLast30Days.FirstOrDefault()?.CurrencyCode ?? locale.CurrencyCode;

            // Header
            sb.AppendLine("FitCore Report");
            sb.AppendLine($"Generated,{DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            // Summary
            sb.AppendLine("SUMMARY");
            sb.AppendLine("Metric,Value");
            sb.AppendLine($"Revenue (last 30 days),{(data.RevenueLast30DaysTotal / 100m):0.00} {currency}");
            sb.AppendLine($"New members (last 30 days),{data.NewMembersLast30Days}");
            sb.AppendLine($"Active members,{data.ActiveMembers}");
            sb.AppendLine();

            // Revenue by day
            sb.AppendLine("REVENUE BY DAY");
            sb.AppendLine("Date,Amount");
            foreach (var r in data.RevenueLast30Days.OrderBy(x => x.Date))
                sb.AppendLine($"{r.Date:yyyy-MM-dd},{(r.AmountMinor / 100m):0.00}");
            sb.AppendLine();

            // Revenue by method
            sb.AppendLine("REVENUE BY METHOD");
            sb.AppendLine("Method,Amount,Count");
            foreach (var r in data.RevenueByMethodLast30Days)
                sb.AppendLine($"{Escape(r.Method)},{(r.AmountMinor / 100m):0.00},{r.Count}");
            sb.AppendLine();

            // Expiring soon
            sb.AppendLine("EXPIRING IN NEXT 30 DAYS");
            sb.AppendLine("Member Number,Full Name,Phone,Plan,End Date");
            foreach (var e in data.ExpiringInNext30Days)
                sb.AppendLine($"{Escape(e.MemberNumber)},{Escape(e.FullName)},{Escape(e.Phone)},{Escape(e.PlanName)},{e.EndDate:yyyy-MM-dd}");

            await File.WriteAllTextAsync(destinationPath, sb.ToString(), Encoding.UTF8, ct);
            return Result<string>.Ok(destinationPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("export.csv.failed", ex.Message);
        }
    }

    public async Task<Result<string>> ExportReportsToPdfAsync(ReportsData data, string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var branding = await _settings.GetAsync<BrandingSettings>(ct);
            var business = await _settings.GetAsync<BusinessSettings>(ct);
            var locale = await _settings.GetAsync<LocaleSettings>(ct);
            var currency = data.RevenueLast30Days.FirstOrDefault()?.CurrencyCode ?? locale.CurrencyCode;
            var now = DateTime.Now;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor("#141210"));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Text(branding.BusinessName ?? "FitCore")
                            .FontSize(20).Bold();
                        if (!string.IsNullOrWhiteSpace(business.PhonePrimary) || !string.IsNullOrWhiteSpace(business.Email))
                            col.Item().Text($"{business.PhonePrimary}  •  {business.Email}")
                                .FontSize(9).FontColor("#5C544A");
                        if (!string.IsNullOrWhiteSpace(business.PhysicalAddress))
                            col.Item().Text(business.PhysicalAddress).FontSize(9).FontColor("#5C544A");
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#B8451F");
                        col.Item().PaddingTop(4).Text($"Reports Snapshot — {now:dd/MM/yyyy HH:mm}")
                            .FontSize(10).FontColor("#5C544A");
                    });

                    // Content
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Spacing(12);

                        // Summary
                        col.Item().Text("Summary").FontSize(14).Bold().FontColor("#B8451F");
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#E8E1D2").Padding(10).Column(c =>
                            {
                                c.Item().Text("Revenue (30d)").FontSize(8).FontColor("#5C544A");
                                c.Item().Text($"{currency} {(data.RevenueLast30DaysTotal / 100m):N2}").FontSize(16).Bold();
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Border(1).BorderColor("#E8E1D2").Padding(10).Column(c =>
                            {
                                c.Item().Text("New Members (30d)").FontSize(8).FontColor("#5C544A");
                                c.Item().Text(data.NewMembersLast30Days.ToString()).FontSize(16).Bold();
                            });
                            row.ConstantItem(8);
                            row.RelativeItem().Border(1).BorderColor("#E8E1D2").Padding(10).Column(c =>
                            {
                                c.Item().Text("Active Members").FontSize(8).FontColor("#5C544A");
                                c.Item().Text(data.ActiveMembers.ToString()).FontSize(16).Bold();
                            });
                        });

                        // Revenue by method
                        col.Item().PaddingTop(8).Text("Revenue by method").FontSize(14).Bold().FontColor("#B8451F");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Background("#F1EBDF").Padding(6).Text("Method").Bold();
                                h.Cell().Background("#F1EBDF").Padding(6).Text("Amount").Bold();
                                h.Cell().Background("#F1EBDF").Padding(6).Text("Count").Bold();
                            });
                            foreach (var r in data.RevenueByMethodLast30Days)
                            {
                                t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(r.Method);
                                t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text($"{currency} {(r.AmountMinor / 100m):N2}");
                                t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(r.Count.ToString());
                            }
                        });

                        // Expiring soon
                        col.Item().PaddingTop(8).Text("Expiring in next 30 days").FontSize(14).Bold().FontColor("#B8451F");
                        if (data.ExpiringInNext30Days.Count == 0)
                        {
                            col.Item().Text("No memberships expiring in the next 30 days.").Italic().FontColor("#8C8377");
                        }
                        else
                        {
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(2);
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Background("#F1EBDF").Padding(6).Text("Number").Bold();
                                    h.Cell().Background("#F1EBDF").Padding(6).Text("Member").Bold();
                                    h.Cell().Background("#F1EBDF").Padding(6).Text("Plan").Bold();
                                    h.Cell().Background("#F1EBDF").Padding(6).Text("Ends").Bold();
                                });
                                foreach (var e in data.ExpiringInNext30Days)
                                {
                                    t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(e.MemberNumber);
                                    t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(e.FullName);
                                    t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(e.PlanName);
                                    t.Cell().BorderBottom(1).BorderColor("#E8E1D2").Padding(6).Text(e.EndDate.ToString("dd/MM/yyyy"));
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Powered by FitCore").FontSize(8).FontColor("#8C8377");
                        t.Span("   •   Page ").FontSize(8).FontColor("#8C8377");
                        t.CurrentPageNumber().FontSize(8).FontColor("#8C8377");
                        t.Span(" of ").FontSize(8).FontColor("#8C8377");
                        t.TotalPages().FontSize(8).FontColor("#8C8377");
                    });
                });
            }).GeneratePdf(destinationPath);

            return Result<string>.Ok(destinationPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("export.pdf.failed", ex.Message);
        }
    }

    public async Task<Result<string>> ExportMembersToCsvAsync(IReadOnlyList<MemberListItem> members, string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Member Number,Full Name,Phone,Email,Status,Archived");
            foreach (var m in members)
            {
                sb.AppendLine($"{Escape(m.MemberNumber)},{Escape(m.FullName)},{Escape(m.Phone)},{Escape(m.Email ?? "")},{Escape(m.Status)},{(m.ArchivedAtUtc.HasValue ? m.ArchivedAtUtc.Value.ToString("yyyy-MM-dd") : "")}");
            }
            await File.WriteAllTextAsync(destinationPath, sb.ToString(), Encoding.UTF8, ct);
            return Result<string>.Ok(destinationPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("export.csv.failed", ex.Message);
        }
    }

    public async Task<Result<string>> ExportPaymentsToCsvAsync(IReadOnlyList<PaymentListItem> payments, string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Amount,Currency,Method,Reference,Status");
            foreach (var p in payments)
            {
                sb.AppendLine($"{p.ReceivedAtUtc:yyyy-MM-dd HH:mm:ss},{(p.AmountMinor / 100m):0.00},{Escape(p.CurrencyCode)},{Escape(p.Method)},{Escape(p.Reference ?? "")},{Escape(p.Status)}");
            }
            await File.WriteAllTextAsync(destinationPath, sb.ToString(), Encoding.UTF8, ct);
            return Result<string>.Ok(destinationPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("export.csv.failed", ex.Message);
        }
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}

