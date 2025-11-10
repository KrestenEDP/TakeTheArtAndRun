using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TakeTheArtAndRunAPI.Data;
using TakeTheArtAndRunAPI.Models;

namespace TakeTheArtAndRunAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<User> userManager,
    //RoleManager<IdentityRole> roleManager,
    IConfiguration config,
    AppDbContext context) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            Role = dto.Role
        };

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, dto.Role.ToString());

        var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
        new Claim("role", user.Role.ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds,
            claims: claims
        );

        var userDto = new
        {
            user.Id,
            user.Email,
            Role = user.Role.ToString(),
            Transactions = new List<object>()
        };

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = userDto
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await context.Users
            .Include(u => u.Transactions)
                .ThenInclude(t => t.Auction)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid credentials." });

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email!),
            new Claim("role", user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds,
            claims: claims
        );

        var userDto = new
        {
            user.Id,
            user.Email,
            Role = user.Role.ToString(),
            Transactions = user.Transactions.Select(t => new
            {
                t.Id,
                t.AmountPaid,
                t.PurchasedAt,
                Auction = t.Auction is not null
                    ? new
                    {
                        t.Auction.Id,
                        t.Auction.Title,
                        t.Auction.ImageUrl,
                        t.Auction.HighestBid,
                        t.Auction.Medium,
                        t.Auction.Dimensions
                    }
                    : null
            })
        };

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = userDto
        });
    }
}

public record RegisterDto(string Email, string Password, UserRole Role=UserRole.User);
public record LoginDto(string Email, string Password);
