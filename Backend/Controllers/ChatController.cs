using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.Models.Chat;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBookingActionService _bookingActionService;
        private static readonly Dictionary<string, List<ChatMessage>> _userConversations = new();

        public ChatController(
            IHttpClientFactory httpClientFactory,
            IBookingActionService bookingActionService)
        {
            _httpClientFactory = httpClientFactory;
            _bookingActionService = bookingActionService;
        }

        public record ChatRequest(string Question);
        public record ConfirmActionRequest(bool Confirm);

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "User";

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            if (!_userConversations.ContainsKey(userId))
                _userConversations[userId] = new List<ChatMessage>();

            var history = _userConversations[userId];
            history.Add(new ChatMessage("user", request.Question));

            var http = _httpClientFactory.CreateClient("openai");

            var body = new
            {
                model = "gpt-4.1",
                input = history.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }).ToArray(),
                tools = new object[]
                {
                    new
                    {
                        type = "function",
                        name = "create_booking",
                        description = $"Propose a booking for a resource at InnoviaHub. Today's date is {DateTime.Now:yyyy-MM-dd} and the current hour is {DateTime.Now.Hour}. You cannot book time slots if the time has already passed. Speak in the language the user uses.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                date = new { type = "string", description = "Date in YYYY-MM-DD format" },
                                timeSlot = new
                                {
                                    type = "string",
                                    @enum = new[] { "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00", "16:00-18:00", "18:00-20:00" }
                                },
                                resourceTypeId = new { type = "integer", description = "1=Desk,2=Meeting room,3=VR Headset,4=AI Server" }
                            },
                            required = new[] { "date", "timeSlot", "resourceTypeId" }
                        }
                    },
                    new
                    {
                        type = "function",
                        name = "delete_booking",
                        description = $"Delete an existing booking. When the user references a booking ID from a previously shown list, extract the corresponding date, time slot, and resource type from the conversation history. The user must provide or you must infer the booking details (date, time slot, and resource type). Today's date is {DateTime.Now:yyyy-MM-dd}. Speak in the language the user uses.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                date = new { type = "string", description = "Date in YYYY-MM-DD format. Extract from conversation history if user mentions a booking ID." },
                                timeSlot = new
                                {
                                    type = "string",
                                    @enum = new[] { "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00", "16:00-18:00", "18:00-20:00" },
                                    description = "Extract from conversation history if user mentions a booking ID."
                                },
                                resourceTypeId = new { type = "integer", description = "1=Desk,2=Meeting room,3=VR Headset,4=AI Server. Extract from conversation history if user mentions a booking ID." }
                            },
                            required = new[] { "date", "timeSlot", "resourceTypeId" }
                        }
                    },
                    new
                    {
                        type = "function",
                        name = "edit_booking",
                        description = $"Edit an existing booking. The user must provide the current booking details and the new desired details. Today's date is {DateTime.Now:yyyy-MM-dd} and the current hour is {DateTime.Now.Hour}. You cannot book time slots if the time has already passed. Speak in the language the user uses.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                currentDate = new { type = "string", description = "Current booking date in YYYY-MM-DD format" },
                                currentTimeSlot = new
                                {
                                    type = "string",
                                    @enum = new[] { "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00", "16:00-18:00", "18:00-20:00" }
                                },
                                currentResourceTypeId = new { type = "integer", description = "Current resource type: 1=Desk,2=Meeting room,3=VR Headset,4=AI Server" },
                                newDate = new { type = "string", description = "New booking date in YYYY-MM-DD format" },
                                newTimeSlot = new
                                {
                                    type = "string",
                                    @enum = new[] { "08:00-10:00", "10:00-12:00", "12:00-14:00", "14:00-16:00", "16:00-18:00", "18:00-20:00" }
                                },
                                newResourceTypeId = new { type = "integer", description = "New resource type: 1=Desk,2=Meeting room,3=VR Headset,4=AI Server" }
                            },
                            required = new[] { "currentDate", "currentTimeSlot", "currentResourceTypeId", "newDate", "newTimeSlot", "newResourceTypeId" }
                        }
                    },
                    new
                    {
                        type = "function",
                        name = "show_bookings",
                        description = "Show the user bookings or bookings under a specific date. IMPORTANT: Always include the booking ID in your response so users can refer to it for deletion or editing.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                date = new { type = "string", description = "if the user gives a specific date use it but if the user want to show all the bookings show them YYYY-MM-DD format" }
                            }
                        }
                    }
                },
                tool_choice = "auto"
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await http.PostAsync("responses", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error = "OpenAI API error", details = error });
            }

            var raw = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(raw);
            var outputArray = doc.RootElement.GetProperty("output");

            if (outputArray.GetArrayLength() > 0)
            {
                var firstOutput = outputArray[0];
                var outputType = firstOutput.GetProperty("type").GetString();

                if (outputType == "function_call")
                {
                    var functionName = firstOutput.GetProperty("name").GetString();
                    var arguments = firstOutput.GetProperty("arguments").GetString();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    if (functionName == "create_booking" && !string.IsNullOrEmpty(arguments))
                    {
                        var bookingArgs = JsonSerializer.Deserialize<BookingArguments>(arguments, options);
                        if (bookingArgs != null)
                            return await _bookingActionService.HandleCreateBookingAsync(userId, bookingArgs, history);
                    }
                    else if (functionName == "delete_booking" && !string.IsNullOrEmpty(arguments))
                    {
                        var bookingArgs = JsonSerializer.Deserialize<BookingArguments>(arguments, options);
                        Console.WriteLine(bookingArgs);
                        if (bookingArgs != null)
                            return await _bookingActionService.HandleDeleteBookingAsync(userId, bookingArgs, history);
                    }
                    else if (functionName == "edit_booking" && !string.IsNullOrEmpty(arguments))
                    {
                        var editArgs = JsonSerializer.Deserialize<EditBookingArguments>(arguments, options);
                        if (editArgs != null)
                            return await _bookingActionService.HandleEditBookingAsync(userId, editArgs, history);
                    }
                    else if (functionName == "show_bookings")
                    {
                        ShowBookingsArguments? showArgs = null;
                        if (!string.IsNullOrEmpty(arguments))
                            showArgs = JsonSerializer.Deserialize<ShowBookingsArguments>(arguments, options);
                        return await _bookingActionService.HandleShowBookingsAsync(userId, showArgs, history);
                    }
                }
                else if (outputType == "message")
                {
                    string textContent = "Sorry, I didn't understand that.";

                    if (firstOutput.TryGetProperty("content", out var contentArray))
                    {
                        if (contentArray.ValueKind == JsonValueKind.Array && contentArray.GetArrayLength() > 0)
                        {
                            var firstContent = contentArray[0];
                            if (firstContent.TryGetProperty("text", out var textProp))
                            {
                                textContent = textProp.GetString() ?? textContent;
                            }
                        }
                        else if (contentArray.ValueKind == JsonValueKind.String)
                        {
                            textContent = contentArray.GetString() ?? textContent;
                        }
                    }

                    history.Add(new ChatMessage("assistant", textContent));

                    return Ok(new
                    {
                        answer = textContent,
                        userName = userName,
                        pendingBooking = (PendingBooking?)null,
                        awaitingConfirmation = false
                    });
                }
            }

            var fallbackMessage = "Sorry, I couldn't process your request.";
            history.Add(new ChatMessage("assistant", fallbackMessage));

            return Ok(new
            {
                answer = fallbackMessage,
                userName,
                pendingBooking = (PendingBooking?)null,
                awaitingConfirmation = false
            });
        }

        [HttpPost("confirmAction")]
        public async Task<IActionResult> ConfirmAction([FromBody] ConfirmActionRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            // Get conversation history for this user so the AI can reply in the same language/context
            if (!_userConversations.ContainsKey(userId))
                _userConversations[userId] = new List<ChatMessage>();

            var history = _userConversations[userId];

            return await _bookingActionService.ConfirmActionAsync(userId, request.Confirm, history);
        }

        [HttpPost("speak")]
        public async Task<IActionResult> Speak([FromBody] SpeakRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { error = "Text cannot be empty" });

            var client = _httpClientFactory.CreateClient("openai");

            var body = new
            {
                model = "gpt-4o-mini-tts",
                input = request.Text,
                voice = "alloy"
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("audio/speech", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();
            return File(audioBytes, "audio/mpeg");
        }

        public record SpeakRequest(string Text);
    }
}