using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VenuePlatform.BLL.Domain.Bookings;
using VenuePlatform.BLL.Domain.Billing;
using VenuePlatform.BLL.Domain.Clients;
using VenuePlatform.BLL.Limits;
using VenuePlatform.BLL.Tenancy;
using VenuePlatform.Contracts.Auth;
using VenuePlatform.Contracts.Billing;
using VenuePlatform.Contracts.Bookings;
using VenuePlatform.DAL.Persistence;
using VenuePlatform.Web.Api;
using VenuePlatform.Web.Pdf;
using VenuePlatform.Web.Security;
using VenuePlatform.Web.Services;

namespace VenuePlatform.Web.Endpoints;

/// <summary>
/// Invoice management endpoints (tenant-scoped).
/// </summary>
public static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(this RouteGroupBuilder group, IConfiguration configuration)
    {
        // Initialize token service with JWT secret
        var jwtKey = configuration.GetSection("Jwt")["Key"]!;
        var tokenService = new InvoiceDownloadToken(jwtKey);
        // POST /{companySlug}/bookings/{id:guid}/invoice - Create invoice from booking (requires Manager/Admin/Owner)
        group.MapPost("/bookings/{id:guid}/invoice", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // 1) Load booking (tenant-filtered)
            var booking = db.Bookings
                .AsNoTracking()
                .FirstOrDefault(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // 2) Validate booking is not cancelled
            if (booking.IsCancelled)
            {
                return Results.BadRequest(new { error = "Invoice cannot be created because the booking is cancelled." });
            }

            // 3) Validate booking is Confirmed
            if (booking.Status != BookingStatus.Confirmed)
            {
                return Results.BadRequest(new { error = "Can only create invoice for confirmed bookings." });
            }

            // 4) Ensure booking has spaces
            var bookingSpaceIds = db.BookingSpaces
                .AsNoTracking()
                .Where(bs => bs.BookingId == id)
                .Select(bs => bs.SpaceId)
                .ToList();

            if (bookingSpaceIds.Count == 0)
            {
                return Results.BadRequest(new { error = "Booking must have at least one space attached." });
            }

            // 5) Check if invoice already exists for this booking
            var existingInvoice = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.CompanyId == tenant.CompanyId && i.BookingId == id);

            if (existingInvoice != null)
            {
                return Results.Conflict(new { error = "Invoice already exists for this booking." });
            }

            // 6) Integrity check: recompute booking total and compare against stored TotalAmount
            // Load spaces with hourly rates
            var spacesForIntegrityCheck = await db.Spaces
                .AsNoTracking()
                .Where(s => bookingSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
                .Select(s => new { s.HourlyRate })
                .ToListAsync();

            if (spacesForIntegrityCheck.Count != bookingSpaceIds.Count)
            {
                return Results.Problem("Some spaces attached to booking were not found.", statusCode: 500);
            }

            var spaceHourlyRates = spacesForIntegrityCheck.Select(s => s.HourlyRate).ToList();

            // Check for SpaceConfiguration override rate
            decimal? overrideRateForIntegrityCheck = null;
            if (booking.SpaceConfigurationId.HasValue)
            {
                overrideRateForIntegrityCheck = await db.SpaceConfigurations
                    .AsNoTracking()
                    .Where(sc => sc.Id == booking.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId)
                    .Select(sc => sc.HourlyRateOverride)
                    .FirstOrDefaultAsync();
            }

            // Recompute expected total using same rules as booking creation
            var expectedTotal = EndpointHelpers.CalculateBookingTotal(
                booking.StartUtc,
                booking.EndUtc,
                spaceHourlyRates,
                overrideRateForIntegrityCheck);

            // Compare stored vs recomputed using 2-decimal rounding
            var storedTotalRounded = Math.Round(booking.TotalAmount, 2);
            var expectedTotalRounded = Math.Round(expectedTotal, 2);

            if (Math.Abs(storedTotalRounded - expectedTotalRounded) >= 0.01m)
            {
                return Results.Conflict(new
                {
                    error = "Booking total does not match recomputed total. Refuse to create invoice.",
                    storedTotalAmount = storedTotalRounded,
                    recomputedTotalAmount = expectedTotalRounded
                });
            }

            // 7) Calculate billed duration (15-min ceiling, same as booking calculation)
            var durationMinutes = (booking.EndUtc - booking.StartUtc).TotalMinutes;
            var billingUnits = (int)Math.Ceiling(durationMinutes / 15.0);
            var billedMinutes = billingUnits * 15;
            var billedHours = billedMinutes / 60m;

            // 8) Load spaces with hourly rates
            var spaces = await db.Spaces
                .AsNoTracking()
                .Where(s => bookingSpaceIds.Contains(s.Id) && s.CompanyId == tenant.CompanyId)
                .Select(s => new { s.Id, s.Name, s.HourlyRate })
                .ToListAsync();

            if (spaces.Count != bookingSpaceIds.Count)
            {
                return Results.Problem("Some spaces attached to booking were not found.", statusCode: 500);
            }

            // 9) Check for SpaceConfiguration override rate
            decimal? overrideRate = null;
            if (booking.SpaceConfigurationId.HasValue)
            {
                overrideRate = await db.SpaceConfigurations
                    .AsNoTracking()
                    .Where(sc => sc.Id == booking.SpaceConfigurationId.Value && sc.CompanyId == tenant.CompanyId)
                    .Select(sc => sc.HourlyRateOverride)
                    .FirstOrDefaultAsync();
            }

            // 10) Assign invoice number (transactional)
            await using var tx = await db.Database.BeginTransactionAsync();

            var counter = await db.InvoiceCounters
                .FirstOrDefaultAsync(c => c.CompanyId == tenant.CompanyId);

            if (counter is null)
            {
                counter = new InvoiceCounter(tenant.CompanyId, 1);
                db.InvoiceCounters.Add(counter);
            }

            var currentNumber = counter.NextInvoiceNumber;
            counter.NextInvoiceNumber += 1;

            // 11) Create invoice
            var invoice = new Invoice(
                id: Guid.NewGuid(),
                companyId: tenant.CompanyId,
                bookingId: booking.Id,
                createdUtc: DateTime.UtcNow,
                createdByUserId: userId,
                subtotalAmount: 0m, // Will be calculated after items are created
                currency: "EUR"
            );

            invoice.SetNumber(currentNumber);
            db.Invoices.Add(invoice);

            // 12) Create invoice items - one per space
            var invoiceItems = new List<InvoiceItem>();
            decimal subtotal = 0m;

            foreach (var space in spaces)
            {
                // Determine rate: use override if set, otherwise space's hourly rate
                var rate = overrideRate ?? space.HourlyRate;
                var lineTotal = Math.Round(rate * billedHours, 2, MidpointRounding.AwayFromZero);

                var item = InvoiceItem.Create(
                    id: Guid.NewGuid(),
                    companyId: tenant.CompanyId,
                    invoiceId: invoice.Id,
                    description: $"Space: {space.Name}",
                    quantity: 1,
                    unitPrice: lineTotal
                );

                invoiceItems.Add(item);
                subtotal += lineTotal;
            }

            foreach (var item in invoiceItems)
            {
                db.InvoiceItems.Add(item);
            }

            // 13) Update invoice subtotal
            invoice.SetSubtotalAmount(subtotal);

            // 14) Save changes and commit transaction
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // 15) Return 201 Created with response
            var itemResponses = invoiceItems.Select(ii => new InvoiceItemResponse(
                ii.Id,
                ii.Description,
                ii.Quantity,
                ii.UnitPrice,
                ii.LineTotal
            )).ToList();

            var response = new InvoiceResponse(
                invoice.Id,
                invoice.BookingId,
                invoice.CreatedUtc,
                invoice.CreatedByUserId,
                invoice.Currency,
                invoice.Status,
                invoice.SubtotalAmount,
                invoice.InvoiceNumber,
                invoice.InvoiceNumberText,
                itemResponses,
                invoice.IssuedUtc,
                invoice.IssuedByUserId,
                invoice.VoidedUtc,
                invoice.VoidedByUserId,
                invoice.SentUtc,
                invoice.SentByUserId,
                // Phase 8.1: Payment totals (new invoice has no payments)
                AmountPaid: 0m,
                AmountDue: invoice.SubtotalAmount,
                IsPaid: false,
                // Phase 8.3: Paid metadata (new invoice is not paid)
                PaidUtc: null,
                PaidByUserId: null
            );

            return Results.Created($"/{tenant.CompanySlug}/invoices/{invoice.Id}", response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/invoices - List invoices for tenant (requires auth + any membership)
        group.MapGet("/invoices", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Check membership
            var isMember = db.UserCompanyMemberships
                .AsNoTracking()
                .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            // Load invoices (tenant-filtered), ordered by CreatedUtc desc
            var invoices = await db.Invoices
                .AsNoTracking()
                .Where(i => i.CompanyId == tenant.CompanyId)
                .OrderByDescending(i => i.CreatedUtc)
                .Select(i => new
                {
                    i.Id,
                    i.BookingId,
                    i.CreatedUtc,
                    i.Status,
                    i.Currency,
                    i.SubtotalAmount,
                    i.InvoiceNumber,
                    i.InvoiceNumberText,
                    i.IssuedUtc,
                    i.VoidedUtc,
                    i.SentUtc,
                    i.PaidUtc
                })
                .ToListAsync();

            // Phase 8.1: Query payments grouped by invoice
            var invoiceIds = invoices.Select(i => i.Id).ToList();
            var paidByInvoice = await db.Payments
                .AsNoTracking()
                .Where(p => invoiceIds.Contains(p.InvoiceId))
                .GroupBy(p => p.InvoiceId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    AmountPaid = g.Sum(x => x.Amount)
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.AmountPaid);

            // Build responses with payment totals
            var responses = invoices.Select(i =>
            {
                var amountPaid = Math.Round(paidByInvoice.GetValueOrDefault(i.Id, 0m), 2, MidpointRounding.AwayFromZero);
                var amountDue = Math.Round(Math.Max(0m, i.SubtotalAmount - amountPaid), 2, MidpointRounding.AwayFromZero);
                var isPaid = amountDue == 0;

                return new InvoiceListItemResponse(
                    i.Id,
                    i.BookingId,
                    i.CreatedUtc,
                    i.Status,
                    i.Currency,
                    i.SubtotalAmount,
                    i.InvoiceNumber,
                    i.InvoiceNumberText,
                    i.IssuedUtc,
                    i.VoidedUtc,
                    i.SentUtc,
                    AmountPaid: amountPaid,
                    AmountDue: amountDue,
                    IsPaid: isPaid,
                    PaidUtc: i.PaidUtc
                );
            }).ToList();

            return Results.Ok(responses);
        })
        .RequireAuthorization();

        // GET /{companySlug}/invoices/{id:guid} - Get invoice with items (requires auth + any membership)
        group.MapGet("/invoices/{id:guid}", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Check membership
            var isMember = db.UserCompanyMemberships
                .AsNoTracking()
                .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            // Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // Load invoice items (tenant-filtered)
            var items = await db.InvoiceItems
                .AsNoTracking()
                .Where(ii => ii.InvoiceId == id && ii.CompanyId == tenant.CompanyId)
                .Select(ii => new InvoiceItemResponse(
                    ii.Id,
                    ii.Description,
                    ii.Quantity,
                    ii.UnitPrice,
                    ii.LineTotal
                ))
                .ToListAsync();

            // Phase 8.1: Query payments for this invoice and compute totals
            var amountPaidRaw = await db.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == id && p.CompanyId == tenant.CompanyId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var amountPaid = Math.Round(amountPaidRaw, 2, MidpointRounding.AwayFromZero);
            var amountDue = Math.Round(Math.Max(0m, invoice.SubtotalAmount - amountPaid), 2, MidpointRounding.AwayFromZero);
            var isPaid = amountDue == 0;

            var response = new InvoiceResponse(
                invoice.Id,
                invoice.BookingId,
                invoice.CreatedUtc,
                invoice.CreatedByUserId,
                invoice.Currency,
                invoice.Status,
                invoice.SubtotalAmount,
                invoice.InvoiceNumber,
                invoice.InvoiceNumberText,
                items,
                invoice.IssuedUtc,
                invoice.IssuedByUserId,
                invoice.VoidedUtc,
                invoice.VoidedByUserId,
                invoice.SentUtc,
                invoice.SentByUserId,
                AmountPaid: amountPaid,
                AmountDue: amountDue,
                IsPaid: isPaid,
                PaidUtc: invoice.PaidUtc,
                PaidByUserId: invoice.PaidByUserId
            );

            return Results.Ok(response);
        })
        .RequireAuthorization();

        // POST /{companySlug}/invoices/{id:guid}/issue - Issue invoice (Draft -> Issued)
        group.MapPost("/invoices/{id:guid}/issue", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // Issue the invoice
            try
            {
                invoice.Issue(userId, DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // POST /{companySlug}/invoices/{id:guid}/void - Void invoice (Draft/Issued -> Void)
        group.MapPost("/invoices/{id:guid}/void", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // Void the invoice (idempotent)
            invoice.Void(userId, DateTime.UtcNow);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // POST /{companySlug}/invoices/{id:guid}/mark-sent - Mark invoice as sent (metadata only)
        group.MapPost("/invoices/{id:guid}/mark-sent", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // Mark as sent (idempotent)
            try
            {
                invoice.MarkSent(userId, DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // POST /{companySlug}/invoices/{id:guid}/mark-paid - Mark invoice as paid (validates payment totals)
        group.MapPost("/invoices/{id:guid}/mark-paid", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // 1) Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // 2) If invoice is Void -> 400
            if (invoice.Status == InvoiceStatus.Void)
            {
                return Results.BadRequest(new { error = "Void invoice cannot be marked as paid." });
            }

            // 3) Compute AmountPaid (sum of payments)
            var amountPaid = await db.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.CompanyId == tenant.CompanyId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // 4) Compute AmountDue
            var amountPaidRounded = Math.Round(amountPaid, 2, MidpointRounding.AwayFromZero);
            var subtotalRounded = Math.Round(invoice.SubtotalAmount, 2, MidpointRounding.AwayFromZero);
            var amountDue = Math.Max(0m, subtotalRounded - amountPaidRounded);
            var amountDueRounded = Math.Round(amountDue, 2, MidpointRounding.AwayFromZero);

            // 5) If AmountDue != 0 -> return 409 Conflict
            if (amountDueRounded != 0m)
            {
                return Results.Conflict(new
                {
                    error = "Invoice cannot be marked as paid because amount is still due.",
                    subtotalAmount = subtotalRounded,
                    amountPaid = amountPaidRounded,
                    amountDue = amountDueRounded
                });
            }

            // 6) If already paid (PaidUtc not null) -> return 204 (idempotent)
            if (invoice.PaidUtc != null)
            {
                return Results.NoContent();
            }

            // 7) Mark as paid
            try
            {
                invoice.MarkPaid(userId, DateTime.UtcNow);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // POST /{companySlug}/bookings/{id:guid}/invoice/void - Void invoice for a booking (explicit workflow)
        group.MapPost("/bookings/{id:guid}/invoice/void", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // 1) Load booking (tenant-filtered)
            var booking = await db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.NotFound();
            }

            // 2) Find invoice for booking
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.CompanyId == tenant.CompanyId && i.BookingId == booking.Id);

            if (invoice is null)
            {
                return Results.NotFound(new { error = "Invoice not found for booking." });
            }

            // 3) If invoice already Void -> return 204 (idempotent)
            if (invoice.Status == InvoiceStatus.Void)
            {
                return Results.NoContent();
            }

            // 4) Void the invoice
            invoice.Void(userId, DateTime.UtcNow);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();

        // POST /{companySlug}/invoices/{id:guid}/payments - Record a payment for an invoice (Manager/Admin/Owner)
        group.MapPost("/invoices/{id:guid}/payments", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, CreatePaymentRequest request, bool? allowOverpay) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // 1) Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // 2) Validate invoice status - cannot add payment to void invoice
            if (invoice.Status == InvoiceStatus.Void)
            {
                return ApiErrors.BadRequest("invoice_void", "Cannot add payment to void invoice.");
            }

            // Phase 8.4: Block payment if invoice already marked as paid
            if (invoice.PaidUtc != null)
            {
                return ApiErrors.BadRequest("invoice_paid_locked", "Cannot add payment to invoice that is already marked as paid.");
            }

            // 3) Validate request
            if (request.Amount <= 0)
            {
                return Results.BadRequest(new { error = "Amount must be greater than 0." });
            }

            if (string.IsNullOrWhiteSpace(request.Method))
            {
                return Results.BadRequest(new { error = "Method is required." });
            }

            var method = request.Method.Trim();
            if (method.Length > 30)
            {
                return Results.BadRequest(new { error = "Method must be at most 30 characters." });
            }

            var reference = request.Reference?.Trim();
            if (!string.IsNullOrEmpty(reference) && reference.Length > 100)
            {
                return Results.BadRequest(new { error = "Reference must be at most 100 characters." });
            }

            // 4) Phase 8.2: Overpayment guardrail
            var currentPaid = await db.Payments
                .Where(p => p.InvoiceId == invoice.Id && p.CompanyId == tenant.CompanyId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var prospectivePaid = currentPaid + request.Amount;

            // Round to 2 decimals AwayFromZero for comparison
            var currentPaidRounded = Math.Round(currentPaid, 2, MidpointRounding.AwayFromZero);
            var subtotalRounded = Math.Round(invoice.SubtotalAmount, 2, MidpointRounding.AwayFromZero);
            var prospectivePaidRounded = Math.Round(prospectivePaid, 2, MidpointRounding.AwayFromZero);

            if (allowOverpay != true && prospectivePaidRounded > subtotalRounded)
            {
                return ApiErrors.Conflict("payment_overpay", "Payment would exceed invoice subtotal.", new
                {
                    subtotalAmount = subtotalRounded,
                    currentAmountPaid = currentPaidRounded,
                    attemptedPaymentAmount = request.Amount,
                    prospectiveAmountPaid = prospectivePaidRounded
                });
            }

            // 5) Create Payment
            var payment = new Payment(
                id: Guid.NewGuid(),
                companyId: tenant.CompanyId,
                invoiceId: invoice.Id,
                amount: request.Amount,
                paidUtc: request.PaidUtc,
                method: method,
                reference: reference,
                createdByUserId: userId,
                createdUtc: DateTime.UtcNow
            );

            db.Payments.Add(payment);
            await db.SaveChangesAsync();

            // 6) Return 201 Created with PaymentResponse
            var response = new PaymentResponse(
                payment.Id,
                payment.InvoiceId,
                payment.Amount,
                payment.PaidUtc,
                payment.Method,
                payment.Reference,
                payment.CreatedByUserId,
                payment.CreatedUtc
            );

            return Results.Created($"/{tenant.CompanySlug}/invoices/{invoice.Id}/payments/{payment.Id}", response);
        })
        .RequireAuthorization();

        // GET /{companySlug}/invoices/{id:guid}/payments - List payments for an invoice (any membership)
        group.MapGet("/invoices/{id:guid}/payments", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Check membership
            var isMember = db.UserCompanyMemberships
                .AsNoTracking()
                .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (!isMember)
            {
                return Results.Forbid();
            }

            // 1) Ensure invoice exists (tenant-filtered)
            var invoiceExists = await db.Invoices
                .AsNoTracking()
                .AnyAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (!invoiceExists)
            {
                return Results.NotFound();
            }

            // 2) Return payments for invoice ordered by PaidUtc asc, then CreatedUtc asc
            var payments = await db.Payments
                .AsNoTracking()
                .Where(p => p.InvoiceId == id && p.CompanyId == tenant.CompanyId)
                .OrderBy(p => p.PaidUtc)
                .ThenBy(p => p.CreatedUtc)
                .Select(p => new PaymentResponse(
                    p.Id,
                    p.InvoiceId,
                    p.Amount,
                    p.PaidUtc,
                    p.Method,
                    p.Reference,
                    p.CreatedByUserId,
                    p.CreatedUtc
                ))
                .ToListAsync();

            return Results.Ok(payments);
        })
        .RequireAuthorization();

        // GET /{companySlug}/invoices/{id:guid}/pdf - Download invoice as PDF (token-based or authenticated)
        group.MapGet("/invoices/{id:guid}/pdf", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id, string? token) =>
        {
            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Determine if access is via valid download token or authenticated membership
            bool hasValidAccess = false;

            if (!string.IsNullOrEmpty(token))
            {
                // Validate download token
                hasValidAccess = tokenService.TryValidateToken(token, id, out _);
            }
            else
            {
                // Fall back to authenticated membership check
                var userId = EndpointHelpers.GetUserIdFromClaims(user);
                if (userId != Guid.Empty)
                {
                    hasValidAccess = db.UserCompanyMemberships
                        .AsNoTracking()
                        .Any(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);
                }
            }

            if (!hasValidAccess)
            {
                return Results.Forbid();
            }

            // 1) Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // 2) Load invoice items (tenant-filtered)
            var items = await db.InvoiceItems
                .AsNoTracking()
                .Where(ii => ii.InvoiceId == id && ii.CompanyId == tenant.CompanyId)
                .ToListAsync();

            // 3) Load booking for invoice.BookingId (tenant-filtered)
            var booking = await db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == invoice.BookingId && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.Problem("Invoice booking not found.", statusCode: 500);
            }

            // 4) Load client name for booking.ClientId (tenant-filtered)
            var client = await db.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == booking.ClientId && c.CompanyId == tenant.CompanyId);

            if (client is null)
            {
                return Results.Problem("Booking client not found.", statusCode: 500);
            }

            // 5) Load company name (from Companies table by tenant company id; NOT by slug)
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

            var companyName = company?.Name ?? "Unknown Company";

            // 6) Call InvoicePdfRenderer.RenderInvoicePdf(...)
            var pdfBytes = InvoicePdfRenderer.RenderInvoicePdf(
                companyName,
                tenant.CompanySlug,
                invoice,
                client.Name,
                booking,
                items);

            // 7) Return file
            var fileName = $"invoice-{invoice.InvoiceNumberText}.pdf";
            return Results.File(pdfBytes, "application/pdf", fileName);
        });

        // POST /{companySlug}/invoices/{id:guid}/generate-download-link - Generate secure temporary download link (Manager/Admin/Owner)
        group.MapPost("/invoices/{id:guid}/generate-download-link", async (ApplicationDbContext db, ITenantContext tenantContext, ClaimsPrincipal user, Guid id) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // 1) Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // 2) Generate token with 24-hour expiration
            var expiresUtc = DateTime.UtcNow.AddHours(24);
            var token = tokenService.GenerateToken(id, expiresUtc);

            // 3) Build download URL
            var downloadUrl = $"/{tenant.CompanySlug}/invoices/{id}/pdf?token={Uri.EscapeDataString(token)}";

            // 4) Return response
            var response = new InvoiceDownloadLinkResponse(downloadUrl, expiresUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .RequireRateLimiting("invoice-link");

        // POST /{companySlug}/invoices/{id:guid}/send-email - Send invoice PDF download link to client via email
        group.MapPost("/invoices/{id:guid}/send-email", async (
            ApplicationDbContext db,
            ITenantContext tenantContext,
            ClaimsPrincipal user,
            Guid id,
            EmailSender emailSender,
            IConfiguration configuration,
            bool? attachPdf = true,
            bool? force = false) =>
        {
            // Extract userId from "sub" claim
            var userId = EndpointHelpers.GetUserIdFromClaims(user);
            if (userId == Guid.Empty)
            {
                return Results.Unauthorized();
            }

            var tenant = tenantContext.Current;
            if (tenant is null)
            {
                return Results.NotFound();
            }

            // Query membership with role
            var membership = db.UserCompanyMemberships
                .AsNoTracking()
                .FirstOrDefault(m => m.UserId == userId && m.CompanyId == tenant.CompanyId);

            if (membership is null)
            {
                return Results.Forbid();
            }

            // Allow only: CompanyOwner, CompanyAdmin, CompanyManager
            var allowedRoles = new[] { TenantRoles.CompanyOwner, TenantRoles.CompanyAdmin, TenantRoles.CompanyManager };
            if (!allowedRoles.Contains(membership.Role))
            {
                return Results.Forbid();
            }

            // Pro-only gate for PDF attachment (attachPdf defaults to true)
            if (attachPdf != false)
            {
                var companyForPlanCheck = await db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

                if (companyForPlanCheck is null || !PlanFeatures.AllowsInvoicePdfAttachment(companyForPlanCheck.Plan))
                {
                    return ApiErrors.Forbidden("plan_required", "This feature requires Pro plan.", new { feature = "InvoicePdfAttachment" });
                }
            }

            // 1) Load invoice (tenant-filtered)
            var invoice = await db.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == tenant.CompanyId);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            // Cannot send void invoice
            if (invoice.Status == InvoiceStatus.Void)
            {
                return Results.BadRequest(new { error = "Cannot send void invoice." });
            }

            // Phase 9.4: Prevent rapid re-sends (10-minute cooldown unless force=true)
            if (invoice.SentUtc is not null && force != true)
            {
                var minutesSinceSent = (DateTime.UtcNow - invoice.SentUtc.Value).TotalMinutes;
                if (minutesSinceSent < 10)
                {
                    return ApiErrors.Conflict("invoice_email_cooldown", "Invoice email was sent recently. Use force=true to resend.", new
                    {
                        sentUtc = invoice.SentUtc.Value,
                        cooldownMinutes = 10
                    });
                }
            }

            // 2) Load booking for invoice.BookingId
            var booking = await db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == invoice.BookingId && b.CompanyId == tenant.CompanyId);

            if (booking is null)
            {
                return Results.Problem("Invoice booking not found.", statusCode: 500);
            }

            // 3) Load client for booking.ClientId
            var client = await db.Clients
                .FirstOrDefaultAsync(c => c.Id == booking.ClientId && c.CompanyId == tenant.CompanyId);

            if (client is null)
            {
                return Results.Problem("Booking client not found.", statusCode: 500);
            }

            // 4) Check if client has email
            if (string.IsNullOrWhiteSpace(client.Email))
            {
                return ApiErrors.BadRequest("client_email_missing", "Client does not have an email address.");
            }

            // 5) Generate download link using existing token logic
            var expiresUtc = DateTime.UtcNow.AddHours(24);
            var token = tokenService.GenerateToken(invoice.Id, expiresUtc);
            var relativeUrl = $"/{tenant.CompanySlug}/invoices/{invoice.Id}/pdf?token={Uri.EscapeDataString(token)}";

            // Prepend public base URL if configured
            var publicBaseUrl = configuration.GetSection("Email")["PublicBaseUrl"];
            var downloadUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
                ? relativeUrl
                : publicBaseUrl.TrimEnd('/') + relativeUrl;

            // 6) Load company name for email signature
            var company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == tenant.CompanyId);

            var companyName = company?.Name ?? "Your Venue";

            // 7) Compose email
            var subject = $"Invoice {invoice.InvoiceNumberText}";
            var body = $"Hello,\n\nYour invoice is available at the link below:\n\n{downloadUrl}\n\nThis link expires in 24 hours.\n\nRegards,\n{companyName}";

            // 8) Generate PDF attachment if requested
            List<(string FileName, byte[] Content, string ContentType)>? attachments = null;
            if (attachPdf != false)
            {
                // Load invoice items (tenant-filtered)
                var items = await db.InvoiceItems
                    .AsNoTracking()
                    .Where(ii => ii.InvoiceId == id && ii.CompanyId == tenant.CompanyId)
                    .ToListAsync();

                try
                {
                    var pdfBytes = InvoicePdfRenderer.RenderInvoicePdf(
                        companyName,
                        tenant.CompanySlug,
                        invoice,
                        client.Name,
                        booking,
                        items);

                    var fileName = $"invoice-{invoice.InvoiceNumberText}.pdf";
                    attachments = new List<(string, byte[], string)>
                    {
                        (fileName, pdfBytes, "application/pdf")
                    };
                }
                catch
                {
                    return Results.Json(new { error = "Failed to generate invoice PDF." }, statusCode: 500);
                }
            }

            // 9) Send email
            try
            {
                if (attachments is not null)
                {
                    await emailSender.SendAsync(client.Email, subject, body, attachments);
                }
                else
                {
                    await emailSender.SendAsync(client.Email, subject, body);
                }
            }
            catch (InvalidOperationException)
            {
                return Results.Problem("Failed to send email: Email configuration is incomplete.", statusCode: 500);
            }
            catch (Exception)
            {
                return Results.Json(new { error = "Failed to send email." }, statusCode: 500);
            }

            // 10) Mark invoice as sent (idempotent)
            try
            {
                invoice.MarkSent(userId, DateTime.UtcNow);
                await db.SaveChangesAsync();
            }
            catch (InvalidOperationException)
            {
                // Already sent - that's fine, still return success
            }

            return Results.NoContent();
        })
        .RequireAuthorization()
        .RequireRateLimiting("invoice-email");

        return group;
    }
}
