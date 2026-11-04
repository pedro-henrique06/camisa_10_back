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
        Assert.True(lib.Min() < 10 && lib.Max() > 20);

        var cal = ViewBuilder.Build(s).Calendar!;
        Assert.Equal(4, cal.Competitions.Count);
        Assert.Contains(cal.Cells, c => c.Kind == "continental");
        Assert.Contains(cal.Cells, c => c.Kind == "state");
    }

    [Fact]
    public void No_brasil_a_temporada_vai_de_janeiro_a_dezembro_e_o_estadual_e_no_comeco()
    {
        var (s, _) = Pro("Palmeiras");
        Assert.Equal(SeasonCalendar.Year, s.CalendarType);
        int Month(int turn) => SeasonCalendar.At(s.CalendarType, turn).Month;
        var comp = s.Competitions.ToDictionary(c => c.Kind);
        Assert.All(comp["state"].Days, d => Assert.InRange(Month(d), 1, 3));
        // CBF 2026: Brasileirão de janeiro a dezembro, junto com o estadual no começo do ano
        Assert.Equal(1, Month(comp["league"].Days[0]));
        Assert.Equal(12, Month(comp["league"].Days[^1]));
        Assert.Equal(11, Month(comp["continental"].Days[^1]));   // final da Libertadores em 28 de novembro
        Assert.Equal("6 de dezembro", SeasonCalendar.Label(SeasonCalendar.At(s.CalendarType, comp["cup"].Days[^1])));
        // nenhuma competição fica toda dentro de um mês só
        Assert.All(comp.Values, c => Assert.True(Month(c.Days[^1]) - Month(c.Days[0]) >= 2, c.Name));
        var cal = ViewBuilder.Build(s).Calendar!;
        Assert.Equal("Temporada de janeiro a dezembro", cal.SeasonSpan);
        Assert.Equal("11 de janeiro", cal.NextDate);
    }

    [Fact]
    public void Na_europa_a_temporada_vai_de_agosto_a_maio()
    {
        var (s, _) = Pro("Real Madrid");
        Assert.Equal(SeasonCalendar.European, s.CalendarType);
        var league = s.Competitions.Single(c => c.Kind == "league").Days;
        Assert.Equal(8, SeasonCalendar.At(s.CalendarType, league[0]).Month);
        Assert.Equal(5, SeasonCalendar.At(s.CalendarType, league[^1]).Month);
        Assert.Equal(12, league.Count); // sem estadual, mais rodadas da liga
    }

    [Fact]
    public void Todo_calendario_tem_24_datas_em_ordem_e_as_mesmas_quantidades_de_jogos()
    {
        foreach (var cal in SeasonCalendar.All)
        {
            var dates = SeasonCalendar.Dates(cal);
            Assert.Equal(SeasonCalendar.Length, dates.Count);
            var real = Enumerable.Range(1, dates.Count).Select(t => SeasonCalendar.Date(cal, t)).ToList();
            Assert.True(real.Zip(real.Skip(1)).All(p => p.First < p.Second), $"{cal}: datas fora de ordem");
            Assert.True((real[^1] - real[0]).TotalDays < 366, $"{cal}: temporada passa de um ano");
            Assert.Equal(12, dates.Count(d => d.Kind is "league" or "state"));
            Assert.Equal(5, dates.Count(d => d.Kind == "cup"));
            Assert.Equal(7, dates.Count(d => d.Kind == "continental"));
            Assert.True(SeasonCalendar.WindowOpen(cal, 1), $"{cal}: a janela abre no começo da temporada");
        }
    }

    [Theory]
    [InlineData("Rosenborg", SeasonCalendar.Nordic, 3, 12)]     // Eliteserien: março a dezembro
    [InlineData("Ulsan HD", SeasonCalendar.EastAsia, 2, 12)]    // K League: fim de fevereiro a dezembro
    [InlineData("Zenit", SeasonCalendar.Winter, 7, 5)]          // Rússia: julho a maio, com pausa no inverno
    [InlineData("América", SeasonCalendar.Concacaf, 7, 5)]      // Liga MX: julho a maio
    [InlineData("Sydney FC", SeasonCalendar.Oceania, 7, 5)]     // Australia Cup em julho, A-League até maio
    [InlineData("Manchester City", SeasonCalendar.European, 8, 5)]
    public void Cada_regiao_tem_o_seu_calendario(string club, string calendar, int firstMonth, int lastMonth)
    {
        var c = World.ClubByName(club);
        Assert.NotNull(c);
        Assert.Equal(calendar, SeasonCalendar.ForCountry(World.Country(c!.CountryIso)));
        Assert.Equal(firstMonth, SeasonCalendar.At(calendar, 1).Month);
        Assert.Equal(lastMonth, SeasonCalendar.At(calendar, SeasonCalendar.Length).Month);
    }

    [Fact]
    public void Nordicos_nao_jogam_em_janeiro_e_fevereiro_e_a_russia_para_no_inverno()
    {
        Assert.DoesNotContain(SeasonCalendar.Dates(SeasonCalendar.Nordic), d => d.Month is 1 or 2);
        Assert.DoesNotContain(SeasonCalendar.Dates(SeasonCalendar.Winter), d => d.Month is 1 || (d.Month == 12 && d.Day > 10) || (d.Month == 2 && d.Day < 27));
        // Champions Cup da CONCACAF de fevereiro a maio
        Assert.All(SeasonCalendar.Dates(SeasonCalendar.Concacaf).Where(d => d.Kind == "continental"), d => Assert.InRange(d.Month, 2, 5));
    }

    [Fact]
    public void Argentina_sem_estadual_tem_mais_rodadas_da_liga()
    {
        var (s, _) = Pro("Boca Juniors");
        Assert.Equal(SeasonCalendar.Year, s.CalendarType);
        Assert.DoesNotContain(s.Competitions, c => c.Kind == "state");
        Assert.Equal(12, s.Competitions.Single(c => c.Kind == "league").Days.Count);
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
                if (comp.Sequential) return; // seleção: jogos em sequência depois da temporada
                Assert.Contains(s.SeasonDay, comp.Days);
                if (seen.LastOrDefault() != (s.Season, s.SeasonDay, kind)) seen.Add((s.Season, s.SeasonDay, kind));
            });
        }
        Assert.Contains(seen, x => x.Kind == "state");
        Assert.Contains(seen, x => x.Kind == "cup");
        Assert.Contains(seen, x => x.Kind == "continental");
        // copas não ficam mais todas depois do dia 20
        Assert.Contains(seen, x => x.Kind == "continental" && x.Day < 12);
    }

    [Fact]
    public void Eliminado_na_fase_de_grupos_some_do_calendario()
    {
        var (s, e) = Pro("Palmeiras");
        var lib = s.Competitions.Single(c => c.Kind == "continental");
        // três derrotas na fase de grupos (jogo sem campo: o placar sai da decisão)
        for (var i = 0; i < 3; i++)
        {
            s.SeasonDay = lib.Days[i];
            s.Current = new GameEvent
            {
                CompetitionId = "continental",
                Title = "jogo",
                Options = { new EventOption { Label = "x", SuccessText = "x", Success = new Fx { CupLose = 1 }, Chance = 1 } }
            };
            s.Phase = GamePhase.Event;
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
        // no fim de abril o Carioca já acabou; na Copa do Brasil e na Libertadores ele entra na fase em que o Flamengo está
        Assert.DoesNotContain(s.Competitions, c => c.Kind == "state");
        Assert.Equal(1, s.Competitions.Single(c => c.Kind == "cup").Played);
        Assert.Equal(2, s.Competitions.Single(c => c.Kind == "continental").Played);
        Assert.Equal(SeasonCalendar.Year, s.CalendarType);
    }
}
