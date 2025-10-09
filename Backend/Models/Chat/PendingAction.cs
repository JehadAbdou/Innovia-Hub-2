namespace Backend.Models.Chat
{
    public record PendingAction(
        string ActionType,
        int? BookingId = null,
        string? Date = null,
        string? TimeSlot = null,
        int? ResourceTypeId = null,
        string? ResourceTypeName = null
    );
}