using artapi.Data;
using artapi.DTOs;
using artapi.Mappers;
using artapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace artapi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(UserManager<User> userManager, AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;

    // GET /api/users
    [HttpGet]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsersAsync()
    {
        var users = await _context.Users.ToListAsync();

        var dtos = users.Select(a => a.ToDto()).ToList();
        
        return Ok(dtos);

    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> SearchUsers(string query)
    {
        var users = await _context.Users
            .Where(u => u.UserName.Contains(query) || u.Email.Contains(query))
            .ToListAsync();

        if (users.Count == 0)
        {
            return NotFound(new { message = "No users found matching the query." });
        }

        return Ok(users.Select(x => x.ToDto()));
    }

}
