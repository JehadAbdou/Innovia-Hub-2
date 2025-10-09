using Xunit;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using Backend.Interfaces.IRepositories;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Backend.Models.Chat;
using Backend.DTOs.Booking;

namespace InnoviaHub.Tests;

public class UserTests
{
    [Fact] // Test för REGISTRERING med GILTIGA uppgifter
    public void RegisterUser_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        var password = "SecurePassword123";
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        // Act
        var isValid = !string.IsNullOrEmpty(user.Name) &&
                    !string.IsNullOrEmpty(user.Email) &&
                    !string.IsNullOrEmpty(user.PasswordHash);

        // Assert
        Assert.True(isValid, "User registration should succeed with valid data.");
    }

    [Fact] // Test för REGISTRERING med SAKNADE uppgifter
    public void RegisterUser_WithMissingData_ShouldFail()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = null, // OM EMAIL SAKNAS
        };

        var passwordHasher = new PasswordHasher<User>();
        var password = "SecurePassword123";
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        // Act
        var isValid = !string.IsNullOrEmpty(user.Name) &&
                    !string.IsNullOrEmpty(user.Email) &&
                    !string.IsNullOrEmpty(user.PasswordHash);

        // Assert
        Assert.False(isValid, "User registration should fail if required fields are missing.");
    }

    [Fact] // Test för INLOGGNING med GILTIGA uppgifter
    public void LoginUser_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        var password = "SecurePassword123";
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        var inputPassword = "SecurePassword123";

        // Act
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, inputPassword);
        var canLogin = passwordVerificationResult == PasswordVerificationResult.Success;

        // Assert
        Assert.True(canLogin, "User login should succeed with valid credentials.");
    }

    [Fact] // Test för INLOGGNING med OGILTIGA uppgifter
    public void LoginUser_WithInvalidCredentials_ShouldFail()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        var password = "SecurePassword123";
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        var inputPassword = "WrongPassword";

        // Act
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, inputPassword);
        var canLogin = passwordVerificationResult == PasswordVerificationResult.Success;

        // Assert
        Assert.False(canLogin, "User login should fail with invalid credentials.");
    }
    [Fact]
    public async Task HandleCreateBookingAsync_WhenResourcesAvailable_ReturnsPendingBooking()
    {
        // Arrange
        var repoMock = new Mock<IBookingRepository>();
        var aiMock = new Mock<IAiService>();

        var bookingArgs = new BookingArguments { Date = "2025-10-10", TimeSlot = "08:00-10:00", ResourceTypeId = 2 };
        var history = new List<ChatMessage>();

        aiMock.Setup(a => a.GetAiResponseAsync(It.IsAny<List<ChatMessage>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync("Please confirm the booking?");

        var sut = new BookingActionService(repoMock.Object, aiMock.Object);

        // Act
        var result = await sut.HandleCreateBookingAsync("user1", bookingArgs, history);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic value = ok.Value;
        Assert.True((bool)value.awaitingConfirmation);
        Assert.NotNull(value.pendingBooking);
    }
    
        [Fact]
    public async Task HandleCreateBookingAsync_WhenNoResources_ReturnsTakenMessage()
    {
        var repoMock = new Mock<IBookingRepository>();
        var aiMock = new Mock<IAiService>();

        var bookingArgs = new BookingArguments { Date = "2025-10-10", TimeSlot = "08:00-10:00", ResourceTypeId = 2 };
        var history = new List<ChatMessage>();

        
        aiMock.Setup(a => a.GetAiResponseAsync(It.IsAny<List<ChatMessage>>(),
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("confirm a proposed booking")),
            It.IsAny<string>()))
            .ReturnsAsync("Please confirm");

        
        repoMock.Setup(r => r.AddBookingAsync(It.IsAny<DTOCreateBooking>()))
            .ThrowsAsync(new InvalidOperationException("No available resources for the selected type, date, and timeslot."));

        var sut = new BookingActionService(repoMock.Object, aiMock.Object);

        
        var initial = await sut.HandleCreateBookingAsync("user1", bookingArgs, history);
        
        var confirmResult = await sut.ConfirmActionAsync("user1", true, history);

        var ok = Assert.IsType<OkObjectResult>(confirmResult);
        dynamic value = ok.Value;
        string msg = value.message;
        Assert.Contains("time slot", msg.ToLower()); // or assert specific localized text if mocked
    }
}