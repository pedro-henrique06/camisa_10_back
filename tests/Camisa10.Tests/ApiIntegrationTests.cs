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

    [MongoFact]
    public async Task Carreira_salva_antes_das_copas_abre_com_o_calendario_completo()
    {
        var http = Client();
        var created = await http.PostAsJsonAsync("/api/careers", new { name = "X", club = "Palmeiras", nation = "Brasil", position = "Meia" });
        var view = (await created.Content.ReadFromJsonAsync<GameView>(Json))!;

        // documento como era antes: sem a lista de competições
        var careers = new MongoClient(MongoFactAttribute.Url).GetDatabase(_db).GetCollection<MongoDB.Bson.BsonDocument>("careers");
        await careers.UpdateOneAsync(new MongoDB.Bson.BsonDocument("_id", view.Id), new MongoDB.Bson.BsonDocument("$unset", new MongoDB.Bson.BsonDocument("Competitions", "")));

        var reloaded = await http.GetFromJsonAsync<GameView>($"/api/careers/{view.Id}", Json);
        Assert.NotEmpty(reloaded!.Calendar!.Competitions);
        Assert.Contains(reloaded.Calendar.Cells, c => c.Kind == "league");
    }

    [MongoFact]
    public async Task Carreira_de_versao_antiga_sem_campos_novos_abre_depois_da_atualizacao()
    {
        var http = Client("player-versao-antiga");
        var created = await http.PostAsJsonAsync("/api/careers", new { name = "X", club = "Palmeiras", nation = "Brasil", position = "Meia" });
        var view = (await created.Content.ReadFromJsonAsync<GameView>(Json))!;

        // documento como era antes da tabela ao vivo, da mesa e da sala de troféus (inclusive dentro das competições)
        var careers = new MongoClient(MongoFactAttribute.Url).GetDatabase(_db).GetCollection<MongoDB.Bson.BsonDocument>("careers");
        var unset = new MongoDB.Bson.BsonDocument
        {
            { "LeagueOrder", "" }, { "LeagueResults", "" }, { "LeagueRound", "" }, { "Trophies", "" }, { "SeasonMatches", "" },
            { "Competitions.$[].KoRounds", "" }, { "Competitions.$[].Sequential", "" }, { "Overall", "" }
        };
        await careers.UpdateOneAsync(new MongoDB.Bson.BsonDocument("_id", view.Id), new MongoDB.Bson.BsonDocument("$unset", unset));
        var broken = await http.GetAsync($"/api/careers/{view.Id}");
        Assert.Equal(HttpStatusCode.InternalServerError, broken.StatusCode);

        // a API sobe de novo (deploy): completa o documento e a carreira volta a abrir e a jogar
        using var restarted = _factory!.WithWebHostBuilder(_ => { });
        var http2 = restarted.CreateClient();
        http2.DefaultRequestHeaders.Add("X-Player-Id", "player-versao-antiga");
        for (var i = 0; i < 50; i++)
        {
            var health = await http2.GetStringAsync("/health");
            if (health.Contains("\"conectado\"")) break;
            await Task.Delay(200);
        }
        var reloaded = await http2.GetFromJsonAsync<GameView>($"/api/careers/{view.Id}", Json);
        Assert.Equal(55, reloaded!.Hud.Overall);
        var list = await http2.GetFromJsonAsync<List<JsonElement>>("/api/careers");
        Assert.Single(list!);
        var next = await http2.PostAsJsonAsync($"/api/careers/{view.Id}/actions", new { type = "Continue", version = reloaded.Version });
        Assert.True(next.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadRequest, next.StatusCode.ToString());
    }

    public void Dispose()
    {
        if (_factory == null) return;
        _factory.Dispose();
        new MongoClient(MongoFactAttribute.Url).DropDatabase(_db);
    }
}

