using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketZ.Core.Models;

public class Purchase
{
    [Key]
    public int PurchaseId { get; set; }

    [StringLength(100)]
    [Display(Name = "Name")]
    public string GuestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email Address is required.")]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string GuestEmail { get; set; } = string.Empty;

    [Required]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal TotalCost { get; set; }

    [ForeignKey("Event")]
    public int EventId { get; set; }

    // Navigation property
    public Event? Event { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "You must purchase at least one ticket.")]
    public int Quantity { get; set; }
}
