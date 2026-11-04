using System.Text.RegularExpressions;
using Camisa10.Api.Data;
using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Camisa10.Api.Endpoints;

public static partial class CareerEndpoints
{
    /// <summary>Cabeçalho com o identificador do jogador (gerado e guardado pelo app).</summary>
    public const string PlayerHeader = "X-Player-Id";

    [GeneratedRegex("^[A-Za-z0-9_-]{8,64}$")]
    private static partial Regex PlayerIdRx();

    public static void MapCareerEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/meta", () => TypedResults.Ok(ViewBuilder.Meta()))
            .WithSummary("Clubes em destaque, seleções e posições para a tela inicial.");

        var world = api.MapGroup("/world");
        world.MapGet("/", () => TypedResults.Ok(ViewBuilder.Countries()))
            .WithSummary("Os 211 países com as suas três divisões.");
        world.MapGet("/search", (string? q) => TypedResults.Ok(ViewBuilder.SearchClubs(q ?? "")))
            .WithSummary("Busca clubes do mundo pelo nome.");
        world.MapGet("/{iso}", Results<Ok<CountryView>, NotFound> (string iso) =>
                ViewBuilder.Country(iso) is { } c ? TypedResults.Ok(c) : TypedResults.NotFound())
            .WithSummary("Clubes das divisões A, B e C de um país.");

        var careers = api.MapGroup("/careers").AddEndpointFilter(RequirePlayer);

