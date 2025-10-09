using System;
using Backend.DTOs.Booking;
using Backend.Interfaces.IRepositories;
using Backend.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Backend.DbContext;
using Microsoft.AspNetCore.Mvc;
namespace Backend.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;
    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }
    
    // Normalize time slot formats so comparisons work whether AI or frontend sends "08:00-10:00" or "08-10"
    private string NormalizeTimeSlot(string timeSlot)
    {
        if (string.IsNullOrWhiteSpace(timeSlot)) return timeSlot;
        // Convert things like "08:00-10:00" => "08-10"
        var parts = timeSlot.Split('-');
        if (parts.Length != 2) return timeSlot.Trim();

        string NormalizePart(string p)
        {
            p = p.Trim();
            // remove minutes if present (":00") or any ':'
            p = p.Replace(":00", "").Replace(":", "");
            return p;
        }

        var left = NormalizePart(parts[0]);
        var right = NormalizePart(parts[1]);
        return $"{left}-{right}";
    }
    public async Task<IEnumerable<Booking>> GetAllBookingsByUser(string id)
    {
        var bookings = await _context.Bookings.Where(b => b.UserId == id).ToListAsync();

        return bookings;
    }
       public async Task<IEnumerable<Booking>> GetBookingByDateForUser(DateTime date, string Id)
    {
        var bookings = await _context.Bookings.Where(b =>b.UserId==Id)
        .Include(b => b.Resource)
        .Include(b => b.ResourceType)
        .Include(b => b.User)
        .Where(b => b.Date == date)
        .ToListAsync();
        if (bookings is null)
        {
            return [];
        }

        return bookings;

    }

    public async Task<Booking> AddBookingAsync(DTOCreateBooking booking)
    {
        // Normalize incoming timeslot so availability checks and storage are consistent
        booking.TimeSlot = NormalizeTimeSlot(booking.TimeSlot);
        var resourcesList = await GetAvailableResourcesAsync(booking.ResourceTypeId, booking.Date, booking.TimeSlot);
        if (!resourcesList.Any())
        {
            throw new InvalidOperationException("No available resources for the selected type, date, and timeslot.");
        }

        var resource = GetResourceByIdAsync(resourcesList);
        if (resource == null)
        {
            throw new InvalidOperationException("No resource found to assign to booking.");
        }

        var newBooking = new Booking
        {
            Date = booking.Date,
            TimeSlot = booking.TimeSlot,
            ResourceTypeId = booking.ResourceTypeId,
            UserId = booking.UserId,
            ResourceId = resource.Id,
        };

        _context.Bookings.Add(newBooking);
        await _context.SaveChangesAsync();
        return newBooking;
    }


    public async Task<List<Resource>> GetAvailableResourcesAsync(
                int resourceTypeId, DateTime date, string timeSlot)
    {
        // Normalize incoming timeslot before querying so comparisons match stored format
        var normalized = NormalizeTimeSlot(timeSlot ?? string.Empty);

        var availableResources = await _context.Resources
            .Where(r => r.IsBookable && r.ResourceTypeId == resourceTypeId)
            .Include(r => r.Bookings)
            .ToListAsync();

        // Filter out resources that have a conflicting booking for the same date and normalized timeslot
        var filtered = availableResources
            .Where(r => !r.Bookings.Any(b => b.Date == date && NormalizeTimeSlot(b.TimeSlot ?? string.Empty) == normalized))
            .ToList();

    return filtered;
    }

    public async Task<IEnumerable<Booking>> GetBookingByDate(DateTime date)
    {
        var bookings = await _context.Bookings
        .Include(b => b.Resource)
        .Include(b => b.ResourceType)
        .Include(b => b.User)
        .Where(b => b.Date == date)
        .ToListAsync();
        if (bookings is null)
        {
            return [];
        }

        return bookings;

    }



    public Resource? GetResourceByIdAsync(List<Resource> resources)
    {
        return resources.FirstOrDefault();

    }

    public async Task<bool> DeleteBooking(int bookingId)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) return false;

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Booking> GetBookingByDetailsAsync(string userId, DateTime date, string timeSlot, int resourceTypeId)
    {
        var normalized = NormalizeTimeSlot(timeSlot ?? string.Empty);

        // Query candidate bookings and compare normalized timeslot in memory to avoid EF translation issues
        var candidates = await _context.Bookings
            .Where(b => b.UserId == userId && b.Date == date && b.ResourceTypeId == resourceTypeId)
            .ToListAsync();

        var booking = candidates.FirstOrDefault(b => NormalizeTimeSlot(b.TimeSlot ?? string.Empty) == normalized);
        return booking!;
    }

    public async Task<Booking> UpdateBookingAsync(int bookingId, DTOUpdateBooking dTOUpdate)
    {
        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null)
        {
            throw new InvalidOperationException($"Booking with ID {bookingId} not found.");
        }
        // Normalize incoming timeslot for availability check and storage
        dTOUpdate.TimeSlot = NormalizeTimeSlot(dTOUpdate.TimeSlot);
        var availableResources = await GetAvailableResourcesAsync(
            dTOUpdate.ResourceTypeId,
            dTOUpdate.Date,
            dTOUpdate.TimeSlot
        );
        if (!availableResources.Any())
        {
            throw new InvalidOperationException("No available resources for the new booking details.");
        }
        var newResource = GetResourceByIdAsync(availableResources);
        if (newResource == null)
            throw new InvalidOperationException("No resource found to assign to booking.");

        booking.Date = dTOUpdate.Date;
        booking.TimeSlot = dTOUpdate.TimeSlot;
        booking.ResourceTypeId = dTOUpdate.ResourceTypeId;
        booking.ResourceId = newResource.Id;
        await _context.SaveChangesAsync();
        return booking;
        
    }
}
