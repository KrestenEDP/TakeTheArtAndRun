using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using artapi.Data;
using artapi.DTOs;
using artapi.Mappers;

namespace artapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    // GET /api/transactions
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TransactionReadDto>>> GetTransactionsAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Transactions)
            .ThenInclude(t => t.Auction)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return Unauthorized();

        var dtos = user.Transactions.Select(t => t.ToDto()).ToList();
        return Ok(dtos);
    }
}
