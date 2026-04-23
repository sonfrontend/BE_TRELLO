using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Entities.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BE_ECOMMERCE.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolePermissionController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllRolePermissions()
    {
        var rolePermissions = await _context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .Select(rp => new {
                rp.RolePermissionId,
                rp.RoleId,
                rp.PermissionId,
                RoleName = rp.Role != null ? rp.Role.RoleName : null,
                PermissionName = rp.Permission != null ? rp.Permission.PermissionName : null
            })
            .ToListAsync();
        return Ok(rolePermissions);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRolePermission([FromBody] RolePermission request)
    {
        var exists = await _context.RolePermissions.AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId);
        if (exists)
            return BadRequest(new { message = "Dữ liệu này đã tồn tại!" });

        request.RolePermissionId = Guid.NewGuid();
        _context.RolePermissions.Add(request);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Thêm dữ liệu thành công!", rolePermission = request });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRolePermission(Guid id)
    {
        var rolePermission = await _context.RolePermissions.FindAsync(id);
        if (rolePermission == null)
            return NotFound(new { message = "Không tìm thấy dữ liệu!" });

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Xóa dữ liệu thành công!" });
    }
}
