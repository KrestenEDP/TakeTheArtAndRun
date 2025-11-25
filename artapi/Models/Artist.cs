namespace artapi.Models;

public class Artist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = String.Empty;
    public string Bio { get; set; } = String.Empty;
    public string ImageUrl { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public List<Auction> Auctions { get; set; } = [];
}
