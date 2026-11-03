using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;

namespace Camisa10.Tests;

public class CompetitionTests
{
    /// <summary>Carreira já no profissional, no clube pedido, no começo da temporada.</summary>
    private static (Career, GameEngine) Pro(string club, int seed = 3)
    {
        var rng = new GameRandom(seed);
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

    [Fact]
    public void Clube_paulista_tem_brasileirao_paulistao_copa_do_brasil_e_libertadores()
    {
        var (s, _) = Pro("Palmeiras");
        Assert.Equal(new[] { "league", "state", "cup", "continental" }, s.Competitions.Select(c => c.Kind));
        Assert.Equal("Brasileirão Série A", s.Competitions[0].Name);
        Assert.Equal("Paulistão", s.Competitions[1].Name);
        Assert.Equal("Copa do Brasil", s.Competitions[2].Name);
        Assert.Equal("Libertadores", s.Competitions[3].Name);

        // no máximo um jogo por dia, todos dentro dos 20 dias
        var days = s.Competitions.SelectMany(c => c.Days).ToList();
        Assert.Equal(days.Count, days.Distinct().Count());
        Assert.All(days, d => Assert.InRange(d, 1, GameEngine.SeasonDays));
        // a Libertadores é intercalada com o resto (não fica toda no fim)
        var lib = s.Competitions[3].Days;
        Assert.True(lib.Min() < 5 && lib.Max() > 15);

        var cal = ViewBuilder.Build(s).Calendar!;
        Assert.Equal(4, cal.Competitions.Count);
        Assert.Contains(cal.Cells, c => c.Kind == "continental");
        Assert.Contains(cal.Cells, c => c.Kind == "state");
    }

    [Theory]
    [InlineData("Flamengo", "Campeonato Carioca")]
    [InlineData("Grêmio", "Gauchão")]
    [InlineData("Cruzeiro", "Campeonato Mineiro")]
    [InlineData("Ponte Preta", "Paulistão")]
    public void Estadual_pelo_estado_do_clube(string club, string state)
    {
        var (s, _) = Pro(club);
        Assert.Equal(state, s.Competitions.Single(c => c.Kind == "state").Name);
    }

    [Fact]
    public void Clube_de_fora_do_brasil_nao_tem_estadual()
    {
        var (s, _) = Pro("Real Madrid");
        Assert.DoesNotContain(s.Competitions, c => c.Kind == "state");
        Assert.Equal("Copa do Rei", s.Competitions.Single(c => c.Kind == "cup").Name);
        Assert.Equal("Liga dos Campeões", s.Competitions.Single(c => c.Kind == "continental").Name);
    }

    [Fact]
    public void Serie_b_nao_joga_copa_continental()
    {
        var (s, _) = Pro("Náutico");
        Assert.DoesNotContain(s.Competitions, c => c.Kind == "continental");
        Assert.Contains(s.Competitions, c => c.Kind == "cup");
    }

    [Fact]
    public void Jogos_de_copa_acontecem_nos_dias_marcados_ao_longo_da_temporada()
    {
        var seen = new List<(int Season, int Day, string Kind)>();
        for (var seed = 1; seed <= 6; seed++)
        {
            GameSimulationTests.PlayToEnd(seed, "Atacante", "Palmeiras", s =>
            {
                if (s.Phase != GamePhase.Event || s.Current?.CompetitionId is not { } kind) return;
                var comp = s.Competitions.Single(c => c.Kind == kind);
                Assert.Contains(s.SeasonDay, comp.Days);
                Assert.True(s.SeasonDay < GameEngine.SeasonDays);
                if (seen.LastOrDefault() != (s.Season, s.SeasonDay, kind)) seen.Add((s.Season, s.SeasonDay, kind));
            });
        }
        Assert.Contains(seen, x => x.Kind == "state");
        Assert.Contains(seen, x => x.Kind == "cup");
        Assert.Contains(seen, x => x.Kind == "continental");
        // copas não ficam mais todas depois do dia 20
        Assert.Contains(seen, x => x.Kind == "continental" && x.Day < 10);
    }

    [Fact]
    public void Eliminado_na_fase_de_grupos_some_do_calendario()
    {
        var (s, e) = Pro("Palmeiras");
        var lib = s.Competitions.Single(c => c.Kind == "continental");
        // três derrotas na fase de grupos
        for (var i = 0; i < 3; i++)
        {
            s.SeasonDay = lib.Days[i];
            s.Current = new GameEvent
            {
                CompetitionId = "continental",
                Title = "jogo",
                Options = { new EventOption { Label = "x", SuccessText = "x", Success = new Fx { CupDraw = 1 }, Chance = 1 } }
            };
            s.Phase = GamePhase.Event;
            lib.Points = 0;
            e.Act(new GameAction(ActionType.Choose, 0));
        }
        Assert.True(lib.Out);
        Assert.Equal("Fase de grupos", lib.Result);
        Assert.Equal(3, lib.Days.Count);
        Assert.Equal("cupG", s.Queue[0].EventId);
    }

    [Fact]
    public void Transferencia_no_meio_da_temporada_troca_as_copas()
    {
        var (s, e) = Pro("Palmeiras");
        s.SeasonDay = 10;
        s.Current = new GameEvent
        {
            Title = "Transferência",
            Options = { new EventOption { Label = "x", SuccessText = "x", Success = new Fx { NewClub = "Flamengo", NewClubId = World.ClubByName("Flamengo")!.Id, NewClubTier = 3 } } }
        };
        s.Phase = GamePhase.Event;
        e.Act(new GameAction(ActionType.Choose, 0));
        // o Carioca já acabou no dia 8; a Copa do Brasil começa no dia 11
        Assert.DoesNotContain(s.Competitions, c => c.Kind == "state");
        Assert.Equal(0, s.Competitions.Single(c => c.Kind == "cup").Played);
        Assert.Equal(3, s.Competitions.Single(c => c.Kind == "continental").Played);
    }
}
