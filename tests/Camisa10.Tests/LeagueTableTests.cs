using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

public class LeagueTableTests(ITestOutputHelper output)
{
    [Fact]
    public void Tabela_ao_vivo_fecha_com_os_jogos_e_decide_o_acesso()
    {
        var seasons = 0;
        var goals = new List<int>();
        var games = new List<int>();
        var ratings = new List<double>();
        for (var seed = 1; seed <= 5; seed++)
        {
            SeasonReport? checkedReport = null;
            GameSimulationTests.PlayToEnd(seed, "Atacante", "Palmeiras", s =>
            {
                // durante a temporada: todos os clubes com o mesmo número de jogos e pontos que batem
                if (s.LeagueTable.Count > 0)
                {
                    Assert.All(s.LeagueTable, r => Assert.InRange(r.Played, s.LeagueRound - 1, s.LeagueRound));
                    Assert.All(s.LeagueTable, r => Assert.Equal(r.Won * 3 + r.Drawn, r.Points));
                    Assert.Equal(s.LeagueTable.Sum(r => r.Won), s.LeagueTable.Sum(r => r.Lost));
                    Assert.Equal(s.LeagueTable.Sum(r => r.GoalsFor), s.LeagueTable.Sum(r => r.GoalsAgainst));
                    Assert.True(s.LeagueTable.Max(r => r.Played) - s.LeagueTable.Min(r => r.Played) <= 1, "rodadas em dia (só a folga muda)");
                }
                if (s.Phase != GamePhase.SeasonSummary || s.LastSeason == null || ReferenceEquals(checkedReport, s.LastSeason)) return;
                var rep = s.LastSeason;
                checkedReport = rep;
                seasons++;
                // estatísticas do balanço = soma dos jogos
                Assert.Equal(rep.Matches.Count, rep.Games);
                Assert.Equal(rep.Matches.Sum(x => x.Goals), rep.Goals);
                Assert.Equal(rep.Matches.Sum(x => x.Assists), rep.Assists);
                if (rep.Table.Count > 0)
                {
                    Assert.Equal(rep.Rank, rep.Table.FindIndex(r => r.Mine) + 1);
                    Assert.Equal(rep.Title, rep.Rank == 1);
                    Assert.True(rep.Table.Zip(rep.Table.Skip(1)).All(p => p.First.Points >= p.Second.Points), "tabela ordenada por pontos");
                }
                if (!s.Base) { goals.Add(rep.Goals); games.Add(rep.Games); ratings.Add(rep.Rating); }
            });
        }
        output.WriteLine($"temporadas {seasons}; por temporada no profissional: {games.Average():F1} jogos, {goals.Average():F1} gols, nota {ratings.Average():F2}");
        Assert.True(seasons > 20);
        Assert.InRange(games.Average(), 10, 40);
        Assert.InRange(goals.Average(), 3, 30);
        Assert.InRange(ratings.Average(), 5, 8.5);
    }

    [Fact]
    public void Turno_sem_repetir_confronto_e_cada_clube_joga_uma_vez_por_rodada()
    {
        var (s, e) = Pro("Palmeiras");
        var table = s.LeagueTable;
        Assert.Equal(20, table.Count);
        var fixtures = typeof(GameEngine).GetMethod("Fixtures", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var pairs = new HashSet<string>();
        for (var r = 0; r < 19; r++)
        {
            var round = (List<(string Home, string Away)>)fixtures.Invoke(e, new object[] { r })!;
            Assert.Equal(10, round.Count);
            var ids = round.SelectMany(x => new[] { x.Home, x.Away }).ToList();
            Assert.Equal(20, ids.Distinct().Count());
            foreach (var (h, a) in round) Assert.True(pairs.Add(string.Join("|", new[] { h, a }.OrderBy(x => x))), "confronto repetido no turno");
        }
        Assert.Equal(190, pairs.Count);
    }

    [Fact]
    public void Classificacao_final_vale_para_o_mundo_na_temporada_seguinte()
    {
        var order = World.Countries.Single(c => c.Iso == "BR").Leagues[1].Clubs.Select(c => c.Id).Reverse().ToList();
        var results = new List<ClubSeasonResult>
        {
            new() { Season = 1, ClubId = order[0], Division = 1, Rank = 1, Size = order.Count, Order = order }
        };
        var standings = WorldSim.Standings(9, 1, results, "BR", 1);
        Assert.Equal(order, standings.Select(r => r.ClubId).ToList());
        var next = WorldSim.Layout(9, 2, results);
        // os 3 primeiros da tabela ao vivo sobem, os 3 últimos caem
        Assert.All(order.Take(3), id => Assert.Equal(0, next.Of(id)!.Value.Division));
        Assert.All(order.TakeLast(3), id => Assert.Equal(2, next.Of(id)!.Value.Division));
    }

    [Fact]
    public void Calendario_mostra_a_tabela_ao_vivo_e_a_posicao_no_hud()
    {
        var (s, _) = Pro("Palmeiras");
        var view = ViewBuilder.Build(s);
        Assert.NotNull(view.Calendar!.Table);
        Assert.Equal(20, view.Calendar.Table!.Rows.Count);
        Assert.Contains(view.Calendar.Table.Rows, r => r.Mine && r.Club == "Palmeiras");
        Assert.Equal(20, view.Hud.LeagueSize);
        // antes da primeira rodada, sem posição (a tabela só tem zeros)
        Assert.Equal(0, view.Hud.LeaguePosition);
    }

    private static (Career, GameEngine) Pro(string club)
    {
        var rng = new GameRandom(3);
        var career = GameEngine.NewCareer("p", "Teste", club, "Brasil", "Atacante", rng);
        career.Base = false;
        career.Age = 22;
        career.Season = 1;
        career.SeasonDay = 0;
        career.ClubId = World.ClubByName(club)!.Id;
        career.Club = club;
        career.LeagueName = null;
        career.Competitions.Clear();
        var engine = new GameEngine(career, rng);
        engine.EnsureWorld();
        return (career, engine);
    }
}

public class TabsDataTests
{
    [Fact]
    public void Calendario_e_tabela_vem_em_qualquer_fase_da_carreira()
    {
        var seen = new HashSet<GamePhase>();
        GameSimulationTests.PlayToEnd(11, "Meia", "Santos", s =>
        {
            if (s.Status != CareerStatus.Active || s.Season == 0) return;
            var v = ViewBuilder.Build(s);
            Assert.NotNull(v.Calendar);
            seen.Add(s.Phase);
        });
        Assert.Contains(GamePhase.Event, seen);
        Assert.Contains(GamePhase.Result, seen);
        Assert.Contains(GamePhase.SeasonSummary, seen);
    }
}
