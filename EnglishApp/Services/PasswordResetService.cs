namespace EnglishApp.Services;

public class PasswordResetService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public PasswordResetService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;
        return $"{request.Scheme}://{request.Host}";
    }
}