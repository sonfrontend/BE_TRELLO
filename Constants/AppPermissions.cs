namespace BE_ECOMMERCE.Constants
{
    // static class: Không cần khởi tạo (new) vẫn gọi được
    public static class AppPermissions
    {
        // Nhóm User
        public const string ViewUser = "View_User";
        public const string CreateUser = "Create_User";
        public const string DeleteUser = "Delete_User";

        // Nhóm Role
        public const string ViewRole = "View_Role";
        public const string CreateRole = "Create_Role";

        // Nhóm Product (Sau này làm thì thêm vào đây)
        public const string ViewProduct = "View_Product";


        // Thêm một mảng tĩnh chứa tất cả các quyền ở trên
        public static readonly string[] All = new[]
        {
            ViewUser,
            CreateUser,
            ViewRole,
            DeleteUser,
            CreateRole,
            ViewProduct
        };
    }
}
