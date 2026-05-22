namespace BE_ECOMMERCE.Constants
{
    // static class: Không cần khởi tạo (new) vẫn gọi được
    public static class PaymentMethodConstant
    {
        public const string COD = "COD";           // Thanh toán khi nhận hàng
        public const string PayPal = "PayPal";     // Thanh toán qua PayPal

        // Thêm một mảng tĩnh chứa tất cả các phương thức thanh toán ở trên
        public static readonly string[] All = new[]
        {
            COD,
            PayPal
        };
    }
}

