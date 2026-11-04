using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>Um amigo da mesa visto pela carreira: o clube e a liga dele agora.</summary>
public record MesaFriend(string CareerId, string PlayerId, string Name, string? ClubId, string? TableKey, string TableHash);

/// <summary>
/// O que a carreira precisa saber da mesa (montado pela API): os amigos, os placares reais dos clubes
/// deles e as classificações finais das ligas, para o mundo ser o mesmo para todos.
/// </summary>
public record MesaContext(string MesaId, IReadOnlyList<MesaFriend> Friends,
    IReadOnlyList<MesaResult>? Results = null, IReadOnlyList<ClubSeasonResult>? ClubSeasons = null);

/// <summary>Mesa: o jogo da liga contra o clube de um amigo vira um duelo entre os dois.</summary>
public sealed partial class GameEngine
{
    /// <summary>Amigos da mesa (a API preenche antes da ação).</summary>
    public MesaContext? Mesa { get; set; }

    /// <summary>Assinatura do sorteio da liga: dois amigos com a mesma assinatura têm os mesmos confrontos.</summary>
    public static string TableHash(Career c) =>
        StableSeed(c.LeagueOrder.Count > 0
            ? string.Join(",", c.LeagueOrder)
            : string.Join(",", c.LeagueTable.Select(r => r.ClubId).OrderBy(x => x, StringComparer.Ordinal))).ToString("x");

    /// <summary>
    /// Acerta a carreira com a mesa antes da ação: classificações finais das ligas dos amigos (para o mundo
    /// da próxima temporada ser o mesmo) e placares reais dos clubes deles na tabela desta temporada.
    /// </summary>
    public void SyncMesa()
    {
        if (Mesa == null) return;
        var me = MyLeagueClubId;
        foreach (var cs in Mesa.ClubSeasons ?? Array.Empty<ClubSeasonResult>())
        {
            if (cs.ClubId == me) continue;
            s.ClubSeasons.RemoveAll(x => x.Season == cs.Season && x.ClubId == cs.ClubId);
            s.ClubSeasons.Add(new ClubSeasonResult { Season = cs.Season, ClubId = cs.ClubId, Division = cs.Division, Rank = cs.Rank, Size = cs.Size, Order = cs.Order.ToList() });
        }
        var changed = false;
        foreach (var r in (Mesa.Results ?? Array.Empty<MesaResult>()).Where(r => r.Season == s.Season && r.Key == s.LeagueTableKey))
        {
            var e = s.LeagueResults.FirstOrDefault(x => x.Round == r.Round && x.Home == r.Home && x.Away == r.Away);
            if (e == null || (e.HomeGoals == r.HomeGoals && e.AwayGoals == r.AwayGoals)) continue;
            // o jogo foi simulado aqui antes do amigo jogar: troca pelo placar real
            Record(e.Home, e.HomeGoals, e.AwayGoals, -1);
            Record(e.Away, e.AwayGoals, e.HomeGoals, -1);
            e.HomeGoals = r.HomeGoals;
            e.AwayGoals = r.AwayGoals;
            Record(e.Home, e.HomeGoals, e.AwayGoals);
            Record(e.Away, e.AwayGoals, e.HomeGoals);
            changed = true;
        }
        // liga já encerrada nesta temporada: a classificação gravada acompanha a tabela acertada
        if (changed && !s.Base && s.ClubSeasons.FirstOrDefault(x => x.Season == s.Season && x.ClubId == me) is { } mine)
        {
            var sorted = Sorted(s.LeagueTable);
            mine.Order = sorted.Select(x => x.ClubId).ToList();
            mine.Rank = sorted.FindIndex(x => x.ClubId == me) + 1;
        }
    }

    public static string DuelKey(string mesaId, int season, int turn, string careerA, string careerB) =>
        string.CompareOrdinal(careerA, careerB) < 0
            ? $"{mesaId}|{season}|{turn}|{careerA}|{careerB}"
            : $"{mesaId}|{season}|{turn}|{careerB}|{careerA}";

