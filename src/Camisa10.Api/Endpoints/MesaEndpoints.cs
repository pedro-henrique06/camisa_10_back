using Camisa10.Api.Data;
using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Camisa10.Api.Endpoints;

public record NewMesaRequest(string Name, string PlayerName);
public record JoinMesaRequest(string Code, string PlayerName);
public record SyncRequest(bool Sync);
public record ChallengeRequest(string OpponentPlayerId);
public record DuelChoiceRequest(int Index);

/// <summary>Mesa vista por um amigo: participantes, carreiras, diário e duelos.</summary>
public record MesaView(string Id, string Code, string Name, bool Sync, bool IsOwner, IReadOnlyList<MesaMemberView> Members,
    IReadOnlyList<MesaFeedItem> Feed, IReadOnlyList<DuelSummary> Duels);

public record MesaMemberView(string PlayerId, string Name, bool Me, string? CareerId, MesaFriendView? Career, Badge? Badge, int Titles, int Goals);

public record DuelSummary(string Id, string Kind, string Competition, string Home, string HomeClub, string Away, string AwayClub,
    int HomeGoals, int AwayGoals, bool Finished, bool Mine, bool MyTurn, DateTime CreatedAt);

public record MesaSummary(string Id, string Code, string Name, int Members, DateTime UpdatedAt);

public static class MesaEndpoints
{
    public static void MapMesaEndpoints(this IEndpointRouteBuilder app)
    {
        var mesas = app.MapGroup("/api/mesas").AddEndpointFilter(CareerEndpoints.RequirePlayer);
        mesas.MapGet("/", List).WithSummary("Mesas de que o jogador participa.");
        mesas.MapPost("/", Create).WithSummary("Cria uma mesa e devolve o código para os amigos.");
        mesas.MapPost("/join", Join).WithSummary("Entra numa mesa pelo código.");
        mesas.MapGet("/{id}", Get).WithSummary("Participantes, carreiras, diário e duelos da mesa.");
        mesas.MapPost("/{id}/careers", CreateCareer).WithSummary("Começa a carreira do jogador nesta mesa (mesmo mundo dos amigos).");
        mesas.MapPost("/{id}/sync", SetSync).WithSummary("Liga ou desliga o calendário junto (só quem criou a mesa).");
        mesas.MapPost("/{id}/duels", Challenge).WithSummary("Desafia um amigo para um duelo amistoso.");

        var duels = app.MapGroup("/api/duels").AddEndpointFilter(CareerEndpoints.RequirePlayer);
        duels.MapGet("/{id}", GetDuel).WithSummary("Duelo visto pelo jogador: placar, lance atual e opções.");
        duels.MapPost("/{id}/choose", Choose).WithSummary("Escolhe a jogada do lance atual (ataque ou defesa).");
        duels.MapPost("/{id}/auto", Auto).WithSummary("Decide pelo amigo que demorou demais no lance.");
    }

    private static string Player(HttpContext http) => http.Request.Headers[CareerEndpoints.PlayerHeader].ToString();

    private static async Task<Ok<List<MesaSummary>>> List(HttpContext http, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var list = await db.Mesas.AsNoTracking().Where(m => m.MemberIds.Contains(me)).OrderByDescending(m => m.UpdatedAt).Take(20).ToListAsync(ct);
        return TypedResults.Ok(list.Select(m => new MesaSummary(m.Id, m.Code, m.Name, m.Members.Count, m.UpdatedAt)).ToList());
    }

    private static string Clean(string? text, string fallback, int max) =>
        string.IsNullOrWhiteSpace(text) ? fallback : text.Trim().Length > max ? text.Trim()[..max] : text.Trim();

