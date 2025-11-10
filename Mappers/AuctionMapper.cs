namespace TakeTheArtAndRunAPI.Mappers;

using TakeTheArtAndRunAPI.DTOs;
using TakeTheArtAndRunAPI.Models;

public static class AuctionMapper
{
    public static AuctionReadDto ToDto(this Auction auction)
    {
        return new AuctionReadDto
        {
            Id = auction.Id,
            ArtistId = auction.ArtistId,
            Title = auction.Title,
            ImageUrl = auction.ImageUrl,
            HighestBid = auction.HighestBid,
            BidCount = auction.BidCount,
            Medium = auction.Medium,
            Dimensions = auction.Dimensions,
            ArtistBio = auction.ArtistBio
        };
    }
}

