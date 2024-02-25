using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace my_new_app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("Users")]
        public IActionResult GetUsers()
        {
            var users = _context.MyUsers.ToList();
            return Ok(users);
        }

        [HttpGet]
        [Route("User/{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.MyUsers.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            var userDto = new UserDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.Email
            };
            return Ok(userDto);
        }

        [HttpPost]
        [Route("User")]
        public IActionResult CreateUser([FromBody] CreateUserDTO createUserDTO)
        {
            try
            {
                if (createUserDTO == null)
                {
                    return BadRequest("Invalid data");
                }
                if (_context.MyUsers.Any(u => u.Email == createUserDTO.Email))
                {
                    return Conflict(new { error = "A user with this email already exists." });
                }

                var newUser = new User
                {
                    Email = createUserDTO.Email,
                    Password = createUserDTO.Password,
                };

                _context.MyUsers.Add(newUser);
                _context.SaveChanges();

                var createdUserDTO = new UserDTO
                {
                    Id = newUser.Id,
                    Email = newUser.Email,
                    UserName = newUser.Email
                };

                return CreatedAtAction(nameof(GetUserById), new { id = createdUserDTO.Id }, createdUserDTO);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { error = "Internal Server Error" });
            }
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Invalid request.");
            }

            // Implement refresh token logic
            // var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            // if (result.NewAccessToken == null)
            // {
            //     return Unauthorized("Invalid or expired refresh token.");
            // }

            // Temporary response for demonstration purposes
            return Ok(new
            {
                AccessToken = "NewAccessToken",
                RefreshToken = "NewRefreshToken"
            });
        }

        private (bool IsValid, string UserId) ValidateRefreshToken(string refreshToken)
        {
            // Implement logic to validate the refresh token, e.g., checking it against a database
            // For simplicity, this is a placeholder implementation
            return (true, "userId"); // Placeholder: return actual validation result
        }
    }
}