    private static async Task<Results<Created<MesaView>, ProblemHttpResult>> Create(HttpContext http, NewMesaRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var name = Clean(req.PlayerName, "Jogador", 24);
        string code;
        do code = MesaService.NewCode();
        while (await db.Mesas.AsNoTracking().AnyAsync(m => m.Code == code, ct));
        var mesa = new Mesa
        {
            Code = code, Name = Clean(req.Name, "Mesa dos amigos", 40), OwnerPlayerId = me, WorldSeed = Random.Shared.Next(1, int.MaxValue),
            Members = { new MesaMember { PlayerId = me, Name = name } }, MemberIds = { me },
            Feed = { new MesaFeedItem { Icon = "🎲", Text = $"{name} abriu a mesa. Código: {code}" } }
        };
        db.Mesas.Add(mesa);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/mesas/{mesa.Id}", await View(db, mesa, me, ct));
    }

    private static async Task<Results<Ok<MesaView>, ProblemHttpResult>> Join(HttpContext http, JoinMesaRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var code = (req.Code ?? "").Trim().ToUpperInvariant();
        var mesa = await db.Mesas.FirstOrDefaultAsync(m => m.Code == code, ct);
        if (mesa == null) return TypedResults.Problem("Não achei nenhuma mesa com esse código.", statusCode: 404);
        if (!mesa.MemberIds.Contains(me))
        {
            if (mesa.Members.Count >= Mesa.MaxMembers) return TypedResults.Problem($"A mesa já tem {Mesa.MaxMembers} jogadores.", statusCode: 400);
            var name = Clean(req.PlayerName, "Jogador", 24);
            mesa.Members.Add(new MesaMember { PlayerId = me, Name = name });
            mesa.MemberIds.Add(me);
            mesa.Feed.Add(new MesaFeedItem { Icon = "👋", Text = $"{name} entrou na mesa." });
            mesa.Version++;
            mesa.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return TypedResults.Ok(await View(db, mesa, me, ct));
    }

    private static async Task<Results<Ok<MesaView>, NotFound>> Get(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var mesa = await db.Mesas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mesa == null || !mesa.MemberIds.Contains(me)) return TypedResults.NotFound();
        return TypedResults.Ok(await View(db, mesa, me, ct));
    }

