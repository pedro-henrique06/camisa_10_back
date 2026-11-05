using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Decisões de carreira e lances de jogo de cada posição, no d20.</summary>
public class DecisionsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Atacante", "pa_")]
    [InlineData("Ponta", "_ponta")]
    [InlineData("Meia", "_meia")]
    [InlineData("Volante", "_vol")]
    [InlineData("Lateral", "_lat")]
    [InlineData("Zagueiro", "_zag")]
    public void Cada_posicao_tem_lances_proprios_no_jogo_com_d20(string position, string marker)
    {
        var positional = 0;
        var withDice = 0;
        var careerMoves = new HashSet<string>();
        GameSimulationTests.PlayToEnd(11, position, "Flamengo", s =>
        {
            if (s.Phase == GamePhase.Event && s.Current is { IsMatch: true } ev && ev.EventId != null && Events.MatchByPosition[position].Any(e => e.Id == ev.EventId))
            {
                Assert.Contains(marker, ev.EventId);
                positional++;
                // as jogadas com chance são resolvidas no d20 com o atributo certo
                var view = ViewBuilder.Build(s).Event!;
                if (view.Options.Any(o => o.Check != null)) withDice++;
            }
            if (s.Phase == GamePhase.Event && s.Current?.EventId is { } id && Events.CareerMoveIds.Contains(id)) careerMoves.Add(id);
        });
        output.WriteLine($"{position}: {positional} lances da posição ({withDice} com d20), decisões de carreira: {string.Join(", ", careerMoves)}");
        Assert.True(positional >= 10, $"{position}: só {positional} lances da posição");
        Assert.Equal(positional, withDice);
        Assert.True(careerMoves.Count >= 3, $"poucas decisões de carreira: {careerMoves.Count}");
    }

    [Fact]
    public void Ids_unicos_no_catalogo_inteiro()
    {
        var ids = Events.Every.Select(e => e.Id).Concat(Events.MatchByPosition.Values.SelectMany(l => l.Select(e => e.Id))).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        // toda opção com chance de jogo tem um atributo do d20
        Assert.All(Events.MatchByPosition.Values.SelectMany(l => l).SelectMany(e => e.Options).Where(o => o.Chance != null),
            o => Assert.Contains(o.Attribute, GameData.TrainingFocusKeys));
        // nas decisões de carreira, as que dependem de você vão para o d20 (as de sorte, como um boato, não)
        Assert.True(Events.CareerMoves.SelectMany(e => e.Options).Count(o => o.Chance != null && o.Attribute != null) >= 10);
        Assert.All(Events.CareerMoves.SelectMany(e => e.Options).Where(o => o.Attribute != null),
            o => Assert.Contains(o.Attribute, GameData.TrainingFocusKeys));
    }
}
