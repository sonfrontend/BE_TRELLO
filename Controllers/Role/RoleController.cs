using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Entities.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoleController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _context.Roles.ToListAsync();
        return Ok(new { status = 200, message = "Lấy dữ liệu thành công!", data = roles });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] Role request)
    {
        request.RoleId = Guid.NewGuid();
        _context.Roles.Add(request);
        await _context.SaveChangesAsync();
        return Ok(new { status = 200, message = "Thêm dữ liệu thành công!", role = request });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] Role request)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });

        role.RoleName = request.RoleName;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật dữ liệu thành công!", role });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Xóa dữ liệu thành công!" });
    }
}
