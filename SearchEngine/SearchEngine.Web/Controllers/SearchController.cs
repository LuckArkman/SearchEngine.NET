using Microsoft.AspNetCore.Mvc;
using SearchEngine.Shared.Contracts;
using System.Text.Json;

namespace SearchEngine.Web.Controllers;

public class SearchController : Controller
{
    private readonly HttpClient _httpClient;

    public SearchController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiGateway");
    }

    public async Task<IActionResult?> Results(string query, int page = 1)
    {
        var response = await _httpClient.GetAsync($"/api/search?query={query}&page={page}");
        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadFromJsonAsync<PagedResult<SearchResultModel>>();
            return View(results);
        }
        return View("Error");
    }
}