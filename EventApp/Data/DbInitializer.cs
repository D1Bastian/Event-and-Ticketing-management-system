using EventApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace EventApp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Create DB if it doesn't exist (assuming migrations apply it) // It's applied in Program.cs
            
            // Check if there are any events already
            if (context.Events.Any())
            {
                return;   // DB has been seeded
            }

            var events = new Event[]
            {
                new Event
                {
                    Title = "Summer Beach BBQ Day",
                    Description = "Join us for the ultimate summer blowout! Enjoy amazing BBQ, cold drinks, beach volleyball, and live DJ sets until sunset.",
                    EventDate = DateTime.UtcNow.AddDays(15),
                    Location = "Sunny Beach Cove",
                    TicketPrice = 25.00m,
                    Capacity = 150,
                    AvailableTickets = 150,
                    Category = "Social"
                },
                new Event
                {
                    Title = "Global Tech Hackathon 2026",
                    Description = "A 48-hour coding marathon where the best minds collaborate to build innovative AI and Web3 solutions. Mentors, food, and prizes provided!",
                    EventDate = DateTime.UtcNow.AddDays(30),
                    Location = "Innovation Hub Downtown",
                    TicketPrice = 15.00m,
                    Capacity = 300,
                    AvailableTickets = 300,
                    Category = "Technology"
                },
                new Event
                {
                    Title = "Neon Nights VIP Party",
                    Description = "An exclusive underground party featuring top electronic music producers, neon art installations, and a premium open bar.",
                    EventDate = DateTime.UtcNow.AddDays(5),
                    Location = "The Warehouse Club",
                    TicketPrice = 75.00m,
                    Capacity = 500,
                    AvailableTickets = 50, // almost sold out
                    Category = "Entertainment"
                },
                new Event
                {
                    Title = "Startup Founders Meetup",
                    Description = "Network with local entrepreneurs, angel investors, and VCs. Pitch your ideas in a relaxed environment.",
                    EventDate = DateTime.UtcNow.AddDays(10),
                    Location = "City Convention Center",
                    TicketPrice = 0.00m, // Free
                    Capacity = 100,
                    AvailableTickets = 100,
                    Category = "Business"
                },
                new Event
                {
                    Title = "Jazz & Wine Night",
                    Description = "Experience a relaxing evening with a live jazz quartet accompanied by a selection of fine regional wines and cheeses.",
                    EventDate = DateTime.UtcNow.AddDays(22),
                    Location = "Grand Plaza Hotel Lounge",
                    TicketPrice = 45.00m,
                    Capacity = 80,
                    AvailableTickets = 80,
                    Category = "Music"
                }
            };

            foreach (var e in events)
            {
                context.Events.Add(e);
            }
            
            context.SaveChanges();
        }
    }
}
