using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using artapi.Controllers;
using artapi.Data;
using artapi.DTOs;
using artapi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        return new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null
        );
    }

    private void SetAdminUser()
    {
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Role, "Admin")
            },
            "mockAuth"
        ));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminUser }
        };
    }

    private void SetNonAdminUser()
    {
        var normalUser = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Role, "User")
            },
            "mockAuth"
        ));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = normalUser }
        };
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
        var list = Assert.IsAssignableFrom<IEnumerable<ArtistReadDto>>(ok.Value);

        Assert.Equal(2, list.Count());
    }

    // ----------------------------------------------------------
    // POST /api/artists  (Admin only)
    // ----------------------------------------------------------
    [Fact]
    public async Task CreateArtist_AdminUser_CreatesArtist()
    {
        SetAdminUser();

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

        var ok = Assert.IsType<OkObjectResult>(result);
        dynamic payload = ok.Value;

        Assert.NotNull(payload.artistId);

        var dbArtist = await _context.Artists.FindAsync((Guid)payload.artistId);
        Assert.Equal("New Artist", dbArtist.Name);
        Assert.Equal("123", dbArtist.UserId);
        Assert.Equal(UserRole.Artist, user.Role);
    }

    [Fact]
    public async Task CreateArtist_UserNotFound_ReturnsNotFound()
    {
        SetAdminUser();

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

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found", ((dynamic)notFound.Value).message);
    }

    [Fact]
    public async Task CreateArtist_NotAdmin_ReturnsUnauthorized()
    {
        SetNonAdminUser();

        var dto = new ArtistCreateDto
        {
            Name = "New Artist",
            UserId = "123"
        };

        var result = await _controller.CreateArtist(dto);

        Assert.IsType<ForbidResult>(result); // Fails policy
    }

    // ----------------------------------------------------------
    // PUT /api/artists/{id}
    // ----------------------------------------------------------
    [Fact]
    public async Task UpdateArtist_AdminUser_UpdatesArtist()
    {
        SetAdminUser();

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
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New Bio", updated.Bio);
        Assert.Equal("new@test.com", updated.Email);
    }

    [Fact]
    public async Task UpdateArtist_NotFound_ReturnsNotFound()
    {
        SetAdminUser();

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

    [Fact]
    public async Task UpdateArtist_NotAdmin_ReturnsForbid()
    {
        SetNonAdminUser();

        var dto = new ArtistUpdateDto
        {
            Name = "X",
            Email = "x@x.com",
            Bio = "Bio",
            ImageUrl = "img.jpg"
        };

        var result = await _controller.UpdateArtistAsync(Guid.NewGuid(), dto);

        Assert.IsType<ForbidResult>(result);
    }
}
