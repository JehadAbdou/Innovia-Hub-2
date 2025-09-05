using System;

namespace Backend.Models;

public class Booking
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string? TimeSlot { get; set; } // (08–10)

    public DateTime? StartTime { get; set; } 
    public DateTime? EndTime { get; set; }

    // Relations
    public required string UserId { get; set; }
    public User User { get; set; } = null!;

    public int ResourceTypeId { get; set; }
    public ResourceType ResourceType { get; set; } = null!;
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
}
