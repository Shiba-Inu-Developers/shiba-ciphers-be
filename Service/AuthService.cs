using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;


namespace my_new_app.Service;

using my_new_app.Model;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context)
    {
        this._context = context;
        this._configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json").Build();
    }

    public static string GenerateOtp()
    {
        var rng = new Random();
        return rng.Next(100000, 999999).ToString(); // Generates a 6-digit OTP
    }

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var emailSettings = _configuration.GetSection("EmailSettings");

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["Sender"]));
        emailMessage.To.Add(new MailboxAddress("", email));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("plain") { Text = message };

        using var client = new SmtpClient();
        await client.ConnectAsync(emailSettings["MailServer"], int.Parse(emailSettings["MailPort"]),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(emailSettings["Sender"], emailSettings["Password"]);
        await client.SendAsync(emailMessage);
        await client.DisconnectAsync(true);
    }

    public bool AuthenticateUser(string email, string password)
    {
        var user = _context.MyUsers.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, user.Password);
    }

    public async Task<bool> RegisterUser(string email, string password)
    {
        try
        {
            if (_context.MyUsers.Any(u => u.Email == email))
            {
                return false;
            }


            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            var otp = GenerateOtp();

            var newUser = new User
            {
                Email = email,
                Password = hashedPassword,
                Otp = otp,
                IsVerified = false,
                RefreshToken = "empty",
                RefreshTokenExpiryTime = DateTime.UtcNow
            };

            _context.MyUsers.Add(newUser);
            await _context.SaveChangesAsync();

            await SendEmailAsync(newUser.Email, "Your OTP", $"Your one-time password is: {otp}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return false;
        }
    }


    public string GenerateJwtToken(User user)
    {
        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    "Jt4CFcDUL1t09iWexlPkdjb4pKDoKaNiOTrs8Kqwl12VxYlPPsJ")); // Replace with your secret key
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            // Add other claims as needed
        };

        var token = new JwtSecurityToken(
            issuer: "MyAppAPI",
            audience: "MyAppUsers",
            claims: claims,
            expires: DateTime.Now.AddMinutes(30), // Token expiration time
            signingCredentials: credentials);
        
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32]; // 32 bytes will generate a 256-bit string
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
    
    public async Task<User> GetUserByEmail(string email)
    {
        return await _context.GetUserByEmailAsync(email);
    }

    public async Task UpdateUser(User user)
    {
        await _context.UpdateUserAsync(user);
    }
    
    public string GetEmailFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        if (jwtToken == null)
        {
            return null;
        }

        var emailClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "sub");
        return emailClaim?.Value;
    }
}