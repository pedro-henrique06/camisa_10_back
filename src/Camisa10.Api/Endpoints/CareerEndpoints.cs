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

    private static async ValueTask<object?> RequirePlayer(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var id = ctx.HttpContext.Request.Headers[PlayerHeader].ToString();
        if (!PlayerIdRx().IsMatch(id))
            return TypedResults.Problem($"Envie o cabeçalho {PlayerHeader} com 8 a 64 caracteres (letras, números, - ou _).",
                statusCode: StatusCodes.Status400BadRequest);
        return await next(ctx);
    }

    private static string Player(HttpContext http) => http.Request.Headers[PlayerHeader].ToString();

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
        new GameEngine(career).EnsureCalendar(); // só para a tela; é gravado na próxima ação
        return TypedResults.Ok(ViewBuilder.Build(career));
    }

    private static async Task<Results<Ok<GameView>, NotFound, ProblemHttpResult, Conflict<GameView>>> Act(
        HttpContext http, string id, ActionRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var career = await Find(db, http, id, ct, tracking: true);
        if (career == null) return TypedResults.NotFound();

        // O app mostrava uma tela antiga (ex.: toque duplo): devolve a tela atual sem aplicar nada.
        if (req.Version is { } v && v != career.Version) return TypedResults.Conflict(ViewBuilder.Build(career));

        try
        {
            new GameEngine(career).Act(new GameAction(req.Type, req.Index, req.Power));
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
        return TypedResults.Ok(ViewBuilder.Build(career));
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

    private static Task<Career?> Find(Camisa10DbContext db, HttpContext http, string id, CancellationToken ct, bool tracking)
    {
        var player = Player(http);
        var q = tracking ? db.Careers : db.Careers.AsNoTracking();
        return q.FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == player, ct);
    }
}
