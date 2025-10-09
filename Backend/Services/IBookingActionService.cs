using Backend.Models.Chat;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Services
{
    public interface IBookingActionService
    {
        Task<IActionResult> HandleCreateBookingAsync(
            string userId,
            BookingArguments bookingArgs,
            List<ChatMessage> history);

        Task<IActionResult> HandleDeleteBookingAsync(
            string userId,
            BookingArguments bookingArgs,
            List<ChatMessage> history);

        Task<IActionResult> HandleEditBookingAsync(
            string userId,
            EditBookingArguments editArgs,
            List<ChatMessage> history);

        Task<IActionResult> HandleShowBookingsAsync(
            string userId,
            ShowBookingsArguments? showArgs,
            List<ChatMessage> history);

    Task<IActionResult> ConfirmActionAsync(string userId, bool confirm, List<ChatMessage>? history = null);
    // Non-optional overload kept for test expression trees and older callers.
    Task<IActionResult> ConfirmActionAsync(string userId, bool confirm);
    }
}