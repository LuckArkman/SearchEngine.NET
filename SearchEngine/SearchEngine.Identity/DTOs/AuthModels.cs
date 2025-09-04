using System.ComponentModel.DataAnnotations;

namespace SearchEngine.Identity.DTOs;

public class AuthModels
{
    public record RegisterModel([Required] string Email, [Required] string Password);
    public record LoginModel([Required] string Email, [Required] string Password);
    public record AuthResponse(string Token, string Email);
}