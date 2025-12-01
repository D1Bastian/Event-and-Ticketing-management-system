using EventApp.Data;
using EventApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EventApp.Controllers;

public class PurchaseController : Controller
{
    private readonly ApplicationDbContext _context;

    public PurchaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    // List all purchases (Guest users)
    public IActionResult Index()
    {
        // Use Include to bring in the related Event data
        var purchases = _context.Purchases
            .Include(p => p.Event)
            .OrderByDescending(p => p.PurchaseDate)
            .ToList();

        return View(purchases);
    }

    // Confirmation page for a single purchase
    public IActionResult Confirmation(int id)
    {
        var purchase = _context.Purchases
            .Include(p => p.Event)
            .FirstOrDefault(p => p.PurchaseId == id);

        if (purchase == null)
            return NotFound();

        return View(purchase);
    }

    // GET: Purchase/Create/5 (Display purchase form for a specific event)
    public IActionResult Create(int eventId)
    {
        // FIX: Changed e.EventId to e.Id to match the Event model's primary key
        var eventItem = _context.Events.FirstOrDefault(e => e.Id == eventId && e.AvailableTickets > 0);
        
        if (eventItem == null)
        {
            TempData["Error"] = "Event not found or sold out.";
            return RedirectToAction("Index", "Events");
        }

        ViewBag.Event = eventItem;
        return View();
    }

    // POST: Purchase/Create (Handle ticket purchase submission)
    [HttpPost]
    [ValidateAntiForgeryToken]
    // Note: It's better practice to bind to a ViewModel instead of individual parameters, 
    // but we'll stick to the current signature for minimal changes.
    public IActionResult Create(int eventId, string guestName, string guestEmail, int quantity)
    {
        // FIX: Changed e.EventId to e.Id to match the Event model's primary key
        var eventItem = _context.Events.FirstOrDefault(e => e.Id == eventId);
        
        // Basic server-side validation
        if (eventItem == null)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Index", "Events");
        }

        if (quantity <= 0 || quantity > eventItem.AvailableTickets)
        {
            TempData["Error"] = $"Invalid ticket quantity. Max available: {eventItem.AvailableTickets}";
            return RedirectToAction(nameof(Create), new { eventId });
        }

        // Save purchase
        var purchase = new Purchase
        {
            EventId = eventId,
            GuestName = guestName,
            GuestEmail = guestEmail,
            Quantity = quantity,
            TotalCost = quantity * eventItem.TicketPrice,
            PurchaseDate = DateTime.UtcNow
        };

        // Update ticket count and save changes in a single transaction
        eventItem.AvailableTickets -= quantity; 
        
        _context.Purchases.Add(purchase);
        _context.SaveChanges();

        return RedirectToAction("Confirmation", new { id = purchase.PurchaseId });
    }
}