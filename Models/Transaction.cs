namespace artapi.Models;

public class Transaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserId { get; private set; } = string.Empty;
    public Guid AuctionId { get; private set; }

    public Auction? Auction { get; private set; }
    public User? User { get; private set; }

    public decimal AmountPaid { get; private set; }
    public DateTime PurchasedAt { get; private set; } = DateTime.UtcNow;

    private Transaction() { }

    public Transaction(string userId, Guid auctionId, decimal amount)
    {
        UserId = userId;
        AuctionId = auctionId;
        AmountPaid = decimal.Round(amount, 2);
        PurchasedAt = DateTime.UtcNow;
    }
}

