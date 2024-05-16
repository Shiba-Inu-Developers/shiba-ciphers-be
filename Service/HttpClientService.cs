public class HttpClientService
{
    private readonly HttpClient _httpClient;

    public HttpClientService()
    {
        _httpClient = new HttpClient();
    }
    public HttpClient CreateClient()
    {
        return _httpClient;
    }
}