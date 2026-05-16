namespace BE_ECOMMERCE.Entities.Transaction;

public class Transaction : BaseEntity
{
    public int Id { get; set; }
    public DateTime TDat { get; set; }
    public string CustomerId { get; set; }
    public string ArticleId { get; set; }
    public double Price { get; set; }
    public int SalesChannelId { get; set; }
}