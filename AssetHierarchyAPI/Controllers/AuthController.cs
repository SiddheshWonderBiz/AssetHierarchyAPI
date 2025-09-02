using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
            _config = config;
            _context = context;
        }

        [HttpPost("signup")]
        public async Task<ActionResult> Signup([FromBody] RegisterModel model)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    return BadRequest("User already exists");
                }

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
            catch (Exception ex)
            {
                return StatusCode(500, $"Signup error: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                Console.WriteLine($"Login attempt for user: {model.Username}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null)
                {
                    Console.WriteLine($"User {model.Username} not found");
                    return BadRequest("User does not exist");
                }

                var passwordHash = Convert.ToBase64String(
                    SHA256.HashData(Encoding.UTF8.GetBytes(model.Password))
                );

                Console.WriteLine($"Password hash comparison for {model.Username}");
                Console.WriteLine($"Stored hash: {user.Password}");
                Console.WriteLine($"Computed hash: {passwordHash}");

                if (user.Password != passwordHash)
                {
                    Console.WriteLine($"Password mismatch for user {model.Username}");
                    return Unauthorized("Invalid password");
                }

                var token = GenerateToken(user);

                Console.WriteLine($"Login successful for {model.Username}, role: {user.Role}");
                Console.WriteLine($"Generated token preview: {token.Substring(0, Math.Min(50, token.Length))}...");

                // Return consistent response format
                return Ok(new
                {
                    token = token,
                    role = user.Role,
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return StatusCode(500, $"Login error: {ex.Message}");
            }
        }

        private string GenerateToken(User user)
        {
            try
            {
                var jwtKey = _config["Jwt:Key"];
                var jwtIssuer = _config["Jwt:Issuer"];
                var jwtAudience = _config["Jwt:Audience"];

                Console.WriteLine($"JWT Config - Key length: {jwtKey?.Length}, Issuer: {jwtIssuer}, Audience: {jwtAudience}");

                if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
                {
                    throw new Exception("JWT Key is missing or too short (minimum 32 characters required)");
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    //new Claim("username", user.Username), // additional claim for easier access
                    //new Claim("role", user.Role) // additional claim for easier access
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                Console.WriteLine($"Token generated successfully for {user.Username}");

                return tokenString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token generation error: {ex.Message}");
                throw new Exception($"Token generation failed: {ex.Message}");
            }
        }

        // DTOs
        public class RegisterModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Role { get; set; } = "Viewer"; // default to Viewer
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}