using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PermissionController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _context.Permissions.ToListAsync();
        return Ok(permissions);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] Permission request)
    {
        request.PermissionId = Guid.NewGuid();
        _context.Permissions.Add(request);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Thêm dữ liệu thành công!", permission = request });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] Permission request)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });

        permission.PermissionName = request.PermissionName;
        permission.Description = request.Description;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật dữ liệu thành công!", permission });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });

        _context.Permissions.Remove(permission);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Xóa dữ liệu thành công!" });
    }
}
