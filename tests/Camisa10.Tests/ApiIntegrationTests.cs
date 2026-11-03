using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Camisa10.Domain.Views;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;

namespace Camisa10.Tests;

/// <summary>Roda só quando a variável CAMISA10_TEST_MONGO aponta para um MongoDB (ex.: mongodb://localhost:27017).</summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public static readonly string? Url = Environment.GetEnvironmentVariable("CAMISA10_TEST_MONGO");
    public MongoFactAttribute()
    {
        if (string.IsNullOrEmpty(Url)) Skip = "Defina CAMISA10_TEST_MONGO para rodar os testes com MongoDB.";
    }
}

public class ApiIntegrationTests : IDisposable
{
    private readonly string _db = "camisa10_test_" + Guid.NewGuid().ToString("N")[..8];
    private readonly WebApplicationFactory<Program>? _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public ApiIntegrationTests()
    {
        if (MongoFactAttribute.Url == null) return;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Mongo", MongoFactAttribute.Url);
            b.UseSetting("Mongo:Database", _db);
        });
    }

    private HttpClient Client(string player = "player-integration-1")
    {
        var c = _factory!.CreateClient();
        c.DefaultRequestHeaders.Add("X-Player-Id", player);
        return c;
    }

    [MongoFact]
    public async Task Carreira_salva_e_recarregada_identica_a_cada_acao()
    {
        var http = Client();
        var created = await http.PostAsJsonAsync("/api/careers", new { name = "Teste", club = "Santos", nation = "Brasil", position = "Atacante" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var view = (await created.Content.ReadFromJsonAsync<GameView>(Json))!;
        var rng = new Random(42);

        for (var i = 0; i < 400 && view.Status == "Active"; i++)
        {
            object action = view.Phase switch
            {
                "Event" => new { type = "Choose", index = rng.Next(view.Event!.Options.Count), version = view.Version },
                "Penalty" => new { type = "Choose", index = rng.Next(6), version = view.Version },
                "Market" => new { type = "Stay", index = (int?)null, version = view.Version },
                "Swap" => new { type = "Choose", index = 0, version = view.Version },
                _ => new { type = "Continue", index = (int?)null, version = view.Version }
            };
            var res = await http.PostAsJsonAsync($"/api/careers/{view.Id}/actions", action);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var after = await res.Content.ReadAsStringAsync();

            var reloaded = await http.GetStringAsync($"/api/careers/{view.Id}");
            Assert.Equal(after, reloaded);

            // Repetir a mesma versão (toque duplo) não aplica a ação de novo.
            if (i % 50 == 0)
            {
                var dup = await http.PostAsJsonAsync($"/api/careers/{view.Id}/actions", action);
                Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
            }
            view = JsonSerializer.Deserialize<GameView>(after, Json)!;
        }

        var history = await http.GetFromJsonAsync<HistoryView>($"/api/careers/{view.Id}/history", Json);
        Assert.NotNull(history);

        // Outro jogador não enxerga a carreira.
        var other = Client("player-integration-2");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/careers/{view.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync($"/api/careers/{view.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/careers/{view.Id}")).StatusCode);
    }

    [MongoFact]
    public async Task Requisicoes_invalidas_sao_rejeitadas()
    {
        var anon = _factory!.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await anon.GetAsync("/api/careers")).StatusCode);

        var http = Client();
        var bad = await http.PostAsJsonAsync("/api/careers", new { name = "X", club = "Clube Inexistente", nation = "Brasil", position = "Meia" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var created = await http.PostAsJsonAsync("/api/careers", new { name = "X", club = "Flamengo", nation = "Brasil", position = "Meia" });
        var view = (await created.Content.ReadFromJsonAsync<GameView>(Json))!;
        Assert.Equal("Calendar", view.Phase);
        var wrong = await http.PostAsJsonAsync($"/api/careers/{view.Id}/actions", new { type = "Retire", version = view.Version });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
    }

    public void Dispose()
    {
        if (_factory == null) return;
        _factory.Dispose();
        new MongoClient(MongoFactAttribute.Url).DropDatabase(_db);
    }
}
