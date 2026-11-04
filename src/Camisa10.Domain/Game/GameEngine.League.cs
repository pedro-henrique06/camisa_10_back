using Camisa10.Domain.Model;
using Camisa10.Domain.World;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Tabela da liga ao vivo: a cada data da liga todos os clubes jogam uma rodada (turno e returno pelo
/// método do círculo). O jogo do jogador vem do placar do jogo ao vivo; os outros são simulados pela
/// força dos clubes. No fim, a classificação decide o acesso e o rebaixamento de todos.
/// </summary>
public sealed partial class GameEngine
{
    /// <summary>Liga atual: país, divisão e os ids dos clubes.</summary>
    private (string Iso, int Division, List<string> Ids)? CurrentLeague()
    {
        var id = s.Base ? s.BaseClubId ?? s.ClubId : s.ClubId;
        var layout = Layout();
        if (layout.Of(id) is not { } p) return null;
        return (p.Iso, p.Division, layout.Leagues[p.Iso][p.Division]);
    }

    private string MyLeagueClubId => (s.Base ? s.BaseClubId ?? s.ClubId : s.ClubId)!;

    /// <summary>Cria a tabela zerada quando muda a liga (nova temporada ou transferência).</summary>
    private void EnsureLeagueTable()
    {
        if (CurrentLeague() is not { } league) return;
        var key = $"{league.Iso}|{league.Division}|{s.Season}|{(s.Base ? "base" : "pro")}";
        if (s.LeagueTableKey == key && s.LeagueTable.Count == league.Ids.Count) return;
        s.LeagueTableKey = key;
        s.LeagueRound = 0;
        s.LeagueTable = league.Ids.Select(id => new LeagueRow
        {
            ClubId = id,
            Club = World.World.Club(id)!.Name + (s.Base ? " sub-20" : "")
        }).ToList();
    }

    /// <summary>Confrontos da rodada <paramref name="round"/> (mandante, visitante), sem repetir até o returno.</summary>
    private List<(string Home, string Away)> Fixtures(int round)
    {
        var ids = s.LeagueTable.Select(r => r.ClubId)
            .OrderBy(id => StableSeed($"{s.WorldSeed}|{s.Season}|{s.LeagueTableKey}|{id}")).ToList();
        if (ids.Count % 2 == 1) ids.Add(""); // folga
        var n = ids.Count;
        var r = round % (n - 1);
        var second = round / (n - 1) % 2 == 1;
        var list = new List<(string, string)>();
        // método do círculo: o primeiro fica parado, os outros giram
        var rot = new List<string> { ids[0] };
        for (var i = 0; i < n - 1; i++) rot.Add(ids[1 + (i + r) % (n - 1)]);
        for (var i = 0; i < n / 2; i++)
        {
            var a = rot[i];
            var b = rot[n - 1 - i];
            if (a == "" || b == "") continue;
            var homeFirst = (i + r) % 2 == 0;
            if (second) homeFirst = !homeFirst;
            list.Add(homeFirst ? (a, b) : (b, a));
        }
        return list;
    }

    /// <summary>Adversário do clube do jogador na rodada (nulo = folga).</summary>
    private (string Id, bool Home)? LeagueOpponent(int round)
    {
        var me = MyLeagueClubId;
        foreach (var (h, a) in Fixtures(round))
        {
            if (h == me) return (a, true);
            if (a == me) return (h, false);
        }
        return null;
    }

    /// <summary>Põe a tabela em dia e devolve o adversário da rodada de hoje (nulo = folga).</summary>
    private (string Id, bool Home)? LeagueFixtureToday(int round)
    {
        CatchUpLeague(round);
        return s.LeagueTable.Count == 0 ? null : LeagueOpponent(round);
    }

    private int LeagueRoundOfToday() =>
        CompetitionOf("league") is { } c ? c.Days.IndexOf(s.SeasonDay) : -1;

    /// <summary>Joga as rodadas que ficaram para trás (lesão, transferência, carreira antiga).</summary>
    private void CatchUpLeague(int upTo)
    {
        EnsureLeagueTable();
        while (s.LeagueRound < upTo) PlayLeagueRound(s.LeagueRound, null);
    }

    /// <summary>Joga a rodada: o jogo do jogador com o placar dele (se houver) e os outros simulados.</summary>
    private void PlayLeagueRound(int round, (int For, int Against)? mine)
    {
        if (round < s.LeagueRound || s.LeagueTable.Count == 0) return;
        var me = MyLeagueClubId;
        foreach (var (h, a) in Fixtures(round))
        {
            int hg, ag;
            if (mine is { } m && (h == me || a == me))
                (hg, ag) = h == me ? (m.For, m.Against) : (m.Against, m.For);
            else
                (hg, ag) = SimulateScore(h, a);
            Record(h, hg, ag);
            Record(a, ag, hg);
        }
        s.LeagueRound = round + 1;
    }

    private void Record(string id, int gf, int ga)
    {
        var row = s.LeagueTable.FirstOrDefault(r => r.ClubId == id);
        if (row == null) return;
        row.Played++;
        row.GoalsFor += gf;
        row.GoalsAgainst += ga;
        if (gf > ga) { row.Won++; row.Points += 3; }
        else if (gf == ga) { row.Drawn++; row.Points += 1; }
        else row.Lost++;
    }

    /// <summary>Placar de um jogo entre dois clubes pela força (prestígio) e o mando.</summary>
    private (int, int) SimulateScore(string home, string away)
    {
        var ph = World.World.Club(home)!.Prestige;
        var pa = World.World.Club(away)!.Prestige;
        var diff = (ph - pa) * 1.6;
        return (Poisson(1.35 * Math.Exp(diff / 2 + .1)), Poisson(1.1 * Math.Exp(-diff / 2 - .1)));
    }

    private int Poisson(double lambda)
    {
        var l = Math.Exp(-lambda);
        var k = 0;
        var p = 1.0;
        do { k++; p *= Rand(); } while (p > l && k < 9);
        return k - 1;
    }

    /// <summary>Classificação: pontos, saldo, gols marcados e nome.</summary>
    public static List<LeagueRow> Sorted(IEnumerable<LeagueRow> table) => table
        .OrderByDescending(r => r.Points)
        .ThenByDescending(r => r.GoalsFor - r.GoalsAgainst)
        .ThenByDescending(r => r.GoalsFor)
        .ThenBy(r => r.Club, StringComparer.Ordinal)
        .ToList();

    /// <summary>Posição atual do clube do jogador (1 em diante); 0 sem tabela ou antes da primeira rodada.</summary>
    public int LeaguePosition() =>
        s.LeagueTable.Count == 0 || s.LeagueRound == 0 ? 0 : Sorted(s.LeagueTable).FindIndex(r => r.ClubId == MyLeagueClubId) + 1;

    /// <summary>Linha da narração depois do apito: onde o time ficou na tabela.</summary>
    private string TableLine()
    {
        var pos = LeaguePosition();
        var row = s.LeagueTable.First(r => r.ClubId == MyLeagueClubId);
        return $"Com o resultado, {PtNames.The(MyTeam, false)} fica em {pos}º na tabela, com {row.Points} ponto{(row.Points == 1 ? "" : "s")}.";
    }
}
