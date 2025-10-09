using Backend.DTOs.Booking;
using Backend.Interfaces.IRepositories;
using Backend.Models;
using Backend.Models.Chat;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Services
{
    public class BookingActionService : IBookingActionService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IAiService _aiService;
        private static readonly Dictionary<string, PendingBooking> _pendingBookings = new();
        private static readonly Dictionary<string, PendingAction> _pendingActions = new();

        public BookingActionService(IBookingRepository bookingRepository, IAiService aiService)
        {
            _bookingRepository = bookingRepository;
            _aiService = aiService;
        }

        public async Task<IActionResult> HandleCreateBookingAsync(
            string userId,
            BookingArguments bookingArgs,
            List<ChatMessage> history)
        {
            var resourceTypeName = GetResourceTypeName(bookingArgs.ResourceTypeId);

            var pendingBooking = new PendingBooking(
                bookingArgs.Date,
                bookingArgs.TimeSlot,
                bookingArgs.ResourceTypeId,
                resourceTypeName
            );

            _pendingBookings[userId] = pendingBooking;
            _pendingActions[userId] = new PendingAction("create");

            var confirmationMessage = await _aiService.GetAiResponseAsync(
                history,
                "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                $"Write a friendly, natural message to the user to confirm a proposed booking of a {resourceTypeName} on {bookingArgs.Date} at {bookingArgs.TimeSlot}. Keep it short and pleasant. The current time is {DateTime.Now.Hour} and you cannot book time slots if the time has already passed. You cannot book by yourself - the user needs to press the confirm button.",
                "I found an available time!"
            );

            history.Add(new ChatMessage("assistant", confirmationMessage));

            return new OkObjectResult(new
            {
                answer = confirmationMessage,
                pendingBooking = pendingBooking,
                awaitingConfirmation = true,
                actionType = "create"
            });
        }

        public async Task<IActionResult> HandleDeleteBookingAsync(
            string userId,
            BookingArguments bookingArgs,
            List<ChatMessage> history)
        {
            var resourceTypeName = GetResourceTypeName(bookingArgs.ResourceTypeId);

            var existingBooking = await _bookingRepository.GetBookingByDetailsAsync(
                userId,
                DateTime.Parse(bookingArgs.Date),
                bookingArgs.TimeSlot,
                bookingArgs.ResourceTypeId
            );

            if (existingBooking == null)
            {
                var notFoundMessage = "I couldn't find a booking with those details.";
                history.Add(new ChatMessage("assistant", notFoundMessage));

                return new OkObjectResult(new
                {
                    answer = notFoundMessage,
                    pendingBooking = (PendingBooking?)null,
                    awaitingConfirmation = false
                });
            }

            _pendingActions[userId] = new PendingAction(
                "delete",
                BookingId: existingBooking.Id,
                Date: bookingArgs.Date,
                TimeSlot: bookingArgs.TimeSlot,
                ResourceTypeId: bookingArgs.ResourceTypeId,
                ResourceTypeName: resourceTypeName
            );

            var confirmationMessage = await _aiService.GetAiResponseAsync(
                history,
                "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                $"Write a friendly message asking the user to confirm deletion of their booking for {resourceTypeName} on {bookingArgs.Date} at {bookingArgs.TimeSlot}. Keep it short and ask for confirmation.",
                "Are you sure you want to delete this booking?"
            );

            history.Add(new ChatMessage("assistant", confirmationMessage));

            var pendingBooking = new PendingBooking(
                bookingArgs.Date,
                bookingArgs.TimeSlot,
                bookingArgs.ResourceTypeId,
                resourceTypeName
            );

            return new OkObjectResult(new
            {
                answer = confirmationMessage,
                pendingBooking = pendingBooking,
                awaitingConfirmation = true,
                actionType = "delete"
            });
        }

        public async Task<IActionResult> HandleEditBookingAsync(
            string userId,
            EditBookingArguments editArgs,
            List<ChatMessage> history)
        {
            var existingBooking = await _bookingRepository.GetBookingByDetailsAsync(
                userId,
                DateTime.Parse(editArgs.CurrentDate),
                editArgs.CurrentTimeSlot,
                editArgs.CurrentResourceTypeId
            );

            if (existingBooking == null)
            {
                var notFoundMessage = "I couldn't find a booking with those details to edit.";
                history.Add(new ChatMessage("assistant", notFoundMessage));

                return new OkObjectResult(new
                {
                    answer = notFoundMessage,
                    pendingBooking = (PendingBooking?)null,
                    awaitingConfirmation = false
                });
            }

            var newResourceTypeName = GetResourceTypeName(editArgs.NewResourceTypeId);

            _pendingActions[userId] = new PendingAction(
                "edit",
                BookingId: existingBooking.Id,
                Date: editArgs.NewDate,
                TimeSlot: editArgs.NewTimeSlot,
                ResourceTypeId: editArgs.NewResourceTypeId,
                ResourceTypeName: newResourceTypeName
            );

            var currentResourceName = GetResourceTypeName(editArgs.CurrentResourceTypeId);

            var confirmationMessage = await _aiService.GetAiResponseAsync(
                history,
                "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                $"Write a friendly message asking the user to confirm changing their booking from {currentResourceName} on {editArgs.CurrentDate} at {editArgs.CurrentTimeSlot} to {newResourceTypeName} on {editArgs.NewDate} at {editArgs.NewTimeSlot}. Keep it short and clear.",
                "Would you like to change your booking?"
            );

            history.Add(new ChatMessage("assistant", confirmationMessage));

            var pendingBooking = new PendingBooking(
                editArgs.NewDate,
                editArgs.NewTimeSlot,
                editArgs.NewResourceTypeId,
                newResourceTypeName
            );

            return new OkObjectResult(new
            {
                answer = confirmationMessage,
                pendingBooking,
                awaitingConfirmation = true,
                actionType = "edit"
            });
        }

        public async Task<IActionResult> HandleShowBookingsAsync(
            string userId,
            ShowBookingsArguments? showArgs,
            List<ChatMessage> history)
        {
            IEnumerable<Booking> bookingsList;

            if (!string.IsNullOrEmpty(showArgs?.Date) && DateTime.TryParse(showArgs.Date, out var date))
            {
                bookingsList = await _bookingRepository.GetBookingByDateForUser(date, userId);
            }
            else
            {
                bookingsList = await _bookingRepository.GetAllBookingsByUser(userId);
            }

            string bookingsInfo;
            if (!bookingsList.Any())
            {
                bookingsInfo = "No bookings found.";
            }
            else
            {
                var bookingDescriptions = new List<string>();
                foreach (var booking in bookingsList)
                {
                    bookingDescriptions.Add($"- {booking.Date:yyyy-MM-dd} at {booking.TimeSlot}, Resource: {GetResourceTypeName(booking.ResourceTypeId)} (ID: {booking.Id})");
                }
                bookingsInfo = string.Join("\n", bookingDescriptions);
            }

            var confirmationMessage = await _aiService.GetAiResponseAsync(
                history,
                "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                $"Here are the bookings:\n{bookingsInfo}\n\nPresent these in a friendly, clear format to the user.",
                "Here are your bookings:"
            );

            history.Add(new ChatMessage("assistant", confirmationMessage));

            return new OkObjectResult(new
            {
                answer = confirmationMessage,
                pendingBooking = (PendingBooking?)null,
                awaitingConfirmation = false,
                actionType = (string?)null
            });
        }

        public async Task<IActionResult> ConfirmActionAsync(string userId, bool confirm, List<ChatMessage>? history = null)
        {       
            Console.WriteLine($"ConfirmAction called for user: {userId}, confirm: {confirm}");
            Console.WriteLine($"Pending actions keys: {string.Join(", ", _pendingActions.Keys)}");

            if (!_pendingActions.TryGetValue(userId, out var action))
                return new BadRequestObjectResult("No pending action found.");

            var convo = history ?? new List<ChatMessage>();

            if (!confirm)
            {
                _pendingActions.Remove(userId);
                _pendingBookings.Remove(userId);

                var cancelMessage = await _aiService.GetAiResponseAsync(
                    convo,
                    "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                    "Write a short, friendly message confirming that the requested action was cancelled.",
                    "Action cancelled."
                );

                return new OkObjectResult(new { message = cancelMessage });
            }

            try
            {

                        Console.WriteLine($"_pendingActions keys: {string.Join(", ", _pendingActions.Keys)}");
                        Console.WriteLine($"Received confirm: {confirm},{action.ActionType}");
                if (action.ActionType == "create")
                {
                    if (!_pendingBookings.TryGetValue(userId, out var pending))
                        return new BadRequestObjectResult("No pending booking found.");

                    var dtoBooking = new DTOCreateBooking
                    {
                        Date = DateTime.Parse(pending.Date),
                        TimeSlot = pending.TimeSlot,
                        ResourceTypeId = pending.ResourceTypeId,
                        UserId = userId
                    };

                    try
                    {
                        var booking = await _bookingRepository.AddBookingAsync(dtoBooking);
                        _pendingBookings.Remove(userId);
                        _pendingActions.Remove(userId);

                        var confirmCreateMessage = await _aiService.GetAiResponseAsync(
                            convo,
                            "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                            $"Write a short friendly confirmation message: the booking for {pending.ResourceTypeName} on {pending.Date} at {pending.TimeSlot} has been created.",
                            $"Booking confirmed! {pending.ResourceTypeName} on {pending.Date} at {pending.TimeSlot}."
                        );

                        return new OkObjectResult(new { message = confirmCreateMessage, bookingId = booking.Id });
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("No available resources"))
                    {
                        // Generate a localized 'time slot already taken' message using AI service
                        var takenMessage = await _aiService.GetAiResponseAsync(
                            convo,
                            "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                            $"Inform the user in a short friendly sentence that the selected time slot {pending.TimeSlot} on {pending.Date} is already taken and suggest choosing another time or date.",
                            "Sorry, that time slot is already taken. Please choose another time or date."
                        );

                        // Clean up pending state
                        _pendingBookings.Remove(userId);
                        _pendingActions.Remove(userId);

                        return new OkObjectResult(new { message = takenMessage });
                    }
                }
                else if (action.ActionType == "delete")
                {
                    if (action.BookingId == null)
                        return new BadRequestObjectResult("Invalid booking ID.");

                    await _bookingRepository.DeleteBooking(action.BookingId.Value);
                    _pendingActions.Remove(userId);

                    var confirmDeleteMessage = await _aiService.GetAiResponseAsync(
                        convo,
                        "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                        $"Write a short friendly message confirming deletion: the booking for {action.ResourceTypeName} on {action.Date} at {action.TimeSlot} has been deleted.",
                        $"Booking deleted successfully. {action.ResourceTypeName} on {action.Date} at {action.TimeSlot}."
                    );

                    return new OkObjectResult(new { message = confirmDeleteMessage });
                }
                else if (action.ActionType == "edit")
                {
                    if (action.BookingId == null || action.Date == null || action.TimeSlot == null || action.ResourceTypeId == null)
                        return new BadRequestObjectResult("Invalid edit data.");

                    var dtoUpdate = new DTOUpdateBooking
                    {
                        Date = DateTime.Parse(action.Date),
                        TimeSlot = action.TimeSlot,
                        ResourceTypeId = action.ResourceTypeId.Value
                    };

                    await _bookingRepository.UpdateBookingAsync(action.BookingId.Value, dtoUpdate);
                    _pendingActions.Remove(userId);
                    _pendingBookings.Remove(userId);

                    var confirmEditMessage = await _aiService.GetAiResponseAsync(
                        convo,
                        "You are a helpful booking assistant for InnoviaHub. Reply in the language the user speaks.",
                        $"Write a short friendly message confirming the update: new booking is {action.ResourceTypeName} on {action.Date} at {action.TimeSlot}.",
                        $"Booking updated! New booking: {action.ResourceTypeName} on {action.Date} at {action.TimeSlot}."
                    );

                    return new OkObjectResult(new { message = confirmEditMessage });
                }

                return new BadRequestObjectResult("Unknown action type.");
            }
            catch (Exception ex)
            {
                return new ObjectResult(new { error = "Failed to process action", details = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }

        // Backwards-compatible overload without optional parameter to support expression trees in tests.
        public Task<IActionResult> ConfirmActionAsync(string userId, bool confirm)
        {
            return ConfirmActionAsync(userId, confirm, null);
        }

        private string GetResourceTypeName(int resourceTypeId) => resourceTypeId switch
        {
            1 => "drop-in-skrivbord",
            2 => "mötesrum",
            3 => "VR-headset",
            4 => "AI-server",
            _ => "resurs"
        };
    }
}