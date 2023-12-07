using Microsoft.EntityFrameworkCore;
using my_new_app.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
namespace my_new_app.Service;
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ApplicationDbContext context;
    private readonly AuthService authService;
   
   public BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    ApplicationDbContext context)
    : base(options, logger, encoder, clock)
{
    this.context = context;
    this.authService = new AuthService(context);
}

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
    {
        var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authorizationHeader != null && authorizationHeader.StartsWith("Basic "))
        {
            var encodedUsernamePassword = authorizationHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1]?.Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
            var username = decodedUsernamePassword.Split(':', 2)[0];
            var password = decodedUsernamePassword.Split(':', 2)[1];

            var user = context.MyUsers.FirstOrDefault(u => u.Email == username);

            if (user == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("User not found."));
            }

            bool isAuthenticated = authService.AuthenticateUser(username, password);

            if (!isAuthenticated)
            {
               return Task.FromResult(AuthenticateResult.Fail("Invalid password."));
            }

            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        Logger.LogInformation("Authentication succeeded for user");
        return Task.FromResult(AuthenticateResult.Fail("Authentication failed."));
        }
    catch (Exception ex)
    {
        // Log any exceptions or failures
        Logger.LogError(ex, "Authentication failed.");

        return Task.FromResult(AuthenticateResult.Fail("Authentication failed."));
    }
    }
}