using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SearchEngine.Web.ViewModels;

namespace SearchEngine.Web.Controllers;


[Authorize] // Só usuários logados podem acessar
public class DashboardController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> SubmitSite(SubmitSiteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "URL inválida.";
            return View("Index", model);
        }

        var token = User.FindFirst("jwt_token")?.Value;
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account"); // Token expirou ou não existe
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var content = new StringContent(
            JsonSerializer.Serialize(new { model.Url }), 
            Encoding.UTF8, 
            "application/json");

        // URL do Gateway!
        var response = await client.PostAsync("http://localhost:5189/ingestion/sites", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Seu site foi enviado para indexação com sucesso!";
        }
        else
        {
            TempData["ErrorMessage"] = "Ocorreu um erro ao enviar seu site. Tente novamente.";
        }

        return RedirectToAction("Index");
    }
}