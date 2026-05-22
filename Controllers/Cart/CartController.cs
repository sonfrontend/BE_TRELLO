using System.Security.Claims;
using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Entities.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers.Cart;

[Route("api/[controller]")]
[ApiController]
public class CartController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    public class AddToCartRequest
    {
        public string ArticleId { get; set; }
        public int Quantity { get; set; }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCartItems()
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == Guid.Parse(userId))
            .Select(c => new
            {
                c.Id,
                c.Quantity,
                Product = new
                {
                    c.Product.ArticleId,
                    c.Product.ProductCode,
                    c.Product.ProductName,
                    c.Product.Price,
                    c.Product.ImageUrl,
                    c.Product.Color,
                    c.Product.Size
                }
            })
            .ToListAsync();

        return Ok(cartItems);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var parsedUserId = Guid.Parse(userId);

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == parsedUserId && c.ArticleId == request.ArticleId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                UserId = parsedUserId,
                ArticleId = request.ArticleId,
                Quantity = request.Quantity
            };
            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm vào giỏ hàng" });
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateCartQuantity(int id, [FromBody] int quantity)
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == Guid.Parse(userId));
        if (cartItem == null) return NotFound("Không tìm thấy sản phẩm trong giỏ hàng");

        if (quantity <= 0)
        {
            _context.CartItems.Remove(cartItem);
        }
        else
        {
            cartItem.Quantity = quantity;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật số lượng" });
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> RemoveFromCart(int id)
    {
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == Guid.Parse(userId));
        if (cartItem == null) return NotFound("Không tìm thấy sản phẩm trong giỏ hàng");

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng" });
    }
}
