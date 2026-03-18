using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BE_TRELLO.Data; // Thay bằng namespace AppDbContext của bạn
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using BE_TRELLO.Entities.Auth;
using System.Text;
using Google.Apis.Auth;
using System.Text.RegularExpressions;

namespace BE_TRELLO.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {


            var user = _context.Users.FirstOrDefault(u => u.UserName == request.UserName);
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (user == null || !isPasswordValid)
            {
                return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không đúng!" });
            }

            // 2. Chế tạo Token
            var token = CreateToken(user);

            // 3. Trả về cho Swagger / React
            return Ok(new
            {
                token = token,
                userInfo = new
                {
                    id = user.UserId,
                    name = user.UserName,
                    email = user.Email,
                },
                message = "Đăng nhập thành công với userName, password!"
            });

        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Kiểm tra xem Email đã tồn tại chưa... (bạn tự viết đoạn này nhé)

            var user = _context.Users.FirstOrDefault(u => u.UserName == request.UserName);
            if (user != null)
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
            }

            // Check password
            if (!Regex.IsMatch(request.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)"))
            {
                return BadRequest(new { message = "Mật khẩu phải chứa ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số!" });
            }

            // 🎯 ĐÂY LÀ PHÉP THUẬT CỦA BCRYPT: Băm mật khẩu
            // Ví dụ user gõ "123456", biến này sẽ biến thành chuỗi: "$2a$11$Kk3/..."
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new Users
            {
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = hashedPassword, // Lưu chuỗi loằng ngoằng này vào Database!
                GoogleId = null // Đăng ký tay thì không có GoogleId
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!" });
        }


        [HttpGet("my-profile")]
        [Authorize] // Ổ khóa bắt buộc phải có Token mới được gọi API này
        public IActionResult GetMyProfile()
        {
            // Lấy thông tin User đang đăng nhập từ Token
            // (ClaimTypes.NameIdentifier chính là UserId mà ta đã nhét vào thẻ lúc nãy)
            var userName = User.FindFirstValue(ClaimTypes.Name);

            return Ok(new
            {
                Message = "Bạn đã lọt qua được trạm kiểm soát bảo mật!",
                UserName = userName
            });
        }


        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                // 1. Lấy Google Client ID từ appsettings.json ra để làm mốc đối chiếu
                var clientId = _config["Google:ClientId"];

                if (string.IsNullOrEmpty(clientId))
                {
                    return StatusCode(500, new { message = "Chưa cấu hình Google ClientId trên Server" });
                }

                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { clientId },
                };

                // 2. Ném ID Token của React gửi lên cho Google kiểm tra
                // Nếu Token bị sửa đổi hoặc hết hạn, dòng này sẽ văng lỗi ngay!
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

                // 3. Nếu hàng chuẩn, móc Email ra và tìm xem người này từng vào hệ thống chưa
                var user = _context.Users.FirstOrDefault(u => u.Email == payload.Email);

                // 4. CHƯA CÓ TÀI KHOẢN? -> Tự động đăng ký luôn không cần hỏi!
                if (user == null)
                {
                    user = new Users // (Tên class model Users của bạn)
                    {
                        // Tùy theo các cột trong DB của bạn mà điều chỉnh nhé
                        UserName = payload.Name, // Lấy luôn tên Google làm tên hiển thị
                        Email = payload.Email,
                        GoogleId = payload.Subject,
                        PasswordHash = "", // Đăng nhập Google thì mật khẩu để trống
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // 5. Cấp thẻ VIP (JWT) của riêng hệ thống cho người dùng này
                var token = CreateToken(user);

                // 6. Trả về cho React
                return Ok(new
                {
                    token = token,
                    userInfo = new
                    {
                        id = user.UserId,
                        name = user.UserName,
                        email = user.Email,
                    },
                    message = "Đăng nhập bằng Google thành công!"
                });
            }
            catch (InvalidJwtException ex)
            {
                // Bắt lỗi nếu React gửi lên Token tào lao
                // return Unauthorized(new { message = "Token Google không hợp lệ hoặc đã hết hạn!" });

                return Unauthorized(new
                {
                    message = "Lỗi từ Google: " + ex.Message,
                    chi_tiet = "Token bị từ chối tại hàm ValidateAsync"
                });
            }
            catch (Exception ex)
            {
                // Lỗi hệ thống (database sập, lỗi code...)
                return StatusCode(500, new { message = "Lỗi Server: " + ex.Message });
            }
        }

        private string CreateToken(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // Class phụ để hứng dữ liệu từ React (bạn viết nó nằm ngoài AuthController, hoặc ở cuối file)
        public class LoginRequest
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RegisterRequest
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class GoogleLoginRequest
        {
            public string IdToken { get; set; } = string.Empty;
        }
    }

}