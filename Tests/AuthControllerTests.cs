using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using artapi.Controllers;
using artapi.Data;
using artapi.Models;
using Xunit;

namespace artapi.Tests;

public class AuthControllerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly AppDbContext _context;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        // Setup in-memory EF Core database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "AuthTestDb")
            .Options;
        _context = new AppDbContext(options);

        // Mock UserManager
        var store = new Mock<IUserStore<User>>();

        _userManagerMock = new Mock<UserManager<User>>(
            store.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<User>>().Object, 
            new List<IUserValidator<User>>(),         
            new List<IPasswordValidator<User>>(),    
            new Mock<ILookupNormalizer>().Object,     
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,      
            new Mock<ILogger<UserManager<User>>>().Object
        );

        // Mock IConfiguration for JWT
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("super_secret_testing_key_which_is_long_enough_123");

        // Controller instance
        _controller = new AuthController(_userManagerMock.Object, _configMock.Object, _context);
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsTokenAndUser()
    {
        // Arrange
        var userName = "Test";
        var email = "test@example.com";
        var dto = new RegisterDto(userName, email, "Password123!");

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as dynamic;
        Assert.NotNull(value!.token);
        Assert.Equal(email, value.user.Email);
    }

    [Fact]
    public async Task Register_InvalidUser_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterDto("Invalid user", "fail@example.com", "weak");

        _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<User>(), dto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Weak password" }));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsType<IEnumerable<IdentityError>>(badRequest.Value, exactMatch: false);
        Assert.Contains(errors, e => e.Description == "Weak password");
    }

    [Fact]
    public async Task Login_ValidUser_ReturnsTokenAndUser()
    {
        // Arrange
        var password = "Password123!";
        var user = new User { Email = "test@example.com", Role = UserRole.User, Id = "user1" };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, password))
            .ReturnsAsync(true);

        var dto = new LoginDto(user.Email, password);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value as dynamic;
        Assert.NotNull(value!.token);
        Assert.Equal(user.Email, value.user.Email);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = new User { Email = "fail@example.com", Role = UserRole.User, Id = "user2" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, "wrongpassword"))
            .ReturnsAsync(false);

        var dto = new LoginDto(user.Email, "wrongpassword");

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials.", ((dynamic)unauthorized.Value!).message);
    }

    [Fact]
    public async Task Login_NonexistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto("doesnotexist@example.com", "any");

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid credentials.", ((dynamic)unauthorized.Value!).message);
    }
}
