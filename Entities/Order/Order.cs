using System;
using System.Collections.Generic;
using BE_ECOMMERCE.Entities.Auth;
using BE_ECOMMERCE.Constants;

namespace BE_ECOMMERCE.Entities.Order;

public class Order : BaseEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    // Status can be: Pending, Approved, Shipping, Delivered, Cancelled
    public string Status { get; set; } = OrderStatusConstant.Pending;

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = PaymentMethodConstant.COD;

    // Thông tin giao hàng
    public string RecipientName { get; set; }
    public string PhoneNumber { get; set; }
    public string ShippingAddress { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; }
}
