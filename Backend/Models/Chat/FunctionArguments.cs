namespace Backend.Models.Chat
{
    public class BookingArguments
    {
        public string Date { get; set; } = "";
        public string TimeSlot { get; set; } = "";
        public int ResourceTypeId { get; set; }
    }

    public class EditBookingArguments
    {
        public string CurrentDate { get; set; } = "";
        public string CurrentTimeSlot { get; set; } = "";
        public int CurrentResourceTypeId { get; set; }
        public string NewDate { get; set; } = "";
        public string NewTimeSlot { get; set; } = "";
        public int NewResourceTypeId { get; set; }
    }

    public class ShowBookingsArguments
    {
        public string Date { get; set; } = "";
    }
}