using Microsoft.EntityFrameworkCore;
using BE_ECOMMERCE.Constants; // Gọi mảng Hằng số của bạn vào
using BE_ECOMMERCE.Entities.Auth; // Gọi class Permission vào

namespace BE_ECOMMERCE.Data
{
    public static class DbSeeder
    {
        // Hàm này sẽ được gọi tự động mỗi khi Server bật lên
        public static async Task AutoSyncPermissions(IServiceProvider serviceProvider)
        {
            // Mở một luồng kết nối an toàn tới Database
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Kéo tất cả tên Quyền đang có sẵn trong SQL Server lên
            var existingPermissions = await context.Permissions
                .Select(p => p.PermissionName)
                .ToListAsync();

            bool isChanged = false; // Cờ đánh dấu xem có cần lưu DB không

            // 2. Vòng lặp duyệt qua mảng tĩnh trong code của bạn
            foreach (var permName in AppPermissions.All)
            {
                // Nếu Quyền trong code CHƯA TỒN TẠI trong Database -> Tạo mới
                if (!existingPermissions.Contains(permName))
                {
                    var newPermission = new Permission
                    {
                        // Lúc này dùng NewGuid() thoải mái vì ta chèn thực tế, không qua Migration
                        PermissionId = Guid.NewGuid(),
                        PermissionName = permName,
                        Description = $"Tự động tạo quyền: {permName}"
                    };

                    context.Permissions.Add(newPermission);
                    isChanged = true; // Bật cờ báo hiệu có thay đổi
                }
            }

            // 3. Nếu có quyền mới được thêm, lệnh SaveChanges mới được chạy
            if (isChanged)
            {
                await context.SaveChangesAsync();
                Console.WriteLine("Đã đồng bộ quyền mới vào Database thành công!");
            }
        }
    }
}