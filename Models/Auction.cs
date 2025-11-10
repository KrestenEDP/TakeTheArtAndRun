using System.Security.Cryptography;

namespace TakeTheArtAndRunAPI.Models;

public class Auction
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ArtistId { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal HighestBid { get; private set; } = 0;
    public int BidCount { get; private set; } = 0;
    public string Medium { get; set; } = string.Empty;
    public string Dimensions { get; set; } = string.Empty;
    public string ArtistBio { get; set; } = string.Empty;
    public Boolean IsSold { get; private set; } = false;

    private Auction() { }

    public Auction(string title, Guid artistId, string imageUrl, decimal limit, string medium, string dimensions, string artistBio)
    {
        Title = title;
        ArtistId = artistId;
        ImageUrl = imageUrl;
        Limit = decimal.Round(limit, 2);
        Medium = medium;
        Dimensions = dimensions;
        ArtistBio = artistBio;
    }

    public Boolean PlaceBid(decimal bidAmount, out bool sold)
    {
        bidAmount = decimal.Round(bidAmount, 2);
        sold = false;

        if (IsSold || bidAmount <= HighestBid)
            return false;

        HighestBid = bidAmount;
        BidCount += 1;

        if (bidAmount >= Limit)
        {
            IsSold = true;
            sold = true;
        }
        return true;
    }
}
