using artapi.Controllers;
using artapi.Data;
using artapi.DTOs;
using artapi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace artapi.Tests;

public class ArtistsControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly ArtistsController _controller;

    public ArtistsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _userManagerMock = CreateUserManagerMock();

        _controller = new ArtistsController(_context, _userManagerMock.Object);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        // Provide default values for all non-nullable parameters
        return new Mock<UserManager<User>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,        // IOptions<IdentityOptions>
            new Mock<IPasswordHasher<User>>().Object,           // IPasswordHasher<User>
            Array.Empty<IUserValidator<User>>(),                        // IUserValidator<User>[]
            Array.Empty<IPasswordValidator<User>>(),                    // IPasswordValidator<User>[]
            new Mock<ILookupNormalizer>().Object,              // ILookupNormalizer
            new Mock<IdentityErrorDescriber>().Object,         // IdentityErrorDescriber
            new Mock<IServiceProvider>().Object,               // IServiceProvider
            new Mock<ILogger<UserManager<User>>>().Object       // ILogger<UserManager<User>>
        );
    }


    // ----------------------------------------------------------
    // GET /api/artists
    // ----------------------------------------------------------
    [Fact]
    public async Task GetArtistsAsync_ReturnsAllArtists()
    {
        _context.Artists.Add(new Artist { Name = "A", Email = "a@a.com" });
        _context.Artists.Add(new Artist { Name = "B", Email = "b@b.com" });
        await _context.SaveChangesAsync();

        var result = await _controller.GetArtistsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<IEnumerable<ArtistReadDto>>(ok.Value, exactMatch: false);

        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task CreateArtist_ValidUser_CreatesArtist()
    {
        var user = new User { Id = "123", Email = "test@test.com" };

        _userManagerMock.Setup(m => m.FindByIdAsync("123"))
            .ReturnsAsync(user);

        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Artist"))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ArtistCreateDto
        {
            Name = "New Artist",
            Bio = "Bio",
            Email = "email@test.com",
            ImageUrl = "img.jpg",
            UserId = "123"
        };

        var result = await _controller.CreateArtist(dto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var artist = Assert.IsType<ArtistReadDto>(okResult.Value);

        Assert.NotNull(artist);
        Assert.Equal("New Artist", artist.Name);
        Assert.Equal("123", artist.UserId);
        Assert.Equal(UserRole.Artist, user.Role);
    }

    [Fact]
    public async Task CreateArtist_UserNotFound_ReturnsNotFound()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("missing"))
            .ReturnsAsync((User?)null);

        var dto = new ArtistCreateDto
        {
            Name = "New Artist",
            Email = "email@test.com",
            Bio = "Bio",
            ImageUrl = "img.jpg",
            UserId = "missing"
        };

        var result = await _controller.CreateArtist(dto);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("User not found", notFound.Value);
    }

    // ----------------------------------------------------------
    // PUT /api/artists/{id}
    // ----------------------------------------------------------
    [Fact]
    public async Task UpdateArtist_ValidArtist_UpdatesArtist()
    {
        var artist = new Artist
        {
            Name = "Old Name",
            Bio = "Old Bio",
            Email = "old@test.com",
            ImageUrl = "old.jpg"
        };

        _context.Artists.Add(artist);
        await _context.SaveChangesAsync();

        var dto = new ArtistUpdateDto
        {
            Name = "New Name",
            Bio = "New Bio",
            Email = "new@test.com",
            ImageUrl = "new.jpg"
        };

        var result = await _controller.UpdateArtistAsync(artist.Id, dto);

        Assert.IsType<OkResult>(result);

        var updated = await _context.Artists.FindAsync(artist.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New Bio", updated.Bio);
        Assert.Equal("new@test.com", updated.Email);
    }

    [Fact]
    public async Task UpdateArtist_NotFound_ReturnsNotFound()
    {
        var dto = new ArtistUpdateDto
        {
            Name = "X",
            Email = "x@x.com",
            Bio = "Bio",
            ImageUrl = "img.jpg"
        };

        var result = await _controller.UpdateArtistAsync(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundResult>(result);
    }

}