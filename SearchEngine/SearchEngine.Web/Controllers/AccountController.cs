using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SearchEngine.Web.ViewModels;

namespace SearchEngine.Web.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AccountController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _httpClientFactory.CreateClient();
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(new { model.Email, model.Password }),
            Encoding.UTF8, "application/json");

        // URL do Gateway!
        var response = await client.PostAsync("http://localhost:5189/identity/auth/login", jsonContent);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Criar claims e autenticar o usuário no MVC
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, authResponse.Email),
                new Claim("jwt_token", authResponse.Token) // Armazena o token para chamadas futuras
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Login Inválido.");
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}

public record AuthResponse(string Token, string Email);