using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventApp.Models
{
    public class Event
    {
        // Primary Key - Must match 'Id' used in the controller actions and filters.
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Event Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
        public string Location { get; set; } = string.Empty;

        // Unified Date and Time Property
        [Required(ErrorMessage = "Event Date and Time is required.")]
        [Display(Name = "Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime EventDate { get; set; }

        // Category property - Used in the Search and Browse actions.
        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        // Capacity property
        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 100000, ErrorMessage = "Capacity must be greater than zero.")]
        public int Capacity { get; set; }

        // Ticket Price property
        [Required(ErrorMessage = "Ticket Price is required.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.00, 10000.00, ErrorMessage = "Price must be between 0 and 10000.")]
        [Display(Name = "Ticket Price")]
        public decimal TicketPrice { get; set; }

        // AvailableTickets property 
        [Required]
        [Display(Name = "Available Tickets")]
        [Range(0, 100000, ErrorMessage = "Available Tickets cannot be negative.")]
        public int AvailableTickets { get; set; }


        // Navigation Property for Purchases (One-to-many relationship)
        // Ensure this uses the correct ICollection<T> type
        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    }
}