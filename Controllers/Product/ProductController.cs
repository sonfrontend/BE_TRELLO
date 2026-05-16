using BE_ECOMMERCE.Data;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {

        try
        {
            // 1. Kéo dữ liệu từ DB lên RAM
            var rawProducts = await _context.Products.AsNoTracking().ToListAsync();

            // 2. Gom nhóm theo ProductCode (Ví dụ: 0108775)
            var groupedData = rawProducts
                .GroupBy(p => p.ProductCode)
                .Select(g =>
                {
                    // Chọn sản phẩm đầu tiên trong nhóm làm "Sản phẩm đại diện"
                    var defaultProduct = g.FirstOrDefault();

                    // Lọc ra các sản phẩm CÒN LẠI (bỏ qua cái đại diện) để đưa vào mảng con
                    var otherVariants = g.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

                    return new
                    {
                        // --- THÔNG TIN CỦA SẢN PHẨM ĐẦU TIÊN ---
                        ArticleId = defaultProduct.ArticleId, // Ví dụ: 0108775015-L
                        ProductCode = defaultProduct.ProductCode,
                        CategoryId = defaultProduct.CategoryId,
                        ProductName = defaultProduct.ProductName,
                        Price = defaultProduct.Price,
                        ImageUrl = defaultProduct.ImageUrl,
                        Description = defaultProduct.Description,
                        Size = defaultProduct.Size,     // Hiện rõ Size của cái đại diện
                        Color = defaultProduct.Color,   // Hiện rõ Màu của cái đại diện
                        StockQuantity = defaultProduct.StockQuantity,

                        // --- MẢNG CHỨA CÁC SẢN PHẨM LIÊN QUAN CÒN LẠI ---
                        Products = otherVariants.Select(v => new
                        {
                            ArticleId = v.ArticleId,
                            Size = v.Size,
                            Color = v.Color,
                            StockQuantity = v.StockQuantity,
                            Price = v.Price,
                            ImageUrl = v.ImageUrl
                        }).OrderBy(v => v.Size).ToList()
                    };
                })
                .OrderByDescending(p => p.ArticleId) // Cũ nhất hoặc mới nhất tùy ý
                .ToList();

            // 3. Xử lý phân trang
            var totalCount = groupedData.Count;
            var pagedProducts = groupedData
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var hasMore = (page * pageSize) < totalCount;

            return Ok(new
            {
                data = pagedProducts,
                hasMore = hasMore,
                nextPage = hasMore ? page + 1 : (int?)null,
                totalCount = totalCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

    [HttpGet("{productCode}")]
    public async Task<IActionResult> GetProductByCode(string productCode)
    {
        try
        {
            var variants = await _context.Products
                .Include(p => p.Categories)
                    .ThenInclude(c => c.ParentCategory)
                .Where(p => p.ProductCode == productCode)
                .AsNoTracking()
                .ToListAsync();

            if (!variants.Any())
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            var defaultProduct = variants.FirstOrDefault();

            var otherVariants = variants.Where(p => p.ArticleId != defaultProduct.ArticleId).ToList();

            var result = new
            {
                // --- THÔNG TIN CỦA SẢN PHẨM ĐẦU TIÊN ---
                ArticleId = defaultProduct.ArticleId, // Ví dụ: 0108775015-L
                ProductCode = defaultProduct.ProductCode,
                CategoryId = defaultProduct.CategoryId,
                CategoryName = defaultProduct.Categories?.Name ?? "Danh mục chung",
                ParentCategoryName = defaultProduct.Categories?.ParentCategory?.Name,
                ProductName = defaultProduct.ProductName,
                Price = defaultProduct.Price,
                ImageUrl = defaultProduct.ImageUrl,
                Description = defaultProduct.Description,
                Size = defaultProduct.Size,     // Hiện rõ Size của cái đại diện
                Color = defaultProduct.Color,   // Hiện rõ Màu của cái đại diện
                StockQuantity = defaultProduct.StockQuantity,

                // --- MẢNG CHỨA CÁC SẢN PHẨM LIÊN QUAN CÒN LẠI ---
                Products = otherVariants.Select(v => new
                {
                    ArticleId = v.ArticleId,
                    Size = v.Size,
                    Color = v.Color,
                    StockQuantity = v.StockQuantity,
                    Price = v.Price,
                    ImageUrl = v.ImageUrl
                }).OrderBy(v => v.Size).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
        }
    }

}
