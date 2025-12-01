using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventApp.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }
        
        // Guest info
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string GuestName { get; set; } = string.Empty; // Initialized for non-nullable safety

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string GuestEmail { get; set; } = string.Empty; // Initialized for non-nullable safety

        // Purchase info
        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalCost { get; set; }

        // Event relationship
        [ForeignKey("Event")]
        public int EventId { get; set; }

        // Navigation property
        public Event? Event { get; set; }

        // Number of tickets purchased
        [Required]
        [Range(1, 100, ErrorMessage = "You must purchase at least one ticket.")]
        public int Quantity { get; set; }
    }
}