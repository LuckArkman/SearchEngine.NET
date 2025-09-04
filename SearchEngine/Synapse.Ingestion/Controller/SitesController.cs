using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace Synapse.Ingestion.Controller;

public record SubmitUrlRequest(string Url);
public record CrawlJob(string Url);

[ApiController]
[Route("api/[controller]")]
[Authorize] // Este endpoint requer que o usuário esteja autenticado!
public class SitesController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SitesController> _logger;

    public SitesController(IConfiguration configuration, ILogger<SitesController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    public IActionResult SubmitUrl([FromBody] SubmitUrlRequest request)
    {
        if (!Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
        {
            return BadRequest("URL inválida.");
        }

        try
        {
            var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQ:HostName"] };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "url_queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var job = new CrawlJob(request.Url);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job));

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "",
                                     routingKey: "url_queue",
                                     basicProperties: properties,
                                     body: body);
                
                _logger.LogInformation("URL '{Url}' enviada para a fila de crawling.", request.Url);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível enviar a URL para a fila.");
            return StatusCode(500, "Erro interno ao processar a requisição.");
        }

        // TODO: Salvar a URL no banco de dados PostgreSQL associada ao usuário.
        // O ID do usuário pode ser obtido de `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`.
        
        return Ok(new { Message = "Seu site foi enfileirado para indexação." });
    }
}