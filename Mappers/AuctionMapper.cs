namespace artapi.Mappers;

using artapi.DTOs;
using artapi.Models;

public static class AuctionMapper
{
    public static AuctionReadDto ToDto(this Auction auction)
    {
        return new AuctionReadDto
        {
            Id = auction.Id,
            ArtistName = auction.ArtistName,
            Title = auction.Title,
            ImageUrl = auction.ImageUrl,
            HighestBid = auction.HighestBid,
            BidCount = auction.BidCount,
            Medium = auction.Medium,
            Dimensions = auction.Dimensions,
            ArtistBio = auction.ArtistBio,
            IsSold = auction.IsSold
        };
    }
}

