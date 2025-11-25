namespace artapi.DTOs;

public class AuctionWriteDto
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public string Medium { get; set; } = string.Empty;
    public string Dimensions { get; set; } = string.Empty;
}
