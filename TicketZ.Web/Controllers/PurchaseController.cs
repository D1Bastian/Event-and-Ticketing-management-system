using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TicketZ.Core.Models;
using TicketZ.Infrastructure.Data;

namespace TicketZ.Web.Controllers;

public class PurchaseController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _receiptProtector;

    public PurchaseController(ApplicationDbContext context, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _receiptProtector = dataProtectionProvider.CreateProtector("PurchaseReceiptAccess");
    }

    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Index()
    {
        var purchases = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Event)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();

        return View(purchases);
    }

    public async Task<IActionResult> Confirmation(int id, string? token)
    {
        var purchase = await _context.Purchases
            .AsNoTracking()
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.PurchaseId == id);

        if (purchase == null)
        {
            return NotFound();
        }

        if (!HasReceiptAccess(purchase, token))
        {
            return Forbid();
        }

        ViewBag.CanViewPurchaseSummary = User.IsInRole("Admin") || User.IsInRole("Organizer");
        ViewBag.ShowConfetti = !string.IsNullOrWhiteSpace(token);

        return View(purchase);
    }

    public async Task<IActionResult> Create(int eventId)
    {
        var model = await BuildPurchaseViewModelAsync(eventId);
        if (model == null)
        {
            TempData["Error"] = "Event not found or sold out.";
            return RedirectToAction("Index", "Event");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PurchaseRequestViewModel model)
    {
        await PopulatePurchaseViewModelAsync(model);

        if (model.AvailableTickets <= 0)
        {
            ModelState.AddModelError(string.Empty, "This event is sold out.");
        }
        else if (model.Quantity > model.AvailableTickets)
        {
            ModelState.AddModelError(nameof(model.Quantity), $"Only {model.AvailableTickets} ticket(s) remain.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var updatedRows = await _context.Events
            .Where(e => e.Id == model.EventId && e.AvailableTickets >= model.Quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.AvailableTickets, e => e.AvailableTickets - model.Quantity));

        if (updatedRows == 0)
        {
            await transaction.RollbackAsync();
            await PopulatePurchaseViewModelAsync(model);
            ModelState.AddModelError(nameof(model.Quantity), "Those tickets were just claimed. Try a smaller quantity.");
            return View(model);
        }

        var purchase = new Purchase
        {
            EventId = model.EventId,
            // Guest checkout is intentionally email-only. Accounts remain
            // optional and are reserved for organizers and administrators.
            GuestName = string.Empty,
            GuestEmail = model.GuestEmail.Trim(),
            Quantity = model.Quantity,
            TotalCost = model.Quantity * model.TicketPrice,
            PurchaseDate = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(Confirmation), new
        {
            id = purchase.PurchaseId,
            token = CreateReceiptToken(purchase)
        });
    }

    private async Task<PurchaseRequestViewModel?> BuildPurchaseViewModelAsync(int eventId)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventItem == null)
        {
            return null;
        }

        return new PurchaseRequestViewModel
        {
            EventId = eventItem.Id,
            EventTitle = eventItem.Title,
            EventDate = eventItem.EventDate,
            Location = eventItem.Location,
            TicketPrice = eventItem.TicketPrice,
            AvailableTickets = eventItem.AvailableTickets
        };
    }

    private async Task PopulatePurchaseViewModelAsync(PurchaseRequestViewModel model)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == model.EventId);

        if (eventItem == null)
        {
            ModelState.AddModelError(string.Empty, "The selected event could not be found.");
            model.EventTitle = "Unavailable event";
            model.Location = string.Empty;
            model.TicketPrice = 0;
            model.AvailableTickets = 0;
            return;
        }

        model.EventTitle = eventItem.Title;
        model.EventDate = eventItem.EventDate;
        model.Location = eventItem.Location;
        model.TicketPrice = eventItem.TicketPrice;
        model.AvailableTickets = eventItem.AvailableTickets;
    }

    private string CreateReceiptToken(Purchase purchase)
    {
        var payload = $"{purchase.PurchaseId}|{purchase.GuestEmail.Trim().ToLowerInvariant()}";
        var protectedPayload = _receiptProtector.Protect(payload);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(protectedPayload));
    }

    private bool HasReceiptAccess(Purchase purchase, string? token)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Organizer"))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedPayload = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var payload = _receiptProtector.Unprotect(protectedPayload);
            var parts = payload.Split('|', 2);

            return parts.Length == 2
                   && int.TryParse(parts[0], out var purchaseId)
                   && purchaseId == purchase.PurchaseId
                   && string.Equals(parts[1], purchase.GuestEmail.Trim().ToLowerInvariant(), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
