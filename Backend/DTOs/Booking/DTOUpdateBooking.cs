using System;

namespace Backend.DTOs.Booking
{
    public class DTOUpdateBooking
    {
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; }
    public int  ResourceTypeId { get; set;}
    }
}