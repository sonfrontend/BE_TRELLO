using System.Security.Claims;
using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Entities.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BE_ECOMMERCE.Constants; // Gọi hằng số vào để dùng

namespace BE_ECOMMERCE.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class OrderController(ApplicationDbContext context, IServiceScopeFactory scopeFactory) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public class CreateOrderRequest
    {
        public List<int> SelectedCartItemIds { get; set; } = new();
        public string RecipientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = PaymentMethodConstant.COD; // "COD", "PayPal"
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var parsedUserId = Guid.Parse(userId);

        if (request.SelectedCartItemIds == null || request.SelectedCartItemIds.Count == 0)
            return BadRequest("Vui lòng chọn ít nhất 1 sản phẩm để thanh toán.");

        // 1. Lấy sản phẩm từ giỏ hàng và tạm thời trừ kho để giữ chỗ
        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == parsedUserId && request.SelectedCartItemIds.Contains(c.Id))
            .ToListAsync();

        if (cartItems.Count == 0) return BadRequest("Không tìm thấy sản phẩm hợp lệ trong giỏ hàng.");

        foreach (var item in cartItems)
        {
            if (item.Product.StockQuantity < item.Quantity)
            {
                return BadRequest($"Sản phẩm '{item.Product.ProductName}' không đủ số lượng trong kho.");
            }
            item.Product.StockQuantity -= item.Quantity; // Trừ kho tạm thời
        }

        decimal totalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);

        // 1. Chuẩn hóa chữ In Hoa để so sánh cho an toàn (VD: Paypal, payPal, PAYPAL đều hiểu hết)
        string pMethod = request.PaymentMethod?.ToUpper() ?? "COD";

        // 2. Logic siêu gọn: CHỈ CÓ PAYPAL mới vào trạng thái Chờ thanh toán (để bật đếm ngược 2 phút).
        // Còn lại (COD) thì vào trạng thái Chờ xác nhận luôn.
        string initialStatus = (pMethod == "PAYPAL")
            ? OrderStatusConstant.PendingPayment
            : OrderStatusConstant.Pending;


        var orderDate = DateTime.Now;

        var order = new BE_ECOMMERCE.Entities.Order.Order
        {
            Status = initialStatus,
            UserId = parsedUserId,
            RecipientName = request.RecipientName,
            PhoneNumber = request.PhoneNumber,
            ShippingAddress = request.ShippingAddress,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod ?? PaymentMethodConstant.COD,
            OrderDate = orderDate
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(); // Lưu để lấy OrderId

        var orderItems = cartItems.Select(c => new OrderItem
        {
            OrderId = order.Id,
            ArticleId = c.ArticleId,
            Quantity = c.Quantity,
            UnitPrice = c.Product.Price
        }).ToList();

        _context.OrderItems.AddRange(orderItems);
        _context.CartItems.RemoveRange(cartItems); // Dọn sạch các món đã chọn khỏi giỏ hàng
        await _context.SaveChangesAsync();

        // -------------------------------------------------------------------------
        // LUỒNG CHẠY NGẦM: CHỜ TRẢ RESPONSE XONX MỚI BẮT ĐẦU ĐẾM NGƯỢC 2 PHÚT
        // -------------------------------------------------------------------------
        if (initialStatus == OrderStatusConstant.PendingPayment)
        {
            int currentOrderId = order.Id; // Lưu mã đơn hàng để dùng trong luồng ngầm

            // Lệnh này CHỈ CHẠY SAU KHI API đã trả kết quả thành công cho ReactJS
            Response.OnCompleted(() =>
            {
                _ = Task.Run(async () =>
                {
                    // Luồng ngầm đi ngủ đúng 2 phút
                    await Task.Delay(TimeSpan.FromMinutes(2));

                    try
                    {
                        // Tự tạo một Scope mới để truy cập Database từ luồng ngầm một cách an toàn
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            var checkOrder = await db.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefaultAsync(o => o.Id == currentOrderId);

                            // Nếu sau 2 phút khách vẫn chưa thanh toán (Status vẫn là PendingPayment)
                            if (checkOrder != null && checkOrder.Status == OrderStatusConstant.PendingPayment)
                            {
                                // 1. Hoàn trả số lượng sản phẩm về lại kho gốc
                                foreach (var detail in checkOrder.OrderItems)
                                {
                                    var productVariant = await db.Products
                                        .FirstOrDefaultAsync(p => p.ArticleId == detail.ArticleId);
                                    if (productVariant != null)
                                    {
                                        productVariant.StockQuantity += detail.Quantity;
                                    }
                                }

                                // 2. CHUYỂN TRẠNG THÁI THÀNH HỦY (Không xóa đơn hàng khỏi DB)
                                checkOrder.Status = OrderStatusConstant.Cancelled;
                                await db.SaveChangesAsync();

                                Console.WriteLine($"Đã tự động chuyển trạng thái đơn hàng {currentOrderId} thành 'Cancelled' do hết hạn.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi chạy hủy đơn ngầm {currentOrderId}: {ex.Message}");
                    }
                });

                return Task.CompletedTask;
            });
        }

        // -------------------------------------------------------------------------
        // TRẢ KẾT QUẢ NGAY LẬP TỨC CHO FRONTEND (MẤT CHƯA TỚI 0.1s)
        // -------------------------------------------------------------------------
        return Ok(new
        {
            message = "Đặt hàng thành công",
            orderId = order.Id,
            orderDate = orderDate,
            totalAmount = order.TotalAmount
        });
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetUserOrders()
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == Guid.Parse(userId))
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                o.Status,
                o.TotalAmount,
                o.RecipientName,
                o.PhoneNumber,
                o.ShippingAddress,
                o.PaymentMethod,
                OrderItems = o.OrderItems.Select(oi => new
                {
                    oi.ArticleId,
                    oi.Product.ProductName,
                    oi.Product.ImageUrl,
                    oi.Product.Color,
                    oi.Product.Size,
                    oi.Quantity,
                    oi.UnitPrice
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("admin")]
    [Authorize]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                o.Status,
                o.TotalAmount,
                o.RecipientName,
                o.PhoneNumber,
                o.ShippingAddress,
                o.PaymentMethod,
                o.UserId,
                OrderItems = o.OrderItems.Select(oi => new
                {
                    oi.ArticleId,
                    oi.Product.ProductName,
                    oi.Product.ImageUrl,
                    oi.Product.Color,
                    oi.Product.Size,
                    oi.Quantity,
                    oi.UnitPrice
                })
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPut("admin/{id}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound("Không tìm thấy đơn hàng");

        order.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Cập nhật trạng thái thành công" });
    }

    [HttpGet("{id}/status")]
    [Authorize]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound("Không tìm thấy đơn hàng");

        return Ok(new { status = order.Status });
    }
}