namespace my_new_app.Model;

public class RefreshToken
{
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}