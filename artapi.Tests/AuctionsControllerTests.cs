﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using artapi.Controllers;
using artapi.Data;
using artapi.DTOs;
using artapi.Models;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace artapi.Tests;

public class AuctionsControllerTests
{
    private readonly AppDbContext _context;
    private readonly AuctionsController _controller;
    private readonly Guid _artistId = Guid.NewGuid();

    public AuctionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var testUser = new User
        {
            Id = _artistId.ToString(),
            UserName = "artistId@example.com",
            Email = "artistId@example.com",
            Role = UserRole.Artist
        };
        _context.Users.Add(testUser);

        var unsoldAuction = new Auction(
            title: "Auction 1",
            artistId: _artistId,
            artistName: "Test Artist 1",
            imageUrl: "img1.jpg",
            limit: 100m,
            medium: "Oil",
            dimensions: "50x50",
            artistBio: "Bio");

        var soldAuction = new Auction(
            title: "Auction 2",
            artistId: _artistId,
            artistName: "Test Artist 2",
            imageUrl: "img2.jpg",
            limit: 200m,
            medium: "Acrylic",
            dimensions: "60x60",
            artistBio: "Bio");

        soldAuction.PlaceBid(250m, out _);

        _context.Auctions.AddRange(unsoldAuction, soldAuction);
        _context.SaveChanges();

        _controller = new AuctionsController(_context);

        // Mock authorized user (Artist)
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new(ClaimTypes.NameIdentifier, _artistId.ToString()),
            new(ClaimTypes.Role, "Artist")
        ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAuctions_Auctions_ReturnsAllAuctions()
    {
        var result = await _controller.GetAuctionsAsync();
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var auctions = Assert.IsType<IEnumerable<AuctionReadDto>>(okResult.Value, exactMatch: false);

        Assert.Equal(2, auctions.Count());
        Assert.Equal("Auction 1", auctions.First().Title);
    }

    [Fact]
    public async Task GetAuctionById_ValidId_ReturnsAuction()
    {
        var auction = _context.Auctions.First();
        var result = await _controller.GetAuctionByIdAsync(auction.Id);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<AuctionReadDto>(okResult.Value);
        Assert.Equal(auction.Title, dto.Title);
    }

    [Fact]
    public async Task GetAuctionById_InvalidId_ReturnsNotFound()
    {
        var result = await _controller.GetAuctionByIdAsync(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAuction_ValidDto_ReturnsCreatedAuction()
    {
        var dto = new AuctionWriteDto
        {
            Title = "New Auction",
            ImageUrl = "img3.jpg",
            Limit = 150m,
            Medium = "Watercolor",
            Dimensions = "40x40",
        };
        var testArtist = new Artist
        {
            Name = "Test Artist",
            Bio = "Bio",
            ImageUrl = "img1.jpg",
            UserId = _artistId.ToString()
        };
        _context.Artists.Add(testArtist);
        _context.SaveChanges();
        var result = await _controller.CreateAuctionAsync(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var auction = Assert.IsType<AuctionReadDto>(createdResult.Value);

        var createdAuction = await _context.Auctions.FindAsync(auction.Id);

        Assert.Equal("New Auction", auction.Title);
        Assert.NotNull(createdAuction);
        Assert.Equal("New Auction", createdAuction.Title);
        Assert.Equal(testArtist.Bio, createdAuction.ArtistBio);
    }

    [Fact]
    public async Task UpdateAuction_OwnerUser_UpdatesAuction()
    {
        var auction = _context.Auctions.First(a => !a.IsSold);
        var dto = new AuctionWriteDto
        {
            Title = "Updated Auction",
            ImageUrl = auction.ImageUrl,
            Limit = auction.Limit,
            Medium = auction.Medium,
            Dimensions = auction.Dimensions
        };

        var result = await _controller.UpdateAuctionAsync(auction.Id, dto);

        Assert.IsType<OkResult>(result);

        var updated = await _context.Auctions.FindAsync(auction.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Auction", updated.Title);
    }

    [Fact]
    public async Task PlaceBid_BidExceedsLimit_CreatesTransactionAndMarksSold()
    {
        var auction = _context.Auctions.First(a => !a.IsSold);

        var result = await _controller.PlaceBidAsync(auction.Id, 200m);

        Assert.IsType<OkObjectResult>(result);

        var updatedAuction = await _context.Auctions.FindAsync(auction.Id);
        Assert.NotNull(updatedAuction);
        Assert.True(updatedAuction.IsSold);

        var transaction = _context.Transactions.FirstOrDefault(t => t.AuctionId == auction.Id);
        Assert.NotNull(transaction);
        Assert.Equal(auction.Id, transaction.AuctionId);
        Assert.Equal(200m, transaction.AmountPaid);

        var userId = _controller.ControllerContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _context.Users
            .Include(u => u.Transactions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        Assert.NotNull(user);
        Assert.Contains(user.Transactions, t => t.AuctionId == auction.Id);
    }

    [Fact]
    public async Task UpdateAuction_NotOwnerUser_ReturnsForbid()
    {
        // Arrange: create another artistId ID
        var otherArtistId = Guid.NewGuid();
        var auction = _context.Auctions.First(a => !a.IsSold);

        // Mock a different user (not the auction owner)
        var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
        new Claim(ClaimTypes.NameIdentifier, otherArtistId.ToString()),
        new Claim(ClaimTypes.Role, "Artist")
        ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsUser }
        };

        var dto = new AuctionWriteDto
        {
            Title = "Hacker Update",
            ImageUrl = auction.ImageUrl,
            Limit = auction.Limit,
            Medium = auction.Medium,
            Dimensions = auction.Dimensions
        };

        // Act
        var result = await _controller.UpdateAuctionAsync(auction.Id, dto);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PlaceBid_BidBelowHighest_ReturnsBadRequest()
    {
        var auction = _context.Auctions.First(a => !a.IsSold);

        auction.PlaceBid(100m, out _);

        var bidUserId = Guid.NewGuid().ToString();
        var claimsUser = new ClaimsPrincipal(new ClaimsIdentity(
        [
        new Claim(ClaimTypes.NameIdentifier, bidUserId),
        new Claim(ClaimTypes.Role, "User")
        ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsUser }
        };

        var result = await _controller.PlaceBidAsync(auction.Id, 50m);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Bid not accepted. Either too low or auction is sold.", badRequest.Value);
    }
}