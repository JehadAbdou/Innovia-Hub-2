using System;

namespace Backend.DTOs.Booking
{
    public class AIBookingDTO
    {
         
            public string date { get; set; } = string.Empty;
            public string timeSlot { get; set; } = string.Empty;
            public int resourceTypeId { get; set; }
        
    }
}