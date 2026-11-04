using Camisa10.Api.Data;
using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace Camisa10.Api.Endpoints;

/// <summary>Regras da mesa que dependem de mais de uma carreira: ritmo junto, duelos e o diário.</summary>
public static class MesaService
{
    private const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Id público do amigo na mesa (não revela o id de jogador, que identifica o convidado).</summary>
    public static string MemberId(string mesaId, string playerId) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(mesaId + "|" + playerId)))[..16].ToLowerInvariant();

    public static string NewCode()
    {
        var r = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => CodeChars[r.Next(CodeChars.Length)]).ToArray());
    }

    /// <summary>
    /// Posição no calendário para comparar quem está na frente (temporada e data). Quem já fechou a
    /// temporada (balanço, promoção, decisão) fica na "data 25", depois da última.
    /// </summary>
    public static int Pace(Career c) =>
        c.Season * 100 + (c.Phase is GamePhase.SeasonSummary or GamePhase.Promotion or GamePhase.Decision ? 25 : Math.Max(0, c.SeasonDay));

    /// <summary>Carreiras dos amigos da mesa (sem a do próprio jogador).</summary>
    public static async Task<List<(MesaMember Member, Career Career)>> Friends(Camisa10DbContext db, Mesa mesa, string careerId, CancellationToken ct)
    {
        var ids = mesa.Members.Where(m => m.CareerId != null && m.CareerId != careerId).Select(m => m.CareerId!).ToList();
        if (ids.Count == 0) return new();
        var careers = await db.Careers.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        return mesa.Members.Where(m => m.CareerId != null && m.CareerId != careerId)
            .Select(m => (m, careers.FirstOrDefault(c => c.Id == m.CareerId)))
            .Where(x => x.Item2 != null).Select(x => (x.m, x.Item2!)).ToList();
    }

    public static MesaContext Context(Mesa mesa, List<(MesaMember Member, Career Career)> friends) =>
        new(mesa.Id, friends.Where(f => f.Career.Status == CareerStatus.Active).Select(f => new MesaFriend(
            f.Career.Id, f.Member.PlayerId, f.Member.Name, f.Career.Base ? f.Career.BaseClubId ?? f.Career.ClubId : f.Career.ClubId,
            f.Career.LeagueTableKey, GameEngine.TableHash(f.Career))).ToList(), mesa.Results, mesa.ClubSeasons);

    /// <summary>
    /// Depois da ação: os placares reais do clube do jogador e a classificação final da liga vão para a mesa,
    /// para as carreiras dos amigos usarem os mesmos resultados.
    /// </summary>
    public static async Task Share(Camisa10DbContext db, string mesaId, Career c, CancellationToken ct)
    {
        var me = c.Base ? c.BaseClubId ?? c.ClubId : c.ClubId;
        var mine = c.LeagueResults.Where(r => r.Home == me || r.Away == me).ToList();
        var seasons = c.ClubSeasons.Where(x => x.ClubId == me).ToList();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var mesa = await db.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId, ct);
            if (mesa == null) return;
            var changed = false;
            foreach (var r in mine)
            {
                var e = mesa.Results.FirstOrDefault(x => x.Season == c.Season && x.Key == c.LeagueTableKey && x.Round == r.Round && x.Home == r.Home && x.Away == r.Away);
                if (e != null) continue;
                mesa.Results.Add(new MesaResult { Season = c.Season, Key = c.LeagueTableKey ?? "", Round = r.Round, Home = r.Home, Away = r.Away, HomeGoals = r.HomeGoals, AwayGoals = r.AwayGoals });
                changed = true;
            }
            foreach (var cs in seasons)
            {
                var e = mesa.ClubSeasons.FirstOrDefault(x => x.Season == cs.Season && x.ClubId == cs.ClubId);
                if (e != null && e.Rank == cs.Rank && e.Order.SequenceEqual(cs.Order)) continue;
                if (e != null) mesa.ClubSeasons.Remove(e);
                mesa.ClubSeasons.Add(new ClubSeasonResult { Season = cs.Season, ClubId = cs.ClubId, Division = cs.Division, Rank = cs.Rank, Size = cs.Size, Order = cs.Order.ToList() });
                changed = true;
            }
            if (!changed) return;
            // só guarda as temporadas recentes
            mesa.Results.RemoveAll(x => x.Season < c.Season - 1);
            mesa.Version++;
            mesa.UpdatedAt = DateTime.UtcNow;
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                db.Entry(mesa).State = EntityState.Detached;
            }
        }
    }

    /// <summary>
    /// Amigos que ainda não chegaram na data do jogador: com a mesa em ritmo junto, ele espera por eles
    /// antes de passar para a próxima data (ou para a próxima temporada).
    /// </summary>
    public static List<string> WaitingFor(Mesa mesa, Career me, List<(MesaMember Member, Career Career)> friends) =>
        !mesa.Sync ? new() : friends.Where(f => f.Career.Status == CareerStatus.Active && Pace(f.Career) < Pace(me))
            .Select(f => f.Member.Name).ToList();

    /// <summary>A ação faz a carreira passar de data (ou de temporada)?</summary>
    public static bool Advances(Career c, ActionType type) =>
        type == ActionType.Continue && c.Phase is GamePhase.Calendar or GamePhase.SeasonSummary or GamePhase.Promotion or GamePhase.Decision;

    public static MesaStatusView Status(Mesa mesa, Career me, List<(MesaMember Member, Career Career)> friends)
    {
        var list = friends.Select(f => Friend(f.Member, f.Career, false)).ToList();
        var mine = mesa.Members.FirstOrDefault(m => m.CareerId == me.Id);
        list.Insert(0, Friend(mine ?? new MesaMember { Name = me.Name }, me, true));
        return new MesaStatusView(mesa.Id, mesa.Name, mesa.Code, mesa.Sync, WaitingFor(mesa, me, friends), list);
    }

    public static MesaFriendView Friend(MesaMember m, Career c, bool me)
    {
        var engine = new GameEngine(c);
        var date = c.SeasonDay >= 1 ? SeasonCalendar.Label(SeasonCalendar.At(c.CalendarType, c.SeasonDay)) : null;
        return new MesaFriendView(m.Name, GameEngine.CleanClub(c.Club), date, c.Season, c.SeasonDay, engine.LeaguePosition(), c.Overall,
            c.Status == CareerStatus.Finished ? "Finished" : c.Phase.ToString(), me);
    }

    /// <summary>Cria o duelo da liga marcado na carreira, se o amigo ainda não criou.</summary>
    public static async Task EnsureLeagueDuel(Camisa10DbContext db, Career me, Mesa mesa, CancellationToken ct)
    {
        var p = me.PendingDuel;
        if (p?.DuelId == null) return;
        if (await db.Duels.AsNoTracking().AnyAsync(d => d.Id == p.DuelId, ct)) return;
        var friend = await db.Careers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == p.OpponentCareerId, ct);
        if (friend == null) return;
        var meMember = mesa.Members.FirstOrDefault(m => m.CareerId == me.Id);
        var frMember = mesa.Members.FirstOrDefault(m => m.CareerId == friend.Id);
        var mine = Side(me, meMember?.Name ?? me.Name);
        var theirs = Side(friend, frMember?.Name ?? friend.Name);
        var duel = new Duel
        {
            Id = p.DuelId, MesaId = mesa.Id, Kind = "league", Competition = p.Competition, Season = me.Season, Turn = me.SeasonDay,
            Round = p.Round, Home = p.Home ? mine : theirs, Away = p.Home ? theirs : mine
        };
        DuelEngine.Setup(duel);
        db.Duels.Add(duel);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            // o amigo criou o mesmo duelo ao mesmo tempo: fica o dele
            db.Entry(duel).State = EntityState.Detached;
            if (!await db.Duels.AsNoTracking().AnyAsync(d => d.Id == p.DuelId, ct)) throw;
        }
    }

    public static DuelSide Side(Career c, string name) => new()
    {
        PlayerId = c.PlayerId, CareerId = c.Id, Name = name,
        Club = c.Base ? GameEngine.CleanClub(c.Club) + " sub-20" : c.Club, Position = c.Position, Attributes = c.Attributes.Clone()
    };

    /// <summary>Escreve no diário da mesa (com nova tentativa se outro amigo gravou ao mesmo tempo).</summary>
    public static async Task Post(Camisa10DbContext db, string mesaId, string icon, string text, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var mesa = await db.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId, ct);
            if (mesa == null) return;
            mesa.Feed.Add(new MesaFeedItem { Icon = icon, Text = text });
            if (mesa.Feed.Count > 60) mesa.Feed.RemoveRange(0, mesa.Feed.Count - 60);
            mesa.Version++;
            mesa.UpdatedAt = DateTime.UtcNow;
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException)
            {
                db.Entry(mesa).State = EntityState.Detached;
            }
        }
    }

    /// <summary>Notícias da carreira para o diário: títulos novos e fim de carreira.</summary>
    public record Snapshot(int Trophies, CareerStatus Status, string Club, bool Base);

    public static Snapshot Snap(Career c) => new(c.Trophies.Count, c.Status, c.Club, c.Base);

    public static IEnumerable<(string Icon, string Text)> News(string name, Snapshot before, Career after)
    {
        foreach (var t in after.Trophies.Skip(before.Trophies))
            yield return (t.Kind == "award" ? "⭐" : "🏆", $"{name} conquistou {(t.Kind == "award" ? "a Bola de Ouro" : t.Name)} com {(t.Kind == "national" ? "a seleção" : t.Team)}!");
        if (before.Status == CareerStatus.Active && after.Status == CareerStatus.Finished)
            yield return ("👟", $"{name} pendurou as chuteiras: {after.FinalTitle}.");
        if (before.Club != after.Club && !after.Base && !before.Base)
            yield return ("🔁", $"{name} trocou o {GameEngine.CleanClub(before.Club)} pelo {after.Club}.");
    }
}