public class MesaIntegrationTests : IDisposable
{
    private readonly string _db = "camisa10_mesa_" + Guid.NewGuid().ToString("N")[..8];
    private readonly WebApplicationFactory<Program>? _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public MesaIntegrationTests()
    {
        if (MongoFactAttribute.Url == null) return;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Mongo", MongoFactAttribute.Url);
            b.UseSetting("Mongo:Database", _db);
        });
    }

    private HttpClient Client(string player)
    {
        var c = _factory!.CreateClient();
        c.DefaultRequestHeaders.Add("X-Player-Id", player);
        return c;
    }

    private static async Task<JsonElement> Json_(HttpResponseMessage r) => JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync());

    /// <summary>Joga um lado do duelo até ele acabar (as escolhas do outro lado vêm da outra chamada).</summary>
    private static async Task<bool> PlayDuel(HttpClient a, HttpClient b, string duelId)
    {
        for (var i = 0; i < 40; i++)
        {
            var progressed = false;
            foreach (var (http, pick) in new[] { (a, 0), (b, 2) })
            {
                var v = await Json_(await http.GetAsync($"/api/duels/{duelId}"));
                if (v.GetProperty("finished").GetBoolean()) return true;
                var turn = v.GetProperty("turn");
                if (turn.GetProperty("myChoice").ValueKind == JsonValueKind.Null)
                {
                    var r = await http.PostAsJsonAsync($"/api/duels/{duelId}/choose", new { index = (i + pick) % 3 });
                    Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                    progressed = true;
                }
            }
            if (!progressed) return false;
        }
        return false;
    }

    [MongoFact]
    public async Task Mesa_com_dois_amigos_anda_junto_tem_classico_em_duelo_e_tabelas_iguais()
    {
        var ana = Client("player-mesa-ana");
        var beto = Client("player-mesa-beto");
        var created = await Json_(await ana.PostAsJsonAsync("/api/mesas", new { name = "Mesa de teste", playerName = "Ana" }));
        var mesaId = created.GetProperty("id").GetString()!;
        var code = created.GetProperty("code").GetString()!;
        Assert.Equal(6, code.Length);
        var joined = await beto.PostAsJsonAsync("/api/mesas/join", new { code, playerName = "Beto" });
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);

        var ca = await Json_(await ana.PostAsJsonAsync($"/api/mesas/{mesaId}/careers", new { name = "Ana", club = "Palmeiras", nation = "Brasil", position = "Atacante" }));
        var cb = await Json_(await beto.PostAsJsonAsync($"/api/mesas/{mesaId}/careers", new { name = "Beto", club = "Corinthians", nation = "Brasil", position = "Zagueiro" }));
        var ids = new[] { ca.GetProperty("id").GetString()!, cb.GetProperty("id").GetString()! };
        var clients = new[] { ana, beto };
        var rng = new Random(3);
        var leagueDuels = new HashSet<string>();
        var waited = 0;

        // joga as duas carreiras alternando: quem está na frente espera o outro
        for (var step = 0; step < 6000; step++)
        {
            var who = step % 2;
            var http = clients[who];
            var v = await Json_(await http.GetAsync($"/api/careers/{ids[who]}"));
            var phase = v.GetProperty("phase").GetString();
            if (v.GetProperty("status").GetString() == "Finished") break;
            if (leagueDuels.Count >= 2) break;
            if (phase == "Duel")
            {
                var duelId = v.GetProperty("duel").GetProperty("duelId").GetString()!;
                leagueDuels.Add(duelId);
                var other = clients[1 - who];
                var otherView = await Json_(await other.GetAsync($"/api/careers/{ids[1 - who]}"));
                // o amigo ainda não chegou no clássico: este lado espera (o outro vai jogar até chegar)
                if (otherView.GetProperty("phase").GetString() != "Duel" && !(await Json_(await http.GetAsync($"/api/duels/{duelId}"))).GetProperty("finished").GetBoolean())
                {
                    step++;
                    // deixa o outro andar
                    var o = otherView;
                    for (var k = 0; k < 400 && o.GetProperty("phase").GetString() != "Duel"; k++)
                    {
                        var r2 = await Act(other, ids[1 - who], o, rng);
                        if (r2 == null) break;
                        o = r2.Value;
                    }
                }
                Assert.True(await PlayDuel(clients[0], clients[1], duelId), "duelo terminou");
            }
            var res = await Act(http, ids[who], v, rng);
            if (res == null) waited++;
        }

        Assert.True(leagueDuels.Count > 0, $"nenhum clássico; idades {string.Join(",", await Task.WhenAll(ids.Select(async (id, i) => (await Json_(await clients[i].GetAsync($"/api/careers/{id}"))).GetProperty("hud").GetProperty("age").GetInt32())))}");
        Assert.True(waited > 0, "o calendário junto fez alguém esperar");

        // depois do clássico, as duas tabelas da liga são iguais (mesmo mundo e placares reais compartilhados)
        var va = await Json_(await ana.GetAsync($"/api/careers/{ids[0]}"));
        var vb = await Json_(await beto.GetAsync($"/api/careers/{ids[1]}"));
        var ta = va.GetProperty("calendar").GetProperty("table");
        var tb = vb.GetProperty("calendar").GetProperty("table");
        if (ta.GetProperty("round").GetInt32() == tb.GetProperty("round").GetInt32() && ta.GetProperty("league").GetString() == tb.GetProperty("league").GetString())
        {
            string Rows(JsonElement t) => string.Join(";", t.GetProperty("rows").EnumerateArray()
                .Select(r => $"{r.GetProperty("club").GetString()}:{r.GetProperty("points").GetInt32()}:{r.GetProperty("played").GetInt32()}"));
            Assert.Equal(Rows(ta), Rows(tb));
        }

        // diário e duelos na mesa
        var mesa = await Json_(await beto.GetAsync($"/api/mesas/{mesaId}"));
        Assert.Contains(mesa.GetProperty("feed").EnumerateArray(), f => f.GetProperty("text").GetString()!.Contains("Ana") && f.GetProperty("text").GetString()!.Contains("Beto"));
        Assert.Contains(mesa.GetProperty("duels").EnumerateArray(), d => d.GetProperty("kind").GetString() == "league" && d.GetProperty("finished").GetBoolean());

        // amistoso a qualquer hora
        var betoId = mesa.GetProperty("members").EnumerateArray().First(m => m.GetProperty("name").GetString() == "Beto").GetProperty("memberId").GetString();
        var friendly = await ana.PostAsJsonAsync($"/api/mesas/{mesaId}/duels", new { opponentMemberId = betoId });
        Assert.Equal(HttpStatusCode.Created, friendly.StatusCode);
        var fid = (await Json_(friendly)).GetProperty("id").GetString()!;
        Assert.True(await PlayDuel(ana, beto, fid));
        var outsider = Client("player-mesa-outsider");
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/duels/{fid}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/mesas/{mesaId}")).StatusCode);
    }

    /// <summary>Uma ação de bot na carreira. Nulo = a mesa mandou esperar o amigo.</summary>
    [MongoFact]
    public async Task Avancar_junto_so_passa_a_data_quando_todos_apertam_pronto()
    {
        var ana = Client("player-turno-ana");
        var beto = Client("player-turno-beto");
        var created = await Json_(await ana.PostAsJsonAsync("/api/mesas", new { name = "Turno", playerName = "Ana" }));
        var mesaId = created.GetProperty("id").GetString()!;
        await beto.PostAsJsonAsync("/api/mesas/join", new { code = created.GetProperty("code").GetString(), playerName = "Beto" });
        // só quem criou muda o modo
        Assert.Equal(HttpStatusCode.Forbidden, (await beto.PostAsJsonAsync($"/api/mesas/{mesaId}/sync", new { sync = true, mode = "turno" })).StatusCode);
        var turno = await Json_(await ana.PostAsJsonAsync($"/api/mesas/{mesaId}/sync", new { sync = true, mode = "turno" }));
        Assert.Equal("turno", turno.GetProperty("mode").GetString());

        var ids = new[]
        {
            (await Json_(await ana.PostAsJsonAsync($"/api/mesas/{mesaId}/careers", new { name = "Ana", club = "Palmeiras", nation = "Brasil", position = "Atacante" }))).GetProperty("id").GetString()!,
            (await Json_(await beto.PostAsJsonAsync($"/api/mesas/{mesaId}/careers", new { name = "Beto", club = "Santos", nation = "Brasil", position = "Meia" }))).GetProperty("id").GetString()!
        };
        var clients = new[] { ana, beto };
        var rng = new Random(9);
        async Task<JsonElement> View(int who) => await Json_(await clients[who].GetAsync($"/api/careers/{ids[who]}"));
        static int Pace(JsonElement v)
        {
            var me = v.GetProperty("mesa").GetProperty("friends").EnumerateArray().First(f => f.GetProperty("me").GetBoolean());
            return me.GetProperty("season").GetInt32() * 100 + me.GetProperty("seasonDay").GetInt32();
        }

        var turns = 0;
        for (var round = 0; round < 400 && turns < 8; round++)
        {
            // cada um resolve o dia até voltar ao calendário
            for (var who = 0; who < 2; who++)
            {
                for (var i = 0; i < 40; i++)
                {
                    var v = await View(who);
                    if (v.GetProperty("phase").GetString() == "Calendar" || v.GetProperty("status").GetString() == "Finished") break;
                    if (await Act(clients[who], ids[who], v, rng) == null) break;
                }
            }
            var a = await View(0);
            var b = await View(1);
            if (a.GetProperty("phase").GetString() != "Calendar" || b.GetProperty("phase").GetString() != "Calendar") continue;
            Assert.Equal(Pace(a), Pace(b));
            var start = Pace(a);

            if (turns % 2 == 1)
            {
                // os dois apertam Pronto ao mesmo tempo: a data passa uma vez só para cada um
                var va = a.GetProperty("version").GetInt32();
                var vb = b.GetProperty("version").GetInt32();
                await Task.WhenAll(
                    ana.PostAsJsonAsync($"/api/careers/{ids[0]}/actions", new { type = "Continue", version = va }),
                    beto.PostAsJsonAsync($"/api/careers/{ids[1]}/actions", new { type = "Continue", version = vb }));
                var pa = await View(0);
                var pb = await View(1);
                Assert.Equal(va + 1, pa.GetProperty("version").GetInt32());
                Assert.Equal(vb + 1, pb.GetProperty("version").GetInt32());
                Assert.Equal(Pace(pa), Pace(pb));
                Assert.True(Pace(pa) > start);
                turns++;
                continue;
            }

            // Ana aperta Pronto: fica esperando o Beto, e a data não passa (nem apertando de novo)
            var r1 = await Json_(await ana.PostAsJsonAsync($"/api/careers/{ids[0]}/actions", new { type = "Continue", version = a.GetProperty("version").GetInt32() }));
            Assert.Equal(start, Pace(r1));
            Assert.True(r1.GetProperty("mesa").GetProperty("ready").GetBoolean());
            Assert.Equal("Beto", r1.GetProperty("mesa").GetProperty("waitingFor")[0].GetString());
            var again = await Json_(await ana.PostAsJsonAsync($"/api/careers/{ids[0]}/actions", new { type = "Continue", version = r1.GetProperty("version").GetInt32() }));
            Assert.Equal(start, Pace(again));
            Assert.Equal(start, Pace(await View(1)));

            // Beto aperta Pronto: a data passa para os dois de uma vez
            var r2 = await Json_(await beto.PostAsJsonAsync($"/api/careers/{ids[1]}/actions", new { type = "Continue", version = b.GetProperty("version").GetInt32() }));
            var a2 = await View(0);
            Assert.True(Pace(a2) > start, "a data da Ana também passou");
            Assert.True(Pace(r2) > start, "a data do Beto passou");
            Assert.Equal(Pace(a2), Pace(r2));
            Assert.False(a2.GetProperty("mesa").GetProperty("ready").GetBoolean());
            turns++;
        }
        Assert.True(turns >= 8, $"turnos jogados: {turns}");
        var mesa = await Json_(await ana.GetAsync($"/api/mesas/{mesaId}"));
        Assert.Contains(mesa.GetProperty("feed").EnumerateArray(), f => f.GetProperty("text").GetString()!.StartsWith("Todos prontos"));
    }

    private static async Task<JsonElement?> Act(HttpClient http, string id, JsonElement v, Random rng)
    {
        if (v.GetProperty("status").GetString() == "Finished") return null;
        var phase = v.GetProperty("phase").GetString();
        var version = v.GetProperty("version").GetInt32();
        object action = phase switch
        {
            // o bot nunca escolhe parar a carreira (a última opção do veredito médico)
            "Event" => new { type = "Choose", index = v.GetProperty("event").GetProperty("id").GetString() == "vd" ? 0 : rng.Next(v.GetProperty("event").GetProperty("options").GetArrayLength()), version },
            "Penalty" => new { type = "Choose", index = 3, version, power = 75 },
            "Market" => new { type = "Stay", version },
            "Swap" => new { type = "Choose", index = 1, version },
            "Decision" => new { type = "Continue", version },
            _ => new { type = "Continue", version }
        };
        var r = await http.PostAsJsonAsync($"/api/careers/{id}/actions", action);
        if (r.StatusCode == HttpStatusCode.BadRequest)
        {
            var text = await r.Content.ReadAsStringAsync();
            Assert.True(text.Contains("Esperando") || text.Contains("duelo"), text);
            return null;
        }
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await Json_(r);
    }

    public void Dispose()
    {
        if (_factory == null) return;
        _factory.Dispose();
        new MongoClient(MongoFactAttribute.Url).DropDatabase(_db);
    }
}
