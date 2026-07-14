using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using EventBoard.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventBoard.Api.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _jwtTokenServiceMock.Object,
            _loggerMock.Object);
    }
        [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsUserId()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.UserExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        User? createdUser = null;

        _userRepositoryMock
            .Setup(x => x.AddUserAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .Returns(Task.CompletedTask);

        // Act
        var id = await _authService.RegisterAsync(
            "Ahad",
            "ahad@test.com",
            "Password123");

        // Assert
        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(createdUser);
        Assert.Equal(id, createdUser!.Id);

        _userRepositoryMock.Verify(x =>
            x.AddUserAsync(It.IsAny<User>()),
            Times.Once);
    }
        [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ThrowsException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.UserExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterAsync(
                "Ahad",
                "ahad@test.com",
                "Password123"));

        _userRepositoryMock.Verify(x =>
            x.AddUserAsync(It.IsAny<User>()),
            Times.Never);
    }
        [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var password = "Password123";

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "Ahad",
            Email = "ahad@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns(("jwt-token", DateTime.UtcNow.AddHours(1)));

        // Act
        var result = await _authService.LoginAsync(user.Email, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("jwt-token", result!.Token);

        _jwtTokenServiceMock.Verify(x =>
            x.GenerateToken(user),
            Times.Once);
    }
        [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(
            "missing@test.com",
            "Password123");

        // Assert
        Assert.Null(result);
    }
        [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "Ahad",
            Email = "ahad@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = "User"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(
            user.Email,
            "WrongPassword");

        // Assert
        Assert.Null(result);

        _jwtTokenServiceMock.Verify(x =>
            x.GenerateToken(It.IsAny<User>()),
            Times.Never);
    }
}