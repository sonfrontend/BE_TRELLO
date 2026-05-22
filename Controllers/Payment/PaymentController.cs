using BE_ECOMMERCE.Data;
using BE_ECOMMERCE.Constants; // Gọi mảng Hằng số của bạn vào
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_ECOMMERCE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        // Dữ liệu từ ReactJS gửi lên
        public class PayPalConfirmRequest
        {
            public int InternalOrderId { get; set; } // Mã đơn hàng trong Database của mình
            public string PayPalOrderId { get; set; } = string.Empty; // Mã giao dịch của PayPal
        }

        [HttpPost("paypal-confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayPalPayment([FromBody] PayPalConfirmRequest request)
        {
            // 1. Tìm đơn hàng trong Database
            var order = await _context.Orders.FindAsync(request.InternalOrderId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // 2. Kiểm tra xem đơn hàng có đúng là đang chờ thanh toán không
            if (order.Status == OrderStatusConstant.PendingPayment)
            {
                // ========================================================================
                // LƯU Ý BẢO VỆ ĐỒ ÁN: 
                // Ở các hệ thống lớn, tại dòng này code sẽ dùng HttpClient gọi sang Server PayPal 
                // để check lại cái "request.PayPalOrderId" xem tiền đã vào ví thật chưa.
                // Ở mức độ đồ án, ta tin tưởng ReactJS đã capture thành công và tiến hành chốt đơn.
                // ========================================================================

                // 3. Đổi trạng thái thành Đã thanh toán (Hoặc Chờ chuẩn bị hàng)
                // Bạn có thể thay bằng Hằng số của bạn, ví dụ: OrderStatusConstant.Paid
                order.Status = "Paid";

                // Tùy chọn: Nếu bảng Order của bạn có cột TransactionId, hãy lưu request.PayPalOrderId vào đó để đối soát
                // order.TransactionId = request.PayPalOrderId;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Thanh toán thành công. Đã cập nhật trạng thái đơn hàng!",
                    orderId = order.Id
                });
            }

            return BadRequest("Đơn hàng này đã được xử lý hoặc không ở trạng thái chờ thanh toán.");
        }
    }
}