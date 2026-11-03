using System.Text.Json.Serialization;
using Camisa10.Api.Data;
using Camisa10.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Railway (e outras plataformas) dizem a porta pela variável PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var mongo = builder.Configuration.GetConnectionString("Mongo") ?? "mongodb://localhost:27017";
var database = builder.Configuration["Mongo:Database"] ?? "camisa10";
builder.Services.AddDbContext<Camisa10DbContext>(o => o.UseMongoDB(mongo, database));

builder.Services.AddSingleton(new DatabaseStatus(DescribeMongo(mongo)));
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapOpenApi();

app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
// Sempre responde (para o Railway saber que a API está no ar) e diz se o banco conectou.
app.MapGet("/health", (DatabaseStatus db) => Results.Ok(new
{
    status = "ok",
    mongo = db.Connected ? "conectado" : "sem conexão",
    mongoServidor = db.Server,
    mongoErro = db.Connected ? null : db.LastError
}));
app.MapCareerEndpoints();

app.Run();

/// <summary>Servidor do Mongo sem usuário e senha, para mostrar em logs e no /health.</summary>
static string DescribeMongo(string connectionString)
{
    try
    {
        var url = MongoUrl.Create(connectionString);
        return string.Join(",", url.Servers.Select(s => $"{s.Host}:{s.Port}"));
    }
    catch
    {
        return "(connection string inválida)";
    }
}

public partial class Program;

/// <summary>Estado da conexão com o MongoDB.</summary>
public sealed class DatabaseStatus(string server)
{
    public string Server { get; } = server;
    public volatile bool Connected;
    public string? LastError;
}

/// <summary>
/// Cria a coleção e os índices no MongoDB. Se o banco não responder, a API continua no ar
/// e tenta de novo a cada pouco, explicando o problema no log.
/// </summary>
public sealed class DatabaseInitializer(IServiceProvider services, DatabaseStatus status, ILogger<DatabaseInitializer> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<Camisa10DbContext>().Database.EnsureCreatedAsync(stoppingToken);
                status.Connected = true;
                status.LastError = null;
                log.LogInformation("MongoDB conectado em {Server}", status.Server);
                return;
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                status.LastError = Summarize(e);
                log.LogError("Não foi possível conectar ao MongoDB em {Server}: {Error}. Confira a variável ConnectionStrings__Mongo " +
                             "(no Railway: ${{{{MongoDB.MONGO_URL}}}}). Nova tentativa em {Delay} s.",
                    status.Server, status.LastError, (int)delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Min(60, delay.TotalSeconds * 2));
            }
        }
    }

    /// <summary>Causa do erro em uma linha (a mensagem do driver traz o estado inteiro do cluster).</summary>
    private static string Summarize(Exception e)
    {
        var text = e.ToString();
        foreach (var marker in new[] { "SocketException", "MongoAuthenticationException", "MongoConnectionException" })
        {
            var i = text.IndexOf(marker + " ", StringComparison.Ordinal);
            if (i < 0) i = text.IndexOf(marker + ":", StringComparison.Ordinal);
            if (i < 0) continue;
            var line = text[i..].Split('\n', '\r')[0];
            var colon = line.IndexOf(": ", StringComparison.Ordinal);
            return (colon > 0 ? line[(colon + 2)..] : line).Trim().TrimEnd('"', '\\');
        }
        var msg = e.GetBaseException().Message;
        return msg.Length > 200 ? msg[..200] + "…" : msg;
    }
}
