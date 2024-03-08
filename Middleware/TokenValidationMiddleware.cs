using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using my_new_app.IdemRiesim;
using my_new_app.Service; // Ensure this is the correct namespace for JwtSettings


namespace my_new_app.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwtSettings _jwtSettings;
        private readonly AuthService _authService;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next, IOptions<JwtSettings> jwtSettings, ILogger<TokenValidationMiddleware> logger, AuthService authService)
        {
            _next = next;
            _jwtSettings = jwtSettings.Value; // Access to Key, Issuer, and Audience
            _authService = authService;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var securedPaths = new List<string> { "/weatherforecast/Users","/weatherforecast/user-info","/weatherforecast/update-user-info" };

            if (securedPaths.Any(path => context.Request.Path.StartsWithSegments(path)))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || authHeader.Equals("Bearer null", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Authentication token is missing or invalid.");
                    
                    return;
                }

                var token = authHeader.Split(" ").Last();

                var isValid = ValidateToken(token);
                if (!isValid)
                {
                    // Attempt to refresh the token here
                    string newToken = await RefreshToken(context, token);
                    if (string.IsNullOrEmpty(newToken))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Unable to refresh token or token is expired.");
                        return;
                    }
                    else
                    {

                        // Replace the old token with the new one in the request header
                        context.Request.Headers["Authorization"] = $"Bearer {newToken}";
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync(newToken);
                        return;
                    }
                }
            }

            await _next(context);
        }




        private bool ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtSettings.Key)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                return true;
            }
            catch (SecurityTokenException)
            {
                return false;
            }
        }
        
        private async Task<string> RefreshToken(HttpContext context, string token)
        {
            var email = _authService.GetEmailFromToken(token);
            if (email == null)
            {
                return null;
            }

            var user = await _authService.GetUserByEmail(email);
            if (user == null)
            {
                return null;
            }
            
            if (user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return null;
            }

            var newJwtToken = _authService.GenerateJwtToken(user); // Implement this method to generate a new JWT token

            // Write new tokens to response
            context.Response.Headers["JWT-Token"] = newJwtToken;

            return newJwtToken;
        }

    }
}