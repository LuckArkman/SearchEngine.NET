using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Synapse.Crawler;

public record CrawlJob(string Url);

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConnection _connection;
    private IModel _channel;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQ:HostName"], DispatchConsumersAsync = true };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "url_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.BasicQos(0, 1, false); // Processar uma mensagem por vez

        _logger.LogInformation("Crawler Worker iniciado e aguardando mensagens.");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Mensagem recebida: {Message}", message);

            try
            {
                var job = JsonSerializer.Deserialize<CrawlJob>(message);
                if (job != null)
                {
                    await ProcessUrl(job.Url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar a URL.");
            }
            finally
            {
                // Confirma o recebimento e processamento da mensagem
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        };
        
        _channel.BasicConsume(queue: "url_queue", autoAck: false, consumer: consumer);
        
        await Task.CompletedTask;
    }

    private async Task ProcessUrl(string url)
    {
        _logger.LogInformation("Iniciando crawling da URL: {Url}", url);
        try
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);

            var title = doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText.Trim() ?? "Sem Título";
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            var content = bodyNode != null ? ExtractText(bodyNode) : "";

            if (!string.IsNullOrWhiteSpace(content))
            {
                var pageContent = new PageContent(url, title, content);
                PublishContentForIndexing(pageContent);
                _logger.LogInformation("Conteúdo da URL '{Url}' publicado para indexação.", url);
            }
            else
            {
                _logger.LogWarning("Nenhum conteúdo extraído da URL: {Url}", url);
            }

            // TODO: Extrair e enfileirar novos links para crawling.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no crawling da URL: {Url}", url);
        }
    }
    private void PublishContentForIndexing(PageContent content)
    {
        _channel.QueueDeclare(queue: "content_queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content));

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(exchange: "",
            routingKey: "content_queue",
            basicProperties: properties,
            body: body);
    }
    private string ExtractText(HtmlNode node)
    {
        // Remove scripts e styles para limpar o conteúdo
        node.Descendants().Where(n => n.Name == "script" || n.Name == "style").ToList().ForEach(n => n.Remove());
    
        // Converte o texto, normalizando espaços em branco
        var text = node.InnerText;
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }
    public record PageContent(string Url, string Title, string Content);
}
