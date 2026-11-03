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
                    Assert.Equal("whistle", mv.Lines[^1].Type);
                    Assert.StartsWith("Fim de jogo", mv.Lines[^1].Text);
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
