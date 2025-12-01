using EventApp.Data;
using EventApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; 
using System.Linq;
using System; // Required for DateTime and DateTime?

namespace EventApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Events (Publicly accessible)
        // No [Authorize] attribute here means anyone can view the list of events.
        public async Task<IActionResult> Index()
        {
            // Use ToListAsync() for asynchronous operation
            return View(await _context.Events.ToListAsync());
        }

        // GET: Events/Details/5 (Publicly accessible)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                // Use FirstOrDefaultAsync() for asynchronous operation
                .FirstOrDefaultAsync(m => m.Id == id); 
            
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // =========================================================================
        // AUTHORIZED ACTIONS (Restricted to Admin and Organizer Roles)
        // =========================================================================

        // GET: Events/Create
        [Authorize(Roles = "Admin,Organizer")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Create([Bind("Title,Description,EventDate,Location,Capacity,TicketPrice,Category,AvailableTickets")] Event @event)
        {
            if (ModelState.IsValid)
            {
                _context.Add(@event);
                // Use SaveChangesAsync() for asynchronous operation
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Edit/5
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Use FindAsync() for asynchronous operation
            var @event = await _context.Events.FindAsync(id); 
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Events/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,EventDate,Location,Capacity,TicketPrice,Category,AvailableTickets")] Event @event)
        {
            // IMPORTANT: Ensuring we check against the ID property
            if (id != @event.Id) 
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    // Use SaveChangesAsync() for asynchronous operation
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Events.AnyAsync(e => e.Id == id)) // Use AnyAsync
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Events/Delete/5
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                // Use FirstOrDefaultAsync() for asynchronous operation
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Use FindAsync() for asynchronous operation
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
            }

            // Use SaveChangesAsync() for asynchronous operation
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // PUBLIC QUERY ACTIONS (Integrated from user query)
        // =========================================================================

        // GET: Events/Overview (Publicly accessible)
        public async Task<IActionResult> Overview()
        {
            var totalEvents = await _context.Events.CountAsync(); // Use CountAsync
            var totalCategories = await _context.Events.Select(e => e.Category).Distinct().CountAsync(); // Use CountAsync
            
            // Note: Assuming 'TicketPrice' field exists on the Event model
            var lowPriceEvents = await _context.Events
                .Where(e => e.TicketPrice < 5) // < $5 tickets
                .OrderBy(e => e.TicketPrice)
                .ToListAsync(); // Use ToListAsync

            ViewBag.TotalEvents = totalEvents;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.LowPriceEvents = lowPriceEvents;
            

            return View();
        }

        // GET: Events/BrowseEvent?category=Music (Publicly accessible)
        public async Task<IActionResult> BrowseEvent(string category)
        {
            // Get all categories for dropdown
            var categories = await _context.Events
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(); // Use ToListAsync

            // Get events (filtered if category is selected)
            var events = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                events = events.Where(e => e.Category == category);
            }

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;

            return View(await events.ToListAsync()); // Use ToListAsync
        }

        // GET: Events/Search (Publicly accessible)
        public async Task<IActionResult> Search(
            string searchTitle, 
            string category, 
            DateTime? startDate, 
            DateTime? endDate, 
            string ticketStatus, 
            string sortOrder)
        {
            // Get all events
            var events = _context.Events.AsQueryable();

            // Filter by title
            if (!string.IsNullOrEmpty(searchTitle))
            {
                // Note: Using ToLowerInvariant() for case-insensitive search (DB specific)
                events = events.Where(e => e.Title.ToLower().Contains(searchTitle.ToLower())); 
            }

            // Filter by category
            if (!string.IsNullOrEmpty(category))
            {
                events = events.Where(e => e.Category == category);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                // CORRECTED: Using EventDate instead of Date
                events = events.Where(e => e.EventDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // CORRECTED: Using EventDate instead of Date
                events = events.Where(e => e.EventDate <= endDate.Value);
            }

            // Filter by ticket availability
            if (!string.IsNullOrEmpty(ticketStatus))
            {
                // CORRECTED: Using Capacity instead of AvailableTickets (assuming Capacity is the source of truth for ticket count)
                // Note: If you have a separate AvailableTickets property, please update the Event model accordingly.
                if (ticketStatus == "Available")
                    events = events.Where(e => e.Capacity > 0); 
                else if (ticketStatus == "SoldOut")
                    events = events.Where(e => e.Capacity == 0); 
            }

            // Sorting
            events = sortOrder switch
            {
                "Title" => events.OrderBy(e => e.Title),
                "Date" => events.OrderBy(e => e.EventDate), // CORRECTED: Using EventDate instead of Date
                "Price" => events.OrderBy(e => e.TicketPrice),
                _ => events.OrderBy(e => e.EventDate), // CORRECTED: Using EventDate instead of Date
            };

            // Get distinct categories for dropdown
            var categories = await _context.Events
                .Select(e => e.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(); // Use ToListAsync

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTitle = searchTitle;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.TicketStatus = ticketStatus;
            ViewBag.SortOrder = sortOrder;

            return View(await events.ToListAsync()); // Use ToListAsync
        }
    }
}