using Microsoft.AspNetCore.Mvc;
using BE_TRELLO.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_TRELLO.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public UserController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }


        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                user.UserId,
                user.UserName,
                user.Email,
                user.GoogleId
            });
        }

    }
}