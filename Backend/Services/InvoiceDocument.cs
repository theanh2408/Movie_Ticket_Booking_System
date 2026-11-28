using System.Xml.Linq;
using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using QuestPDF.Fluent;

namespace Backend.Services;

internal static class InvoiceDocument
{
    /// <summary>PDF dùng font Arial (có sẵn trên Windows, hiển thị đúng tiếng Việt).</summary>
    public static byte[] ToPdf(InvoiceDetailDto inv)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var b = inv.Booking;
        DateTime? created = inv.CreateDate.HasValue ? AppClock.ToLocal(inv.CreateDate.Value) : null;   // hiển thị giờ Việt Nam
        var tickets = b?.Tickets ?? new List<TicketDto>();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("HÓA ĐƠN VÉ XEM PHIM").FontSize(20).Bold();
                    col.Item().Text($"Mã hóa đơn: #{inv.InvoiceID}    Trạng thái: {inv.Status}");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Text($"Ngày phát hành: {created:dd/MM/yyyy HH:mm}");
                    if (b != null)
                    {
                        col.Item().Text($"Mã đơn đặt vé: #{b.BookingID}");
                        col.Item().Text($"Khách hàng: {b.CustomerName ?? "Khách vãng lai"}   SĐT: {b.CustomerPhone}");
                        col.Item().Text($"Phim: {b.MovieTitle}");
                        col.Item().Text($"Rạp: {b.CinemaName}   Phòng: {b.RoomName}");
                        col.Item().Text($"Suất chiếu: {b.ShowDate:dd/MM/yyyy} {b.StartTime:HH:mm}");
                    }

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(110);
                            c.ConstantColumn(90);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Text("Ghế").Bold();
                            h.Cell().AlignRight().Text("Giá vé").Bold();
                            h.Cell().Text("Trạng thái").Bold();
                        });
                        foreach (var t in tickets)
                        {
                            table.Cell().Text(t.SeatCode ?? "");
                            table.Cell().AlignRight().Text($"{t.Price:N0}");
                            table.Cell().Text(t.Status ?? "");
                        }
                    });

                    if (b != null)
                    {
                        col.Item().PaddingTop(10).AlignRight().Text($"Tạm tính: {b.SubTotal:N0}");
                        col.Item().AlignRight().Text($"Giảm giá: {b.DiscountAmount:N0}");
                    }
                    col.Item().AlignRight().Text($"TỔNG THANH TOÁN: {inv.TotalPrice:N0}").Bold().FontSize(13);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    public static byte[] ToXml(InvoiceDetailDto inv)
    {
        var b = inv.Booking;
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Invoice",
                new XElement("InvoiceID", inv.InvoiceID),
                new XElement("Status", inv.Status),
                new XElement("CreateDateUtc", inv.CreateDate?.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("TotalPrice", inv.TotalPrice),
                b == null ? null : new XElement("Booking",
                    new XElement("BookingID", b.BookingID),
                    new XElement("CustomerName", b.CustomerName),
                    new XElement("CustomerPhone", b.CustomerPhone),
                    new XElement("MovieTitle", b.MovieTitle),
                    new XElement("CinemaName", b.CinemaName),
                    new XElement("RoomName", b.RoomName),
                    new XElement("ShowDate", b.ShowDate?.ToString("yyyy-MM-dd")),
                    new XElement("StartTime", b.StartTime?.ToString("HH:mm")),
                    new XElement("SubTotal", b.SubTotal),
                    new XElement("DiscountAmount", b.DiscountAmount),
                    new XElement("Tickets", b.Tickets.Select(t => new XElement("Ticket",
                        new XElement("TicketID", t.TicketID),
                        new XElement("SeatCode", t.SeatCode),
                        new XElement("Price", t.Price),
                        new XElement("Status", t.Status)))))));

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }
}
