using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetHierarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("signup")]
        public async Task<ActionResult> Signup([FromBody] RegisterModel model)
        {
            if (model.Username.Length < 2 || string.IsNullOrWhiteSpace(model.Username)) {
                return BadRequest(new { message = "Username must be at least 2 characters long" });
            }
            if (model.Username.Length > 20 || string.IsNullOrWhiteSpace(model.Username))
            {
                return BadRequest(new { message = "Username must be at max 20 characters long" });
            }
            if (model.Password.Length < 6 || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Password must be at least 6 characters long" });
            }
            if (model.Password.Length > 30 || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Password must be at max 30 characters long not more than it " });
            }
            if (!Regex.IsMatch(model.Username, @"^[a-zA-Z0-9_]{2,20}$"))
                return BadRequest(new { message = "Username must be 2-20 characters and contain only letters, numbers, or underscores" });

            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return BadRequest( new { message = "User already exists" });

            var passwordHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(model.Password))
            );

            var user = new User
            {
                Username = model.Username,
                Password = passwordHash,
                Role = "Viewer" // default role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null) return BadRequest(new { message = "User does not exist" });

            var passwordHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(model.Password))
            );

            if (user.Password != passwordHash) return Unauthorized(new { message = "Invalid password" });

            var token = GenerateToken(user);

            return Ok(new
            {
                token,
                role = user.Role,
                username = user.Username
            });
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RegisterModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; } = "Viewer";
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}
