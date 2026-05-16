using BE_ECOMMERCE.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers.Category;

[Route("api/[controller]")]
[ApiController]
public class CategoryController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        // Lấy tất cả danh mục, kèm theo danh mục con (nếu có)
        var categories = await _context.Categories
            .Where(c => c.ParentId == null) // Bắt đầu từ danh mục cha cấp 1
            .Include(c => c.SubCategories)
            .ToListAsync();

        return Ok(categories);
    }
}
