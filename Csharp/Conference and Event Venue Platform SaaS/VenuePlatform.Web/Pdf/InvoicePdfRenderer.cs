using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VenuePlatform.BLL.Domain.Billing;
using VenuePlatform.BLL.Domain.Bookings;

namespace VenuePlatform.Web.Pdf;

/// <summary>
/// Renders an invoice as a PDF document using QuestPDF.
/// </summary>
public static class InvoicePdfRenderer
{
    public static byte[] RenderInvoicePdf(
        string companyName,
        string companySlug,
        Invoice invoice,
        string clientName,
        Booking booking,
        IReadOnlyList<InvoiceItem> items)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                // Header: Invoice Title and Company
                page.Header().Column(column =>
                {
                    column.Item().Text($"Invoice {invoice.InvoiceNumberText}")
                        .FontSize(24)
                        .Bold();

                    column.Item().PaddingTop(8);

                    column.Item().Text(companyName)
                        .FontSize(14)
                        .SemiBold();
                });

                // Content
                page.Content().PaddingVertical(20).Column(column =>
                {
                    // Booking Info
                    column.Item().PaddingBottom(16).Column(bookingCol =>
                    {
                        bookingCol.Item().Text("Booking Information").Bold();
                        bookingCol.Item().PaddingTop(4);
                        bookingCol.Item().Text($"Title: {booking.Title}");
                        bookingCol.Item().Text($"Start: {booking.StartUtc:yyyy-MM-dd HH:mm} UTC");
                        bookingCol.Item().Text($"End: {booking.EndUtc:yyyy-MM-dd HH:mm} UTC");
                    });

                    // Client Info
                    column.Item().PaddingBottom(16).Column(clientCol =>
                    {
                        clientCol.Item().Text("Client").Bold();
                        clientCol.Item().PaddingTop(4);
                        clientCol.Item().Text(clientName);
                    });

                    // Status Fields
                    column.Item().PaddingBottom(16).Column(statusCol =>
                    {
                        statusCol.Item().Text("Status").Bold();
                        statusCol.Item().PaddingTop(4);
                        statusCol.Item().Text($"Status: {invoice.Status}");
                        statusCol.Item().Text($"Issued: {invoice.IssuedUtc:yyyy-MM-dd HH:mm} UTC");

                        if (invoice.SentUtc.HasValue)
                        {
                            statusCol.Item().Text($"Sent: {invoice.SentUtc.Value:yyyy-MM-dd HH:mm} UTC");
                        }

                        if (invoice.PaidUtc.HasValue)
                        {
                            statusCol.Item().Text($"Paid: {invoice.PaidUtc.Value:yyyy-MM-dd HH:mm} UTC");
                        }
                    });

                    // Items Table
                    column.Item().PaddingBottom(8).Text("Items").Bold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Description").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Line Total").Bold();
                        });

                        // Rows
                        foreach (var item in items)
                        {
                            table.Cell().Element(CellStyle).Text(item.Description);
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatMoney(item.LineTotal, invoice.Currency));
                        }

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                        }
                    });

                    // Subtotal
                    column.Item().PaddingTop(16).AlignRight().Column(totalCol =>
                    {
                        totalCol.Item().Text($"Subtotal: {FormatMoney(invoice.SubtotalAmount, invoice.Currency)}").SemiBold();
                    });
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatMoney(decimal amount, string currency)
    {
        return $"{amount:N2} {currency}";
    }
}