    /// <summary>Id do duelo a partir da chave (o mesmo para os dois amigos).</summary>
    public static string DuelId(string key) => "d" + ((uint)StableSeed(key)).ToString("x8") + ((uint)StableSeed(key + "#")).ToString("x8");

    /// <summary>
    /// O adversário da rodada é o clube de um amigo da mesa, na mesma liga e com a mesma tabela?
    /// Então o jogo vira duelo (os dois veem o mesmo confronto).
    /// </summary>
    private MesaFriend? FriendOpponent((string Id, bool Home) fixture)
    {
        if (Mesa == null || s.MesaId == null) return null;
        var hash = TableHash(s);
        return Mesa.Friends.FirstOrDefault(f => f.ClubId == fixture.Id && f.TableKey == s.LeagueTableKey && f.TableHash == hash);
    }

    /// <summary>Marca o duelo do dia: a fila do dia segue, e o duelo entra no lugar do jogo.</summary>
    private void QueueDuel(MesaFriend friend, (string Id, bool Home) fixture, int round, string competition)
    {
        var key = DuelKey(s.MesaId!, s.Season, s.SeasonDay, s.Id, friend.CareerId);
        s.PendingDuel = new DuelRef
        {
            Key = key, DuelId = DuelId(key), OpponentCareerId = friend.CareerId, OpponentName = friend.Name,
            OpponentClub = World.World.Club(fixture.Id)?.Name ?? "", Home = fixture.Home, Round = round, Competition = competition
        };
        s.Queue.Add(new GameEvent
        {
            EventId = "duel",
            Title = $"Clássico da Mesa: contra {friend.Name}",
            Text = $"O jogo da rodada é contra o {s.PendingDuel.OpponentClub}, de {friend.Name}. Desta vez a decisão é dos dois."
        });
    }

    /// <summary>Aplica o resultado do duelo na carreira: placar na tabela, jogo da temporada e moral.</summary>
    public void FinishDuel(DuelOutcome o)
    {
        if (s.Phase != GamePhase.Duel || s.PendingDuel == null) throw new GameRuleException("Não há duelo esperando.");
        EnsureWorld();
        var p = s.PendingDuel;
        if (o.Kind == "league" && s.LeagueRound == p.Round && s.LeagueTable.Count > 0) PlayLeagueRound(p.Round, (o.For, o.Against));
        var m = new MatchState { MyGoals = o.MyGoals, MyAssists = o.MyAssists, Good = o.Won, Bad = o.Lost, For = o.For, Against = o.Against };
        s.SeasonMatches.Add(new MatchRecord
        {
            Turn = s.SeasonDay, Kind = "league", Competition = p.Competition, Opponent = p.OpponentClub, Home = p.Home,
            For = o.For, Against = o.Against, Goals = o.MyGoals, Assists = o.MyAssists, Rating = MatchRating(m)
        });
        var win = o.For > o.Against;
        var draw = o.For == o.Against;
        var fx = win ? new Fx { Fame = 4, Morale = 6 } : draw ? new Fx { Morale = 1 } : new Fx { Morale = -4 };
        var before = Snapshot();
        var applied = Apply(fx);
        var me = PtNames.Cap(PtNames.The(CleanClub(s.Club), false));
        s.LastResult = new PickResult
        {
            Title = $"Clássico da Mesa contra {p.OpponentName}",
            Chosen = win ? "Vitória no duelo" : draw ? "Empate no duelo" : "Derrota no duelo",
            Text = $"{me} {o.For} x {o.Against} {p.OpponentClub}." +
                   (o.MyGoals > 0 ? $" Você marcou {o.MyGoals}." : "") + (o.MyAssists > 0 ? $" {o.MyAssists} assistência{(o.MyAssists > 1 ? "s" : "")}." : "") +
                   (s.LeagueTable.Count > 0 && o.Kind == "league" ? $" {me} está em {LeaguePosition()}º na tabela." : ""),
            Success = win || draw,
            Chips = Chips(fx, before, applied)
        };
        s.PendingDuel = null;
        s.Current = null;
        s.Phase = GamePhase.Result;
    }
}
