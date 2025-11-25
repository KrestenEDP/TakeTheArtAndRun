using artapi.DTOs;
using artapi.Models;

namespace artapi.Mappers;

public static class TransactionMapper
{
    public static TransactionReadDto ToDto(this Transaction transaction)
    {
        return new TransactionReadDto
        {
            Amount = transaction.AmountPaid,
            Auction = AuctionMapper.ToDto(transaction.Auction!),
            Date = transaction.PurchasedAt
        };
    }
}
