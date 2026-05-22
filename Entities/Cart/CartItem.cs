using System;
using BE_ECOMMERCE.Entities.Auth;
using BE_ECOMMERCE.Entities.Product;

namespace BE_ECOMMERCE.Entities.Cart;

public class CartItem : BaseEntity
{
    public int Id { get; set; }
    
    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public string ArticleId { get; set; }
    public virtual BE_ECOMMERCE.Entities.Product.Product Product { get; set; }

    public int Quantity { get; set; }
}
