using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using artapi.Data;
using artapi.Mappers;
using artapi.Models;

namespace artapi.Controllers;

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
            UserName = dto.UserName,
            Email = dto.Email,
            Role = UserRole.User,
        };

        if (await userManager.FindByEmailAsync(dto.Email) != null)
            return BadRequest(new { message = "Email is already registered." });

        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await userManager.AddToRoleAsync(user, UserRole.User.ToString());

        var token = GenerateJwtToken(user);
        var userDto = GenerateUserDto(user);

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

        var token = GenerateJwtToken(user);
        var userDto = GenerateUserDto(user);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = userDto
        });
    }

    private JwtSecurityToken GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds,
            claims: claims
        );

        return token;
    }

    private static object GenerateUserDto(User user)
    {
        var userDto = new
        {
            user.Id,
            user.UserName,
            user.Email,
            Role = user.Role.ToString(),
        };

        return userDto;
    }
}

public record RegisterDto(string UserName, string Email, string Password);
public record LoginDto(string Email, string Password);
