using AssetHierarchyAPI.Data;
using AssetHierarchyAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

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
            // Username validation
            if (string.IsNullOrWhiteSpace(model.Username) || model.Username.Length < 2)
                return BadRequest(new { message = "Username must be at least 2 characters long" });

            if (model.Username.Length > 20)
                return BadRequest(new { message = "Username must be at max 20 characters long" });

            if (!Regex.IsMatch(model.Username, @"^[a-zA-Z0-9_]{2,20}$"))
                return BadRequest(new { message = "Username must be 2-20 characters and contain only letters, numbers, or underscores" });

            // Password validation
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long" });

            if (model.Password.Length > 30)
                return BadRequest(new { message = "Password must be at max 30 characters long" });

            // Email validation
            if (string.IsNullOrWhiteSpace(model.UserEmail))
                return BadRequest(new { message = "Email is required" });

            try
            {
                var emailCheck = new MailAddress(model.UserEmail);
            }
            catch
            {
                return BadRequest(new { message = "Invalid email format" });
            }

            // Check if username or email already exists
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                return BadRequest(new { message = "Username already exists" });

            if (await _context.Users.AnyAsync(u => u.UserEmail == model.UserEmail))
                return BadRequest(new { message = "Email already exists" });

            // Hash password
            var passwordHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(model.Password))
            );

            var user = new User
            {
                Username = model.Username.Trim(),
                UserEmail = model.UserEmail.Trim(),
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
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Username == model.Username || u.UserEmail == model.UserEmail);
            
            if (user == null) 
                return BadRequest(new { message = "User does not exist" });

            var passwordHash = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(model.Password))
            );

            if (user.Password != passwordHash) 
                return Unauthorized(new { message = "Invalid password" });

            var token = GenerateToken(user);

            // Set cookie with proper settings
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, 
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/" // Ensure cookie is available for all paths
            });

            return Ok(new
            {
                message = "Login successful",
                user = new
                {
                    username = user.Username,
                    email = user.UserEmail,
                    role = user.Role
                }
            });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse") // This should point to google-callback
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return Redirect("http://localhost:5173/login?error=auth_failed");
            }

            var email = result.Principal.FindFirst(c => c.Type == ClaimTypes.Email)?.Value;
            var name = result.Principal.Identity?.Name ?? email?.Split('@')[0];

            if (email == null)
            {
                return Redirect("http://localhost:5173/login?error=no_email");
            }

            // Find or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email);
            if (user == null)
            {
                user = new User
                {
                    Username = name,
                    UserEmail = email,
                    Role = "Viewer"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Generate JWT token
            var token = GenerateToken(user);

            // Store JWT in HttpOnly cookie
            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });

            // ✅ Clear the temporary external cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("http://localhost:5173/auth-success");
        }

        [HttpGet("github-login")]
        public IActionResult Githublogin()
        {
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GithubResponse")
            };
            return Challenge(props, "GitHub");
        }
        [HttpGet("github-callback")]
        public async Task<IActionResult> GithubResponse()
        {
            var res = await HttpContext.AuthenticateAsync("GitHub");
            if(!res.Succeeded)
                return Redirect("http://localhost:5173/login?error=auth_failed");

            var email = res.Principal.FindFirst(c => c.Type == ClaimTypes.Email)?.Value;
            var name = res.Principal.Identity?.Name ?? email?.Split('@')[0];

            if (email == null)
            {
                email = res.Principal.FindFirst(c => c.Type == ClaimTypes.Name)?.Value + "@github.com";
            }
            var user =  await _context.Users.FirstOrDefaultAsync(c => c.UserEmail == email);
            if (user == null) {
                user = new User
                {
                    Username = name,
                    UserEmail = email,
                    Role = "Viewer",
                   
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            var token = GenerateToken(user);

            Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("http://localhost:5173/auth-success");

        }


        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("AuthToken", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax
            });
            return Ok(new { message = "Logged out" });
        }

        [HttpGet("me")]
        [Authorize] // Add authorization attribute
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Get username from claims
                var username = User.Identity?.Name;
                var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;


                return Ok(new
                {
                    //isAuthenticated = User.Identity?.IsAuthenticated,
                    //claims = User.Claims.Select(c => new { c.Type, c.Value })
                    username,
                    email,
                    role
                });

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error retrieving user information", error = ex.Message });
            }
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.UserEmail),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Changed to UtcNow
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RegisterModel
        {
            public string Username { get; set; }
            public string UserEmail { get; set; }
            public string Password { get; set; }
            public string Role { get; set; } = "Viewer";
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string UserEmail { get; set; }
            public string Password { get; set; }
        }
    }
}