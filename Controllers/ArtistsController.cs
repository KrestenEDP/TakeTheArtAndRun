namespace TakeTheArtAndRunAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TakeTheArtAndRunAPI.Data;
using TakeTheArtAndRunAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class ArtistsController(AppDbContext context) : ControllerBase
{
    // GET /api/artists
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Artist>>> GetArtistsAsync()
    {
        var artists = await context.Artists.ToListAsync();
        return Ok(artists);
    }
}