using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/customers/{customerId:int}/bookings")]
[Authorize]
public class CustomerBillingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerBillingController(ApplicationDbContext context) => _context = context;

    [HttpPost("{bookingId:int}/promotion")]
    public async Task<IActionResult> ApplyPromotion(int customerId, int bookingId, [FromBody] ApplyPromotionRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsCustomerAsync(customerId, cancellationToken))
            return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        var booking = await _context.Bookings.Include(b => b.BookingPromotions)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId, cancellationToken);
        if (booking == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt vé." });
        if (booking.Status != "PENDING") return Conflict(new { success = false, message = "Chỉ được áp mã cho đơn đang ở trạng thái Pending." });
        if (booking.ExpiredAt <= DateTime.UtcNow) return Conflict(new { success = false, message = "Đơn đặt vé đã hết thời gian giữ ghế." });

        var code = request.Code.Trim().ToUpperInvariant();
        var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Code != null && p.Code.ToUpper() == code, cancellationToken);
        if (promotion == null || promotion.Status != "ACTIVE" ||
            (promotion.StartDate.HasValue && promotion.StartDate.Value > DateTime.UtcNow) ||
            (promotion.EndDate.HasValue && promotion.EndDate.Value < DateTime.UtcNow))
            return BadRequest(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });
        if (!promotion.DiscountValue.HasValue || promotion.DiscountValue <= 0)
            return BadRequest(new { success = false, message = "Giá trị mã khuyến mãi không hợp lệ." });

        var subtotal = booking.SubTotal ?? 0;
        if (promotion.MinOrderAmount.HasValue && subtotal < promotion.MinOrderAmount.Value)
            return BadRequest(new { success = false, message = $"Đơn hàng tối thiểu để dùng mã là {promotion.MinOrderAmount.Value:0.##}." });

        var usedCount = await _context.BookingPromotions.CountAsync(bp => bp.PromotionId == promotion.PromotionId && bp.Booking!.Status == "PAID", cancellationToken);
        if (promotion.UsageLimit.HasValue && usedCount >= promotion.UsageLimit.Value)
            return Conflict(new { success = false, message = "Mã khuyến mãi đã hết lượt sử dụng." });

        if (promotion.UsagePerCustomer.HasValue)
        {
            var customerUsedCount = await _context.BookingPromotions.CountAsync(bp => bp.PromotionId == promotion.PromotionId && bp.Booking!.CustomerId == customerId && bp.Booking.Status == "PAID", cancellationToken);
            if (customerUsedCount >= promotion.UsagePerCustomer.Value)
                return Conflict(new { success = false, message = "Bạn đã sử dụng mã khuyến mãi quá số lần cho phép." });
        }

        decimal discount;
        try
        {
            discount = CalculateDiscount(promotion, subtotal);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        _context.BookingPromotions.RemoveRange(booking.BookingPromotions);
        booking.DiscountAmount = discount;
        booking.TotalAmount = Math.Max(0, subtotal - discount);
        _context.BookingPromotions.Add(new BookingPromotion
        {
            BookingId = booking.BookingId,
            PromotionId = promotion.PromotionId,
            DiscountAmount = discount,
            AppliedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                bookingId,
                promotionId = promotion.PromotionId,
                code = promotion.Code,
                subTotal = booking.SubTotal,
                discountAmount = booking.DiscountAmount,
                totalAmount = booking.TotalAmount,
                expiresAt = booking.ExpiredAt
            },
            message = "Áp dụng mã khuyến mãi thành công."
        });
    }

    [HttpPost("{bookingId:int}/invoice")]
    public async Task<IActionResult> CreateInvoice(int customerId, int bookingId, CancellationToken cancellationToken)
    {
        if (!await OwnsCustomerAsync(customerId, cancellationToken))
            return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var booking = await _context.Bookings.Include(b => b.Invoice).Include(b => b.BookingQRs).Include(b => b.Tickets)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId, cancellationToken);
        if (booking == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt vé." });
        var payment = booking.Payments.Where(p => p.PaymentStatus == "SUCCESS").OrderByDescending(p => p.PaymentDate).FirstOrDefault();
        if (payment == null) return Conflict(new { success = false, message = "Chỉ được tạo hóa đơn sau khi thanh toán thành công." });

        foreach (var ticket in booking.Tickets.Where(t => string.IsNullOrWhiteSpace(t.Barcode)))
            ticket.Barcode = $"TKT-{Guid.NewGuid():N}";
        if (!booking.BookingQRs.Any(q => q.Status == "Active"))
            _context.BookingQRs.Add(new BookingQR { BookingId = booking.BookingId, Token = $"BOOKING-{Guid.NewGuid():N}", Status = "Active" });

        var invoice = booking.Invoice;
        if (invoice == null)
        {
            invoice = new Invoice
            {
                BookingId = booking.BookingId,
                CreateDate = payment.PaymentDate ?? DateTime.UtcNow,
                Status = "PAID",
                TotalPrice = booking.TotalAmount
            };
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var existingTicketIds = await _context.InvoiceDetails.Where(d => d.InvoiceId == invoice.InvoiceId).Select(d => d.TicketId).ToListAsync(cancellationToken);
        _context.InvoiceDetails.AddRange(booking.Tickets.Where(t => !existingTicketIds.Contains(t.TicketId)).Select(t => new InvoiceDetail { InvoiceId = invoice.InvoiceId, TicketId = t.TicketId }));
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                invoiceId = invoice.InvoiceId,
                bookingId = booking.BookingId,
                createDate = invoice.CreateDate,
                status = invoice.Status,
                totalPrice = invoice.TotalPrice,
                qrToken = booking.BookingQRs.Where(q => q.Status == "Active").Select(q => q.Token).FirstOrDefault(),
                ticketIds = booking.Tickets.Select(t => t.TicketId).ToList()
            },
            message = "Tạo hóa đơn thành công."
        });
    }

    private async Task<bool> OwnsCustomerAsync(int customerId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return userId.HasValue && await _context.Customers.AnyAsync(c => c.CustomerId == customerId && c.UserId == userId.Value, cancellationToken);
    }

    private int? GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    private static decimal CalculateDiscount(Promotion promotion, decimal subtotal)
    {
        var type = promotion.DiscountType?.Trim().ToUpperInvariant();
        var value = promotion.DiscountValue!.Value;
        decimal discount = type is "PERCENT" or "PERCENTAGE" or "PERCENT_DISCOUNT"
            ? subtotal * value / 100m
            : type is "FIXED" or "AMOUNT" or "FIXED_AMOUNT"
                ? value
                : throw new ArgumentException("Loại khuyến mãi không được hỗ trợ.");
        if (promotion.MaxDiscountAmount.HasValue) discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
        return Math.Min(Math.Max(0, discount), subtotal);
    }
}
