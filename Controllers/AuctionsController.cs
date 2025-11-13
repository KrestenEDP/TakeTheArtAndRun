namespace artapi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using artapi.Data;
using artapi.DTOs;
using artapi.Mappers;
using artapi.Models;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    // GET /api/auctions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuctionReadDto>>> GetAuctionsAsync()
    {
        var auctions = await _context.Auctions
            .ToListAsync();

        var dtos = auctions.Select(a => a.ToDto()).ToList();

        return Ok(dtos);
    }

    // GET /api/auctions/{id}
    [HttpGet("{id}", Name = "GetAuctionById")]
    public async Task<ActionResult<AuctionReadDto>> GetAuctionByIdAsync(Guid id)
    {
        var auction = await _context.Auctions
            .Where(a => !a.IsSold)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (auction == null)
            return NotFound();

        return Ok(auction.ToDto());
    }

    // POST /api/auctions
    [HttpPost]
    [Authorize(Policy = Policies.Artist)]
    public async Task<ActionResult<AuctionReadDto>> CreateAuctionAsync([FromBody] AuctionWriteDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId);
        if (artist == null) return Forbid("You must be an artistId to create an auction.");
        var newAuction = new Auction
        (
            title: dto.Title,
            artistId: artist.Id,
            artistName: artist.Name,
            artistBio: artist.Bio,
            imageUrl: dto.ImageUrl,
            limit: dto.Limit,
            medium: dto.Medium,
            dimensions: dto.Dimensions
        );

        _context.Auctions.Add(newAuction);
        await _context.SaveChangesAsync();

        var readDto = newAuction.ToDto();
        return CreatedAtAction("GetAuctionById", new { id = newAuction.Id }, readDto);
    }

    // PUT /api/auctions/{id} - only the artistId who created it
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Artist)]
    public async Task<IActionResult> UpdateAuctionAsync(Guid id, [FromBody] AuctionWriteDto dto)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null)
            return NotFound();

        var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(currentUserIdString, out var currentUserId))
            return Forbid("Invalid user ID.");
        if (auction.ArtistId != currentUserId)
            return Forbid("You can only update your own auctions.");

        auction.Title = dto.Title;
        auction.ImageUrl = dto.ImageUrl;
        auction.Limit = dto.Limit;
        auction.Medium = dto.Medium;
        auction.Dimensions = dto.Dimensions;

        await _context.SaveChangesAsync();
        return Ok();
    }

    // POST /api/auctions/{id}/bid - any logged-in user
    [HttpPost("{id}/bid")]
    [Authorize]
    public async Task<IActionResult> PlaceBidAsync(Guid id, [FromBody] decimal bidAmount)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Forbid("Invalid user ID.");

        var success = auction.PlaceBid(bidAmount, out bool sold);
        if (!success) return BadRequest("Bid not accepted. Either too low or auction is sold.");

        if (sold)
        {
            var transaction = new Transaction(userId, auction.Id, auction.HighestBid);
            _context.Transactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
        return Ok(sold);
    }
}

