using Microsoft.AspNetCore.Mvc;
using my_new_app.Model;
using my_new_app.DTOs;
using my_new_app.Service;

namespace my_new_app.Controllers;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ApplicationDbContext context;
    private readonly AuthService authService;

    public WeatherForecastController(ApplicationDbContext context)
    {
        this.context = context;
        authService = new AuthService(context);
    }

    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
    {
        try
        {
            if (loginDTO == null)
            {
                return BadRequest("Invalid data");
            }

            bool isAuthenticated = authService.AuthenticateUser(loginDTO.Email, loginDTO.Password);

            if (!isAuthenticated)
            {
                return Unauthorized(new { error = "Invalid credentials" });
            }

            var user = context.MyUsers.FirstOrDefault(u => u.Email == loginDTO.Email);
            var token = authService.GenerateJwtToken(user);
            string refreshToken = authService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            context.SaveChanges();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(1),
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

            return Ok(new { token = token, message = "Login successful" });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        var users = context.MyUsers.ToList();

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }

    [HttpGet]
    [Route("users")]
    public IEnumerable<User> GetUsers()
    {
        var users = context.MyUsers.ToList();
        return users;
    }

    [HttpGet]
    [Route("User/{id}")]
    public IActionResult GetUserById(int id)
    {
        var user = context.MyUsers.FirstOrDefault(u => u.Id == id);
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

    [HttpGet("UserEmail/{email}")]
    public IActionResult GetUserByEmail(string email)
    {
        var user = context.MyUsers.FirstOrDefault(u => u.Email == email);
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

            Task<bool> registrationSuccess = authService.RegisterUser(createUserDTO.Email, createUserDTO.Password);
            registrationSuccess.Wait();

            if (registrationSuccess.Result)
            {
                return Ok(new { message = "Registration successful" });
            }
            else
            {
                return BadRequest(new { error = "Registration failed" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }

    [HttpPost]
    [Route("VerifyOtp")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpDTO verifyOtpDTO)
    {
        try
        {
            if (verifyOtpDTO == null)
            {
                return BadRequest("Invalid data");
            }

            var user = context.MyUsers.FirstOrDefault(u => u.Email == verifyOtpDTO.Email);
            if (user == null)
            {
                return NotFound();
            }

            if (user.Otp != verifyOtpDTO.Otp)
            {
                return BadRequest(new { error = "Invalid OTP" });
            }

            user.IsVerified = true;
            context.SaveChanges();

            return Ok(new { message = "OTP verified" });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }

    [HttpGet]
    [Route("hello")]
    //[Authorize(AuthenticationSchemes = "Basic")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public IActionResult Hello()
    {
        return Ok(new { message = "Hello World!" });
    }
}