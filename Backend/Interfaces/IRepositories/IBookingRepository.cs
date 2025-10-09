using System;
using Backend.DTOs.Booking;
using Backend.Models;

namespace Backend.Interfaces.IRepositories;

public interface IBookingRepository
    {
    Task<IEnumerable<Booking>> GetAllBookingsByUser(string userId);
    Task<Booking> AddBookingAsync(DTOCreateBooking booking);
    Task<IEnumerable<Booking>> GetBookingByDate(DateTime date);
    Task<IEnumerable<Booking>> GetBookingByDateForUser(DateTime date,string userId);
    Task<Booking> GetBookingByDetailsAsync(string userId,DateTime date,string timeSlot,int resourceTypeId);
    Task<Booking> UpdateBookingAsync(int bookingId, DTOUpdateBooking dTOUpdate);
    Task<List<Resource>> GetAvailableResourcesAsync(int resourceId, DateTime date, string timeSlot);
    Task <bool>DeleteBooking(int bookingId);
}