    private static async Task<Results<Created<GameView>, ProblemHttpResult, NotFound>> CreateCareer(
        HttpContext http, string id, NewCareerRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var mesa = await db.Mesas.FirstOrDefaultAsync(m => m.Id == id, ct);
        var member = mesa?.Members.FirstOrDefault(m => m.PlayerId == me);
        if (mesa == null || member == null) return TypedResults.NotFound();
        if (member.CareerId != null && await db.Careers.AsNoTracking().AnyAsync(c => c.Id == member.CareerId && c.Status == CareerStatus.Active, ct))
            return TypedResults.Problem("Você já tem uma carreira em andamento nesta mesa.", statusCode: 400);
        Career career;
        try
        {
            career = GameEngine.NewCareer(me, req.Name ?? member.Name, req.Club, req.Nation, req.Position);
        }
        catch (GameRuleException e)
        {
            return TypedResults.Problem(e.Message, statusCode: 400);
        }
        // mesmo mundo dos amigos
        career.WorldSeed = mesa.WorldSeed;
        career.MesaId = mesa.Id;
        // um documento por gravação (o MongoDB sem réplica não aceita transação): primeiro a carreira, depois a mesa
        db.Entry(mesa).State = EntityState.Detached;
        db.Careers.Add(career);
        await db.SaveChangesAsync(ct);
        db.Entry(career).State = EntityState.Detached;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var m = await db.Mesas.FirstAsync(x => x.Id == id, ct);
            var mem = m.Members.First(x => x.PlayerId == me);
            mem.CareerId = career.Id;
            m.Feed.Add(new MesaFeedItem { Icon = "⚽", Text = $"{mem.Name} começou a carreira na base do {GameEngine.CleanClub(career.Club)} ({career.Position})." });
            m.Version++;
            m.UpdatedAt = DateTime.UtcNow;
            try { await db.SaveChangesAsync(ct); break; }
            catch (DbUpdateConcurrencyException) { db.Entry(m).State = EntityState.Detached; }
        }
        return TypedResults.Created($"/api/careers/{career.Id}", ViewBuilder.Build(career));
    }

    private static async Task<Results<Ok<MesaView>, ProblemHttpResult, NotFound>> SetSync(HttpContext http, string id, SyncRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var mesa = await db.Mesas.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mesa == null || !mesa.MemberIds.Contains(me)) return TypedResults.NotFound();
        if (mesa.OwnerPlayerId != me) return TypedResults.Problem("Só quem criou a mesa muda o ritmo.", statusCode: 403);
        mesa.Sync = req.Sync;
        mesa.Feed.Add(new MesaFeedItem { Icon = "⏱", Text = req.Sync ? "A mesa voltou a jogar com o calendário junto." : "A mesa agora joga no ritmo de cada um." });
        mesa.Version++;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await View(db, mesa, me, ct));
    }

    private static async Task<Results<Created<DuelView>, ProblemHttpResult, NotFound>> Challenge(
        HttpContext http, string id, ChallengeRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var mesa = await db.Mesas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mesa == null || !mesa.MemberIds.Contains(me)) return TypedResults.NotFound();
        var a = mesa.Members.First(m => m.PlayerId == me);
        var b = mesa.Members.FirstOrDefault(m => m.PlayerId == req.OpponentPlayerId);
        if (b == null || b.PlayerId == me) return TypedResults.Problem("Escolha um amigo da mesa.", statusCode: 400);
        if (a.CareerId == null || b.CareerId == null) return TypedResults.Problem("Os dois precisam ter uma carreira na mesa.", statusCode: 400);
        var ca = await db.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == a.CareerId, ct);
        var cb = await db.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == b.CareerId, ct);
        if (ca == null || cb == null) return TypedResults.Problem("Carreira não encontrada.", statusCode: 400);
        var open = await db.Duels.AsNoTracking().AnyAsync(d => d.MesaId == mesa.Id && !d.Finished && d.Kind == "friendly" &&
            ((d.Home.PlayerId == me && d.Away.PlayerId == b.PlayerId) || (d.Home.PlayerId == b.PlayerId && d.Away.PlayerId == me)), ct);
        if (open) return TypedResults.Problem($"Já existe um amistoso em andamento com {b.Name}.", statusCode: 400);
        var duel = new Duel
        {
            Id = "f" + Guid.NewGuid().ToString("N")[..16], MesaId = mesa.Id, Kind = "friendly", Competition = "Amistoso da Mesa",
            Season = ca.Season, Turn = ca.SeasonDay, Home = MesaService.Side(ca, a.Name), Away = MesaService.Side(cb, b.Name)
        };
        DuelEngine.Setup(duel);
        db.Duels.Add(duel);
        await db.SaveChangesAsync(ct);
        await MesaService.Post(db, mesa.Id, "⚔️", $"{a.Name} desafiou {b.Name} para um amistoso.", ct);
        return TypedResults.Created($"/api/duels/{duel.Id}", DuelViews.Build(duel, "home", DateTime.UtcNow));
    }

    private static async Task<MesaView> View(Camisa10DbContext db, Mesa mesa, string me, CancellationToken ct)
    {
        var ids = mesa.Members.Where(m => m.CareerId != null).Select(m => m.CareerId!).ToList();
        var careers = ids.Count == 0 ? new List<Career>() : await db.Careers.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        var members = mesa.Members.Select(m =>
        {
            var c = careers.FirstOrDefault(x => x.Id == m.CareerId);
            return new MesaMemberView(m.PlayerId, m.Name, m.PlayerId == me, m.CareerId,
                c == null ? null : MesaService.Friend(m, c, m.PlayerId == me), c == null ? null : ViewBuilder.BadgeFor(c.Club),
                c?.Titles ?? 0, c == null ? 0 : c.Goals + c.SeasonMatches.Sum(x => x.Goals));
        }).ToList();
        var duels = await db.Duels.AsNoTracking().Where(d => d.MesaId == mesa.Id).OrderByDescending(d => d.CreatedAt).Take(20).ToListAsync(ct);
        var summaries = duels.Select(d =>
        {
            var side = d.Home.PlayerId == me ? "home" : d.Away.PlayerId == me ? "away" : null;
            var myTurn = false;
            if (side != null && !d.Finished)
            {
                var m = d.Moments[d.Current];
                myTurn = m.Attacker == side ? m.AttackChoice == null : m.DefenseChoice == null;
            }
            return new DuelSummary(d.Id, d.Kind, d.Competition, d.Home.Name, d.Home.Club, d.Away.Name, d.Away.Club,
                d.Home.Goals, d.Away.Goals, d.Finished, side != null, myTurn, d.CreatedAt);
        }).ToList();
        var feed = mesa.Feed.AsEnumerable().Reverse().Take(30).ToList();
        return new MesaView(mesa.Id, mesa.Code, mesa.Name, mesa.Sync, mesa.OwnerPlayerId == me, members, feed, summaries);
    }

    // ---------------------------------------------------------------- duelos
    private static async Task<Results<Ok<DuelView>, NotFound>> GetDuel(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct)
    {
        var me = Player(http);
        var duel = await db.Duels.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        var side = duel == null ? null : new DuelEngine(duel).SideOf(me);
        return duel == null || side == null ? TypedResults.NotFound() : TypedResults.Ok(DuelViews.Build(duel, side, DateTime.UtcNow));
    }

    private static Task<Results<Ok<DuelView>, NotFound, ProblemHttpResult>> Choose(HttpContext http, string id, DuelChoiceRequest req, Camisa10DbContext db, CancellationToken ct) =>
        Play(http, id, db, ct, (engine, side) => engine.Choose(side, req.Index));

    private static Task<Results<Ok<DuelView>, NotFound, ProblemHttpResult>> Auto(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct) =>
        Play(http, id, db, ct, (engine, side) => engine.AutoPick(side, DateTime.UtcNow));

    /// <summary>Aplica a jogada no duelo (com nova tentativa se o amigo jogou no mesmo instante).</summary>
    private static async Task<Results<Ok<DuelView>, NotFound, ProblemHttpResult>> Play(HttpContext http, string id, Camisa10DbContext db, CancellationToken ct,
        Action<DuelEngine, string> action)
    {
        var me = Player(http);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var duel = await db.Duels.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (duel == null) return TypedResults.NotFound();
            var engine = new DuelEngine(duel);
            var side = engine.SideOf(me);
            if (side == null) return TypedResults.NotFound();
            var wasFinished = duel.Finished;
            try
            {
                action(engine, side);
            }
            catch (GameRuleException e)
            {
                return TypedResults.Problem(e.Message, statusCode: 400);
            }
            duel.Version++;
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.Entry(duel).State = EntityState.Detached;
                continue;
            }
            if (!wasFinished && duel.Finished)
            {
                var (h, a) = (duel.Home, duel.Away);
                var res = h.Goals > a.Goals ? $"vitória de {h.Name}" : h.Goals < a.Goals ? $"vitória de {a.Name}" : "empate";
                await MesaService.Post(db, duel.MesaId, duel.Kind == "league" ? "🏟️" : "⚔️",
                    $"{duel.Competition}: {h.Club} {h.Goals} x {a.Goals} {a.Club} ({h.Name} x {a.Name}), {res}.", ct);
            }
            return TypedResults.Ok(DuelViews.Build(duel, side, DateTime.UtcNow));
        }
        return TypedResults.Problem("O duelo mudou ao mesmo tempo. Tente de novo.", statusCode: 409);
    }
}
