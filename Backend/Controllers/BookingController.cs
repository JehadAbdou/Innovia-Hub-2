using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Interfaces.IRepositories;
using System.Xml;
using Backend.DTOs.Booking;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using System.Diagnostics;
using Backend.DbContext;
namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IBookingRepository _bookingRepository;
    private readonly IHubContext<BookingHub> _hubContext;

    public BookingController(AppDbContext context, IBookingRepository bookingRepository, IHubContext<BookingHub> hubContext)
    {
        _context = context;
        _bookingRepository = bookingRepository;
        _hubContext = hubContext;
    }

    [HttpPost("{resourceTypeId}/freeSlots")]
    public async Task<IActionResult> GetFreeSlots(int resourceTypeId, [FromBody] BookingInfo bookingInfo)
    {
        var date = bookingInfo.Date;

        // Define all possible timeslots
        var allSlots = new List<string> { "08-10", "10-12", "12-14", "14-16", "16-18", "18-20" };
        var freeSlots = new List<string>();


        foreach (var slot in allSlots)
        {
            var availableResources = await _bookingRepository.GetAvailableResourcesAsync(resourceTypeId, date, slot);
            if (availableResources.Any())
            {
                freeSlots.Add(slot);
            }
        }

        return Ok(freeSlots);
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _context.Bookings
        .Include(b => b.User)
        .Include(b => b.ResourceType)
        .Include(b => b.Resource)
        .Select(b => new BookingsDto
        {
            bookingId = b.Id,
            userName = b.User.UserName,
            date = b.Date.ToString("yyyy-MM-dd"),
            timeSlot = b.TimeSlot,
            resourceType = b.ResourceType.ResourceTypeName,
            resourceName = b.Resource.ResourceName

        }).ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("date")]
    public async Task<IActionResult> GetBookingsByDate([FromQuery] DateTime date)
    {
        var bookings = await _bookingRepository.GetBookingByDate(date);
        var filteredBookings = bookings
        .Select(b => new BookingsDto
        {
            userName = b.User.UserName,
            bookingId = b.Id,
            date = b.Date.ToString("yyyy-MM-dd"),
            timeSlot = b.TimeSlot,
            resourceName = b.Resource.ResourceName,
            resourceType = b.ResourceType.ResourceTypeName
        });

        return Ok(filteredBookings);
    }

    // Hämta användarens bokningar
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserBookings(string userId)
    {
        var userBookings = await _context.Bookings
        .Include(b => b.Resource)
        .Where(b => b.UserId == userId)
        .Select(b => new UserBookingDTO
        {
            bookingId = b.Id,
            date = b.Date.ToString("yyyy-MM-dd"),
            timeSlot = b.TimeSlot,
            resourceName = b.Resource.ResourceName

        }).ToListAsync();

        return Ok(userBookings);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] DTOCreateBooking dto)
    {
        try
        {
            var booking = await _bookingRepository.AddBookingAsync(dto);

            // SignalR: skicka uppdatering till alla klienter
            await _hubContext.Clients.All.SendAsync("ReceiveBookingUpdate", new
            {
                date = booking.Date.ToString("yyyy-MM-dd"),
                timeSlot = booking.TimeSlot,
                resourceName = booking.Resource!.ResourceName,
                userId = booking.UserId
            });

            return Ok(new UserBookingDTO
            {
                bookingId = booking.Id,
                date = booking.Date.ToString("yyyy-MM-dd"),
                timeSlot = booking.TimeSlot,
                resourceName = booking.Resource!.ResourceName

            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var booking = await _context.Bookings.Include(b => b.Resource)
       .FirstOrDefaultAsync(b => b.Id == id);

        var deleteInfo =
        new
        {
            date = booking.Date.ToString("yyyy-MM-dd"),
            timeSlot = booking.TimeSlot,
            resourceName = booking.Resource!.ResourceName,
            userId = booking.UserId
        };
        var success = await _bookingRepository.DeleteBooking(id);
        if (!success) return NotFound();

        //SignalR: meddela att bokningen tagits bort
        await _hubContext.Clients.All.SendAsync("ReceiveDeleteBookingUpdate", deleteInfo);
        return NoContent();
    }

    //Hämta alla bokningar för en specifik resource
    [Authorize(Roles = "admin")]
    [HttpPost("{resourceTypeId}")]
    public async Task<IActionResult> GetResourceFreeSlots(int resourceTypeId, [FromBody] BookingInfo bookingInfo)
    {
        var date = bookingInfo.Date;
        var bookings = await _context.Bookings.Where(b => b.Date == date && b.ResourceTypeId == resourceTypeId)
        .Select(b => new BookingsDto
        {
            userName = b.User.UserName,
            date = b.Date.ToString(),
            timeSlot = b.TimeSlot,
            resourceType = b.ResourceType.ResourceTypeName,
            resourceName = b.Resource.ResourceName

        }).ToListAsync();
        if (bookings == null || bookings.Count == 0)
        {
            return NotFound(new { message = "No bookings under this day" });
        }

        return Ok(bookings);
    }

    //uppdatera status för en resource bokningsbar/ej bokningsbar
    [HttpPatch("resource/{resourceId}")]
    public async Task<IActionResult> ChangeResourceStatus(int resourceId)
    {
        var resource = await _context.Resources.FindAsync(resourceId);
        if (resource == null)
        {
            return NotFound(new { message = "Resource not found" });
        }

        // Toggle status
        resource.IsBookable = !resource.IsBookable;

        // Save first
        await _context.SaveChangesAsync();

        // Then broadcast
        await _hubContext.Clients.All.SendAsync("ReceiveResourceStatusUpdate", new
        {
            resourceId = resource.Id,
            isBookable = resource.IsBookable
        });
        Console.WriteLine($"📢 Broadcast sent for resource {resource.Id}, isBookable: {resource.IsBookable}");
        return Ok(new { message = "Resource status is changed" });
    }

}