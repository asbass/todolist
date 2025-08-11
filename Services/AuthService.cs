using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using todolist.data;
using todolist.Helpers;
using todolist.model;

namespace todolist.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(UserManager<User> userManager,RoleManager<IdentityRole> roleManager, AppDbContext context, JwtService jwt)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _jwt = jwt;
        }

        public async Task<bool> UserExists(string username)
            => await _userManager.FindByNameAsync(username.ToLower()) != null;

        public async Task<User> Register(string username, string password)
        {
            var user = new User { UserName = username.ToLower() };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                throw new Exception("Tạo user thất bại: " + string.Join("; ", result.Errors.Select(e => e.Description)));

            // Gán role mặc định
            await _userManager.AddToRoleAsync(user, "User");

            return user;
        }

        public async Task<string> Login(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return null;

            var isValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isValid)
                return null;

            return await _jwt.GenerateToken(user);
        }


    }

}
