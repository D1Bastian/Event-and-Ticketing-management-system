using EventApp.Data;
using EventApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventApp.Controllers;

public class EventController : Controller
{
    private readonly ApplicationDbContext _context;

    public EventController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return View(eventItem);
    }

    [Authorize(Roles = "Admin,Organizer")]
    public IActionResult Create()
    {
        return View(new Event
        {
            EventDate = DateTime.Now.AddDays(7),
            Capacity = 50,
            AvailableTickets = 50
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Create([Bind("Title,Description,EventDate,Location,Capacity,TicketPrice,Category,AvailableTickets")] Event eventItem)
    {
        ValidateEvent(eventItem);

        if (!ModelState.IsValid)
        {
            return View(eventItem);
        }

        _context.Add(eventItem);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem == null)
        {
            return NotFound();
        }

        return View(eventItem);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,EventDate,Location,Capacity,TicketPrice,Category,AvailableTickets")] Event eventItem)
    {
        if (id != eventItem.Id)
        {
            return NotFound();
        }

        ValidateEvent(eventItem);

        if (!ModelState.IsValid)
        {
            return View(eventItem);
        }

        try
        {
            _context.Update(eventItem);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Events.AnyAsync(e => e.Id == id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (eventItem == null)
        {
            return NotFound();
        }

        return View(eventItem);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,Organizer")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var eventItem = await _context.Events.FindAsync(id);
        if (eventItem != null)
        {
            _context.Events.Remove(eventItem);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Overview()
    {
        var totalEvents = await _context.Events.CountAsync();
        var totalCategories = await _context.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Category))
            .Select(e => e.Category)
            .Distinct()
            .CountAsync();
        var totalAvailableTickets = await _context.Events.SumAsync(e => (int?)e.AvailableTickets) ?? 0;
        var lowPriceEvents = await _context.Events
            .AsNoTracking()
            .Where(e => e.TicketPrice < 5)
            .OrderBy(e => e.TicketPrice)
            .ThenBy(e => e.EventDate)
            .ToListAsync();

        ViewBag.TotalEvents = totalEvents;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.TotalAvailableTickets = totalAvailableTickets;
        ViewBag.LowPriceEvents = lowPriceEvents;

        return View();
    }

    public async Task<IActionResult> BrowseEvent(string? category)
    {
        var categories = await _context.Events
            .AsNoTracking()
            .Where(e => !string.IsNullOrWhiteSpace(e.Category))
            .Select(e => e.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var events = _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            events = events.Where(e => e.Category == category);
        }

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category ?? string.Empty;

        return View(await events.ToListAsync());
    }

    public async Task<IActionResult> Search(
        string? searchTitle,
        string? category,
        DateTime? startDate,
        DateTime? endDate,
        string? ticketStatus,
        string? sortOrder)
    {
        searchTitle = searchTitle?.Trim();
        category = category?.Trim();

        var events = _context.Events
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTitle))
        {
            events = events.Where(e => EF.Functions.ILike(e.Title, $"%{searchTitle}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            events = events.Where(e => e.Category == category);
        }

        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            ViewBag.FilterError = "Start date must be on or before the end date.";
            return View(await BuildSearchResultsAsync(Enumerable.Empty<Event>(), searchTitle, category, startDate, endDate, ticketStatus, sortOrder));
        }

        if (startDate.HasValue)
        {
            var normalizedStart = startDate.Value.Date;
            events = events.Where(e => e.EventDate >= normalizedStart);
        }

        if (endDate.HasValue)
        {
            var normalizedEndExclusive = endDate.Value.Date.AddDays(1);
            events = events.Where(e => e.EventDate < normalizedEndExclusive);
        }

        if (!string.IsNullOrWhiteSpace(ticketStatus))
        {
            events = ticketStatus switch
            {
                "Available" => events.Where(e => e.AvailableTickets > 0),
                "SoldOut" => events.Where(e => e.AvailableTickets <= 0),
                _ => events
            };
        }

        events = (sortOrder ?? "Date") switch
        {
            "Title" => events.OrderBy(e => e.Title).ThenBy(e => e.EventDate),
            "Price" => events.OrderBy(e => e.TicketPrice).ThenBy(e => e.EventDate),
            _ => events.OrderBy(e => e.EventDate).ThenBy(e => e.Title)
        };

        return View(await BuildSearchResultsAsync(await events.ToListAsync(), searchTitle, category, startDate, endDate, ticketStatus, sortOrder));
    }

    private async Task<List<Event>> BuildSearchResultsAsync(
        IEnumerable<Event> results,
        string? searchTitle,
        string? category,
        DateTime? startDate,
        DateTime? endDate,
        string? ticketStatus,
        string? sortOrder)
    {
        var categories = await _context.Events
            .AsNoTracking()
            .Where(e => !string.IsNullOrWhiteSpace(e.Category))
            .Select(e => e.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category ?? string.Empty;
        ViewBag.SearchTitle = searchTitle ?? string.Empty;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewBag.TicketStatus = ticketStatus ?? string.Empty;
        ViewBag.SortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "Date" : sortOrder;

        return results.ToList();
    }

    private void ValidateEvent(Event eventItem)
    {
        if (eventItem.AvailableTickets > eventItem.Capacity)
        {
            ModelState.AddModelError(nameof(Event.AvailableTickets), "Available tickets cannot exceed the event capacity.");
        }
    }
}
