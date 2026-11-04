using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Camisa10.Api.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;

namespace Camisa10.Tests;

public class PasswordTests
{
    [Fact]
    public void Senha_confere_so_com_a_senha_certa()
    {
        var stored = AuthEndpoints.Hash("camisa10");
        Assert.StartsWith("pbkdf2$", stored);
        Assert.DoesNotContain("camisa10", stored);
        Assert.True(AuthEndpoints.Verify("camisa10", stored));
        Assert.False(AuthEndpoints.Verify("camisa11", stored));
        Assert.False(AuthEndpoints.Verify("camisa10", "lixo"));
        // o mesmo texto gera hashes diferentes (sal aleatório)
        Assert.NotEqual(stored, AuthEndpoints.Hash("camisa10"));
    }
}

public class AuthIntegrationTests : IDisposable
{
    private readonly string _db = "camisa10_auth_" + Guid.NewGuid().ToString("N")[..8];
    private readonly WebApplicationFactory<Program>? _factory;

    public AuthIntegrationTests()
    {
        if (MongoFactAttribute.Url == null) return;
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ConnectionStrings:Mongo", MongoFactAttribute.Url);
            b.UseSetting("Mongo:Database", _db);
        });
    }

    private HttpClient Guest(string player)
    {
        var c = _factory!.CreateClient();
        c.DefaultRequestHeaders.Add("X-Player-Id", player);
        return c;
    }

    private HttpClient WithToken(string token, string? player = null)
    {
        var c = player == null ? _factory!.CreateClient() : Guest(player);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private static async Task<JsonElement> Json_(HttpResponseMessage r) => JsonSerializer.Deserialize<JsonElement>(await r.Content.ReadAsStringAsync());

    private static Task<HttpResponseMessage> NewCareer(HttpClient http, string name) =>
        http.PostAsJsonAsync("/api/careers", new { name, club = "Santos", nation = "Brasil", position = "Atacante" });

    [MongoFact]
    public async Task Conta_leva_as_carreiras_para_outro_aparelho_e_protege_o_id_do_convidado()
    {
        // celular 1: joga como convidado e cria a conta (a carreira passa a ser da conta)
        var phone1 = Guest("p-celular-um-123456");
        Assert.Equal(HttpStatusCode.Created, (await NewCareer(phone1, "Pedro")).StatusCode);
        var reg = await phone1.PostAsJsonAsync("/api/auth/register", new { email = "Pedro@Exemplo.com ", password = "segredo1", name = "Pedro" });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var regBody = await Json_(reg);
        Assert.Equal(1, regBody.GetProperty("imported").GetInt32());
        Assert.Equal("pedro@exemplo.com", regBody.GetProperty("account").GetProperty("email").GetString());
        var token1 = regBody.GetProperty("token").GetString()!;

        // o id do convidado sozinho não abre mais as carreiras
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone1.GetAsync("/api/careers")).StatusCode);
        var list = await Json_(await WithToken(token1).GetAsync("/api/careers"));
        Assert.Equal(1, list.GetArrayLength());

        // e-mail repetido, senha errada
        Assert.Equal(HttpStatusCode.Conflict, (await Guest("p-outro-aparelho-1").PostAsJsonAsync("/api/auth/register",
            new { email = "pedro@exemplo.com", password = "outra123", name = "Outro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory!.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "pedro@exemplo.com", password = "errada" })).StatusCode);

        // celular 2: tinha uma carreira de convidado; ao entrar, ela vem junto
        var phone2 = Guest("p-celular-dois-1234");
        Assert.Equal(HttpStatusCode.Created, (await NewCareer(phone2, "Pedro 2")).StatusCode);
        var login = await phone2.PostAsJsonAsync("/api/auth/login", new { email = "pedro@exemplo.com", password = "segredo1" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await Json_(login);
        Assert.Equal(1, loginBody.GetProperty("imported").GetInt32());
        var token2 = loginBody.GetProperty("token").GetString()!;
        var both = await Json_(await WithToken(token2).GetAsync("/api/careers"));
        Assert.Equal(2, both.GetArrayLength());

        var me = await Json_(await WithToken(token2).GetAsync("/api/auth/me"));
        Assert.Equal("Pedro", me.GetProperty("name").GetString());

        // sair: o token deixa de valer (o do celular 1 continua)
        Assert.Equal(HttpStatusCode.NoContent, (await WithToken(token2).PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await WithToken(token2).GetAsync("/api/careers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await WithToken(token1).GetAsync("/api/careers")).StatusCode);
    }

    [MongoFact]
    public async Task Mesa_segue_a_conta_e_nao_mostra_o_id_dos_amigos()
    {
        var guest = Guest("p-mesa-convidado-1");
        var mesa = await Json_(await guest.PostAsJsonAsync("/api/mesas", new { name = "Mesa", playerName = "Ana" }));
        var mesaId = mesa.GetProperty("id").GetString()!;
        var member = mesa.GetProperty("members")[0];
        Assert.False(member.TryGetProperty("playerId", out _));
        Assert.NotEqual("p-mesa-convidado-1", member.GetProperty("memberId").GetString());

        // a conta é criada em outro aparelho; ao entrar neste, a mesa do convidado vem para a conta
        var reg = await Json_(await Guest("p-mesa-celular-novo").PostAsJsonAsync("/api/auth/register", new { email = "ana@x.com", password = "123456", name = "Ana" }));
        var login = await Json_(await guest.PostAsJsonAsync("/api/auth/login", new { email = "ana@x.com", password = "123456" }));
        var token = login.GetProperty("token").GetString()!;
        var view = await Json_(await WithToken(token).GetAsync($"/api/mesas/{mesaId}"));
        Assert.True(view.GetProperty("isOwner").GetBoolean());
        Assert.True(view.GetProperty("members")[0].GetProperty("me").GetBoolean());
        Assert.Equal(1, (await Json_(await WithToken(reg.GetProperty("token").GetString()!).GetAsync("/api/mesas"))).GetArrayLength());
    }

    public void Dispose()
    {
        if (_factory == null) return;
        _factory.Dispose();
        new MongoClient(MongoFactAttribute.Url).DropDatabase(_db);
    }
}
