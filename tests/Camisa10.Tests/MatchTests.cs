using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Xunit.Abstractions;

namespace Camisa10.Tests;

public class MatchTests(ITestOutputHelper output)
{
    [Fact]
    public void Lances_de_jogo_tem_campo_placar_e_narracao_ate_o_apito_final()
    {
        var finals = new List<MatchState>();
        string? sample = null;
        for (var seed = 1; seed <= 4; seed++)
        {
            GameSimulationTests.PlayToEnd(seed, "Atacante", "Palmeiras", s =>
            {
                var view = ViewBuilder.Build(s);
                if (s.Phase == GamePhase.Event && s.Current is { IsMatch: true, Minute: not null } ev)
                {
                    Assert.NotNull(view.Event!.Match);
                    var lines = view.Event.Match!.Lines;
                    Assert.NotEmpty(lines);
                    Assert.Equal("decision", lines[^1].Type);
                    Assert.Equal(ev.Minute, lines[^1].Minute);
                    Assert.True(lines.Zip(lines.Skip(1)).All(p => p.First.Minute <= p.Second.Minute), "minutos em ordem");
                }
                if (s.Phase == GamePhase.Result && view.Result!.Match is { Final: true } mv && s.Match is { } m && !finals.Contains(m))
                {
                    Assert.Contains(mv.Lines, l => l.Type == "whistle" && l.Text.StartsWith("Fim de jogo"));
                    // na liga, depois do apito, a narração diz onde o time ficou na tabela
                    if (m.Kind == "league") Assert.Contains("na tabela", mv.Lines[^1].Text);
                    finals.Add(m);
                    if (sample == null && m.Feed.Count > 6) sample = string.Join("\n", m.Feed.Select(l => $"{l.Minute}' [{l.Type}] {l.Text} ({l.For}-{l.Against})"));
                }
            });
        }
        output.WriteLine(sample);
        Assert.True(finals.Count > 50);
        var goals = finals.Average(m => m.For + m.Against);
        Assert.InRange(goals, 1.5, 4.5);
        // copa: a decisão define quem vence
        Assert.Contains(finals, m => m.Kind != "league");
        Assert.All(finals, m => Assert.True(m.Feed.Count(l => l.Type == "whistle" && l.Text.StartsWith("Fim")) == 1));
    }
}

public class NationalMatchTests
{
    [Fact]
    public void Torneio_da_selecao_tem_campo_placar_e_narracao()
    {
        var rng = new Camisa10.Domain.Game.GameRandom(5);
        var s = Camisa10.Domain.Game.GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", rng);
        var e = new Camisa10.Domain.Game.GameEngine(s, rng);
        s.Base = false;
        s.Age = 24;
        s.Season = 2;
        s.ClubId = Camisa10.Domain.World.World.ClubByName("Flamengo")!.Id;
        s.Club = "Flamengo";
        s.SetFlag("sel");
        s.Queue.Clear();
        s.Scheduled.Clear();
        s.Competitions.Clear();
        e.EnsureWorld();
        s.SeasonDay = Camisa10.Domain.Game.GameEngine.SeasonDays;
        s.Phase = GamePhase.Calendar;
        s.CalendarShown = true;

        var games = 0;
        for (var i = 0; i < 40 && (s.Cup?.On ?? true) && games < 7; i++)
        {
            e.Act(new Camisa10.Domain.Game.GameAction(Camisa10.Domain.Game.ActionType.Continue));
            while (s.Phase == GamePhase.Event && s.Current is { Focus: not "cup" })
                e.Act(new Camisa10.Domain.Game.GameAction(Camisa10.Domain.Game.ActionType.Choose, 0));
            if (s.Phase != GamePhase.Event || s.Current?.Focus != "cup") break;

            var view = ViewBuilder.Build(s);
            var match = view.Event!.Match!;
            Assert.Equal("national", match.Kind);
            Assert.Equal("Brasil", match.Team);
            Assert.Equal("Copa América", match.Competition);
            Assert.Equal("#f7d117", match.TeamBadge.Primary);
            Assert.Equal("national", view.Event.Competition!.Kind);
            Assert.Equal("decision", match.Lines[^1].Type);

            e.Act(new Camisa10.Domain.Game.GameAction(Camisa10.Domain.Game.ActionType.Choose, 1));
            var result = ViewBuilder.Build(s).Result!.Match!;
            Assert.True(result.Final);
            Assert.Contains("do Brasil", result.Lines[^1].Text);
            // artigo certo para o adversário (ex.: "da França", nunca "do França")
            var feed = string.Join(" ", s.Match!.Feed.Select(l => l.Text));
            var wrong = Camisa10.Domain.Game.PtNames.Article(match.Opponent, true) == "o" ? null : "do " + match.Opponent;
            if (wrong != null) Assert.DoesNotContain(wrong, feed);
            games++;
        }
        Assert.True(games >= 3, "jogou a fase de grupos com o campo");
    }

    [Theory]
    [InlineData("Brasil", "do Brasil", "o Brasil")]
    [InlineData("França", "da França", "a França")]
    [InlineData("Portugal", "de Portugal", "Portugal")]
    [InlineData("Coreia do Sul", "da Coreia do Sul", "a Coreia do Sul")]
    [InlineData("EUA", "dos EUA", "os EUA")]
    [InlineData("Costa do Marfim", "da Costa do Marfim", "a Costa do Marfim")]
    [InlineData("Canadá", "do Canadá", "o Canadá")]
    public void Artigos_das_selecoes(string name, string of, string the)
    {
        Assert.Equal(of, Camisa10.Domain.Game.PtNames.Of(name, true));
        Assert.Equal(the, Camisa10.Domain.Game.PtNames.The(name, true));
        Assert.Equal("do " + name, Camisa10.Domain.Game.PtNames.Of(name, false));
    }
}
