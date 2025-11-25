namespace artapi.DTOs;

public class TransactionReadDto
{
    public decimal Amount { get; set; }
    public AuctionReadDto Auction { get; set; } = new AuctionReadDto();
    public DateTime Date { get; set; }
}
