using System;
using BE_ECOMMERCE.Entities.Product;

namespace BE_ECOMMERCE.Entities.Order;

public class OrderItem : BaseEntity
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public virtual Order Order { get; set; }

    public string ArticleId { get; set; }
    public virtual BE_ECOMMERCE.Entities.Product.Product Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
