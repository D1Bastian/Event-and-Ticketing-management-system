using System.ComponentModel.DataAnnotations;

namespace EventApp.Models;

public class PurchaseRequestViewModel
{
    public int EventId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    [Display(Name = "Ticket Price")]
    public decimal TicketPrice { get; set; }

    [Display(Name = "Available Tickets")]
    public int AvailableTickets { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string GuestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string GuestEmail { get; set; } = string.Empty;

    [Required]
    [Range(1, 100, ErrorMessage = "You must purchase between 1 and 100 tickets.")]
    public int Quantity { get; set; } = 1;
}
