using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using artapi.Controllers;
using artapi.Data;
using artapi.DTOs;
using artapi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace artapi.Tests;

public class TransactionsControllerTests
{
    private readonly AppDbContext _context;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        _controller = new TransactionsController(_context);
    }

    private void SetUser(string userId)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void ClearUser()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // ---------------------------------------------------------
    // GET /api/transactions  (Authorized user)
    // ---------------------------------------------------------

    [Fact]
    public async Task GetTransactionsAsync_ReturnsUserTransactions()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        var user = new User { Id = userId, Email = "user@test.com" };
        _context.Users.Add(user);

        var auction = new Auction(
            "Test Auction", Guid.NewGuid(), "Artist", "img.jpg",
            100, "Oil", "20x20", "Bio");

        _context.Auctions.Add(auction);

        var tx = new Transaction
        (
            userId,
            auction.Id,
            150m
        );

        user.Transactions.Add(tx);

        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetTransactionsAsync();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<IEnumerable<TransactionReadDto>>(ok.Value, exactMatch: false);

        Assert.Single(dtos);
        Assert.Equal(tx.AmountPaid, dtos.First().Amount);
        Assert.Equal(auction.Title, dtos.First().Auction.Title);
    }

    [Fact]
    public async Task GetTransactionsAsync_NoTransactions_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid().ToString();
        SetUser(userId);

        _context.Users.Add(new User { Id = userId, Email = "empty@test.com" });
        await _context.SaveChangesAsync();

        var result = await _controller.GetTransactionsAsync();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<IEnumerable<TransactionReadDto>>(ok.Value, exactMatch: false);

        Assert.Empty(dtos);
    }

    [Fact]
    public async Task GetTransactionsAsync_NoUserClaim_ReturnsUnauthorized()
    {
        ClearUser();

        var result = await _controller.GetTransactionsAsync();
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetTransactionsAsync_UserNotFound_ReturnsUnauthorized()
    {
        SetUser("missing-user-id");

        var result = await _controller.GetTransactionsAsync();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}