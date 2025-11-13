using artapi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using artapi.Data;
using artapi.DTOs;
using artapi.Mappers;
using artapi.Models;

namespace artapi.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ArtistsController(AppDbContext context, UserManager<User> userManager) : ControllerBase
{
    // GET /api/artists
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artist>>> GetArtistsAsync()
    {
        var artists = await context.Artists.ToListAsync();

        var dtos = artists.Select(a => a.ToDto()).ToList();
        return Ok(dtos);
    }

    // POST /api/artists
    [HttpPost]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> CreateArtist([FromBody] ArtistCreateDto dto)
    {
        var user = await userManager.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "User not found" });

        user.Role = UserRole.Artist;
        await userManager.AddToRoleAsync(user, "Artist");

        var artist = new Artist
        {
            Name = dto.Name,
            Bio = dto.Bio,
            ImageUrl = dto.ImageUrl,
            Email = dto.Email,
            UserId = dto.UserId
        };

        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        return Ok(new { message = "Artist created and role assigned", artistId = artist.Id });
    }

    // PUT /api/artists/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Admin)]
    public async Task<IActionResult> UpdateArtistAsync(Guid id, [FromBody] ArtistUpdateDto updatedArtist)
    {
        var artistId = await context.Artists.FindAsync(id);
        if (artistId == null)
        {
            return NotFound();
        }
        artistId.Name = updatedArtist.Name;
        artistId.Bio = updatedArtist.Bio;
        artistId.ImageUrl = updatedArtist.ImageUrl;
        artistId.Email = updatedArtist.Email;
        context.Artists.Update(artistId);
        await context.SaveChangesAsync();
        return Ok();
    }
}