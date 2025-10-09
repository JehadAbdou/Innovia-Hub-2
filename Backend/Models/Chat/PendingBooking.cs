namespace Backend.Models.Chat
{
    public record PendingBooking(
        string Date,
        string TimeSlot,
        int ResourceTypeId,
        string ResourceTypeName
    );
}