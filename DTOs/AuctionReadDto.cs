namespace artapi.DTOs;

public class AuctionReadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal HighestBid { get; set; }
    public int BidCount { get; set; }
    public string Medium { get; set; } = string.Empty;
    public string Dimensions { get; set; } = string.Empty;
    public string ArtistBio { get; set; } = string.Empty;
    public bool IsSold { get; set; }
}
