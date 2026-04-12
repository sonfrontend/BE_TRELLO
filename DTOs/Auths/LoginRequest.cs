namespace BE_ECOMMERCE.DTOs.Auths;

public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}