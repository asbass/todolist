using Microsoft.AspNetCore.Mvc;
using todolist.DTOs;
using todolist.Services;

namespace todolist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;

        public AuthController(AuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            if (await _auth.UserExists(dto.Username))
                return BadRequest("Username already exists");

            var user = await _auth.Register(dto.Username, dto.Password);
            return Ok(new { user.Id, user.UserName });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            var token = await _auth.Login(dto.Username, dto.Password);
            if (token == null) return Unauthorized("Invalid credentials");

            return Ok(new { token });
        }
    }

}
