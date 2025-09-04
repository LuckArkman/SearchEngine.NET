var builder = WebApplication.CreateBuilder(args);

// Adicionar e configurar o YARP a partir do appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Mapear as rotas do proxy
app.MapReverseProxy();

app.Run();