        careers.MapGet("/", ListCareers).WithSummary("Carreiras do jogador (em andamento e encerradas).");
        careers.MapPost("/", CreateCareer).WithSummary("Começa uma nova carreira na base.");
        careers.MapGet("/{id}", GetCareer).WithSummary("Tela atual da carreira.");
        careers.MapPost("/{id}/actions", Act).WithSummary("Executa uma ação do jogador e devolve a nova tela.");
        careers.MapGet("/{id}/history", GetHistory).WithSummary("Evolução da carreira temporada a temporada.");
        careers.MapDelete("/{id}", DeleteCareer).WithSummary("Apaga uma carreira.");
    }

    private const string PlayerItem = "c10.player";

    public static bool ValidPlayerId(string id) => PlayerIdRx().IsMatch(id);

    /// <summary>
    /// Descobre o jogador: pela sessão da conta (Authorization: Bearer) ou, para convidados, pelo id do
    /// aparelho (X-Player-Id). Um id que já é de uma conta só vale com a sessão dela.
    /// </summary>
    internal static async ValueTask<object?> RequirePlayer(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var db = http.RequestServices.GetRequiredService<Camisa10DbContext>();
        if (AuthEndpoints.BearerToken(http) is { } token)
        {
            var session = await AuthEndpoints.Session(db, token, http.RequestAborted);
            if (session == null)
                return TypedResults.Problem("Sua sessão expirou. Entre de novo na sua conta.", statusCode: StatusCodes.Status401Unauthorized);
            http.Items[PlayerItem] = session.PlayerId;
            return await next(ctx);
        }
        var id = http.Request.Headers[PlayerHeader].ToString();
        if (!ValidPlayerId(id))
            return TypedResults.Problem($"Envie o cabeçalho {PlayerHeader} com 8 a 64 caracteres (letras, números, - ou _).",
                statusCode: StatusCodes.Status400BadRequest);
        if (await db.Accounts.AsNoTracking().AnyAsync(a => a.PlayerId == id, http.RequestAborted))
            return TypedResults.Problem("Estas carreiras são de uma conta. Entre com o seu e-mail e senha.", statusCode: StatusCodes.Status401Unauthorized);
        http.Items[PlayerItem] = id;
        return await next(ctx);
    }

    /// <summary>Jogador da requisição (definido pelo <see cref="RequirePlayer"/>).</summary>
    internal static string Player(HttpContext http) => (string)http.Items[PlayerItem]!;

    private static async Task<Ok<List<CareerSummary>>> ListCareers(HttpContext http, Camisa10DbContext db, string? status, CancellationToken ct)
    {
        var player = Player(http);
        var query = db.Careers.AsNoTracking().Where(c => c.PlayerId == player);
        if (Enum.TryParse<CareerStatus>(status, true, out var st)) query = query.Where(c => c.Status == st);
        var list = await query.OrderByDescending(c => c.UpdatedAt).Take(20).ToListAsync(ct);
        return TypedResults.Ok(list.Select(c => new CareerSummary(c.Id, c.Name, c.Age, c.Club, c.Position, c.Status.ToString(), c.FinalTitle, c.UpdatedAt)).ToList());
    }

    private static async Task<Results<Created<GameView>, ProblemHttpResult>> CreateCareer(
        HttpContext http, NewCareerRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        Career career;
        try
        {
            career = GameEngine.NewCareer(Player(http), req.Name ?? "", req.Club, req.Nation, req.Position);
        }
        catch (GameRuleException e)
        {
            return TypedResults.Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        db.Careers.Add(career);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/careers/{career.Id}", ViewBuilder.Build(career));
    }

    private static async Task<Results<Ok<GameView>, NotFound>> GetCareer(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct)
    {
        var career = await Find(db, http, id, ct, tracking: false);
        if (career == null) return TypedResults.NotFound();
        var engine = new GameEngine(career);
        engine.EnsureCalendar(); // só para a tela; é gravado na próxima ação
        if (career.MesaId != null && await db.Mesas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == career.MesaId, ct) is { } mesa)
        {
            // a tabela já aparece com os placares reais dos amigos
            engine.Mesa = MesaService.Context(mesa, await MesaService.Friends(db, mesa, career.Id, ct));
            engine.SyncMesa();
        }
        return TypedResults.Ok(await WithMesa(db, career, ViewBuilder.Build(career), ct));
    }

    private static async Task<Results<Ok<GameView>, NotFound, ProblemHttpResult, Conflict<GameView>>> Act(
        HttpContext http, string id, ActionRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var career = await Find(db, http, id, ct, tracking: true);
        if (career == null) return TypedResults.NotFound();

        // O app mostrava uma tela antiga (ex.: toque duplo): devolve a tela atual sem aplicar nada.
        if (req.Version is { } v && v != career.Version) return TypedResults.Conflict(await WithMesa(db, career, ViewBuilder.Build(career), ct));

        // Mesa: amigos (para os duelos) e o calendário junto
        var mesa = career.MesaId == null ? null : await db.Mesas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == career.MesaId, ct);
        var friends = mesa == null ? new() : await MesaService.Friends(db, mesa, career.Id, ct);
        if (mesa != null && MesaService.Advances(career, req.Type) && MesaService.WaitingFor(mesa, career, friends) is { Count: > 0 } waiting)
            return TypedResults.Problem($"Esperando {string.Join(", ", waiting)} chegar nesta data para a mesa seguir junta.",
                statusCode: StatusCodes.Status400BadRequest);

        var before = MesaService.Snap(career);
        var engine = new GameEngine(career) { Mesa = mesa == null ? null : MesaService.Context(mesa, friends) };
        // acerta a tabela e o mundo com os resultados reais dos amigos
        engine.SyncMesa();
        try
        {
            if (career.Phase == GamePhase.Duel && req.Type == ActionType.Continue && career.PendingDuel?.DuelId is { } duelId)
            {
                // fim do duelo da Mesa: o placar vale para a carreira
                if (mesa != null) await MesaService.EnsureLeagueDuel(db, career, mesa, ct);
                var duel = await db.Duels.AsNoTracking().FirstOrDefaultAsync(d => d.Id == duelId, ct);
                if (duel == null || !duel.Finished)
                    return TypedResults.Problem("O duelo ainda não acabou. Jogue os lances com o seu amigo.", statusCode: StatusCodes.Status400BadRequest);
                var side = duel.Home.CareerId == career.Id ? "home" : "away";
                engine.FinishDuel(new DuelEngine(duel).OutcomeFor(side));
            }
            else engine.Act(new GameAction(req.Type, req.Index, req.Power));
        }
        catch (GameRuleException e)
        {
            return TypedResults.Problem(e.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        career.Version++;
        career.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var fresh = await Find(db, http, id, ct, tracking: false);
            return TypedResults.Conflict(ViewBuilder.Build(fresh!));
        }
        if (mesa != null)
        {
            await MesaService.Share(db, mesa.Id, career, ct);
            if (career.PendingDuel != null) await MesaService.EnsureLeagueDuel(db, career, mesa, ct);
            var name = mesa.Members.FirstOrDefault(m => m.CareerId == career.Id)?.Name ?? career.Name;
            foreach (var (icon, text) in MesaService.News(name, before, career)) await MesaService.Post(db, mesa.Id, icon, text, ct);
        }
        return TypedResults.Ok(await WithMesa(db, career, ViewBuilder.Build(career), ct));
    }

    private static async Task<Results<Ok<HistoryView>, NotFound>> GetHistory(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct)
    {
        var career = await Find(db, http, id, ct, tracking: false);
        return career == null ? TypedResults.NotFound() : TypedResults.Ok(ViewBuilder.History(career));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCareer(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct)
    {
        var career = await Find(db, http, id, ct, tracking: true);
        if (career == null) return TypedResults.NotFound();
        db.Careers.Remove(career);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>Junta à tela a situação da mesa (amigos e quem falta chegar na data).</summary>
    private static async Task<GameView> WithMesa(Camisa10DbContext db, Career career, GameView view, CancellationToken ct)
    {
        if (career.MesaId == null) return view;
        var mesa = await db.Mesas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == career.MesaId, ct);
        if (mesa == null) return view;
        if (career.Phase == GamePhase.Duel) await MesaService.EnsureLeagueDuel(db, career, mesa, ct);
        var friends = await MesaService.Friends(db, mesa, career.Id, ct);
        return view with { Mesa = MesaService.Status(mesa, career, friends) };
    }

    private static Task<Career?> Find(Camisa10DbContext db, HttpContext http, string id, CancellationToken ct, bool tracking)
    {
        var player = Player(http);
        var q = tracking ? db.Careers : db.Careers.AsNoTracking();
        return q.FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == player, ct);
    }
}
