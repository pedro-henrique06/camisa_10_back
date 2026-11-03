using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;

namespace Camisa10.Tests;

public class GameSimulationTests
{
    /// <summary>Joga uma carreira inteira escolhendo opções aleatórias.</summary>
    internal static (Career Career, int Steps) PlayToEnd(int seed, string position = "Meia", string club = "Flamengo", Action<Career>? onStep = null)
    {
        var rng = new GameRandom(seed);
        var picker = new Random(seed);
        var career = GameEngine.NewCareer("player", "Teste", club, "Brasil", position, rng);
        var engine = new GameEngine(career, rng);
        var steps = 0;
        while (career.Status == CareerStatus.Active)
        {
            Assert.True(++steps < 50_000, "carreira não terminou");
            var view = ViewBuilder.Build(career); // a view precisa ser montável em qualquer fase
            onStep?.Invoke(career);
            if (view.Hud.CanSearchClub && picker.NextDouble() < .02)
            {
                engine.Act(new GameAction(ActionType.SearchClub));
                continue;
            }
            switch (career.Phase)
            {
                case GamePhase.Event:
                    Assert.NotNull(view.Event);
                    engine.Act(new GameAction(ActionType.Choose, picker.Next(view.Event!.Options.Count)));
                    break;
                case GamePhase.Penalty:
                    Assert.NotNull(view.Event!.PenaltyZones);
                    engine.Act(new GameAction(ActionType.Choose, picker.Next(6)));
                    break;
                case GamePhase.Market:
                    Assert.NotEmpty(view.Market!.Offers);
                    if (picker.NextDouble() < .5) engine.Act(new GameAction(ActionType.Stay));
                    else engine.Act(new GameAction(ActionType.Choose, picker.Next(view.Market.Offers.Count)));
                    break;
                case GamePhase.Swap:
                    engine.Act(new GameAction(ActionType.Choose, picker.Next(2)));
                    break;
                case GamePhase.Decision:
                    engine.Act(new GameAction(picker.NextDouble() < .3 ? ActionType.Retire : ActionType.Continue));
                    break;
                default:
                    engine.Act(new GameAction(ActionType.Continue));
                    break;
            }
        }
        return (career, steps);
    }

    [Theory]
    [InlineData("Atacante")]
    [InlineData("Ponta")]
    [InlineData("Meia")]
    [InlineData("Volante")]
    [InlineData("Lateral")]
    [InlineData("Zagueiro")]
    public void Carreiras_aleatorias_chegam_ao_fim(string position)
    {
        var titles = new List<string>();
        for (var seed = 1; seed <= 20; seed++)
        {
            var (c, _) = PlayToEnd(seed * 7919, position, GameData.BaseClubs[seed % GameData.BaseClubs.Length].Club);
            Assert.Equal(GamePhase.Finished, c.Phase);
            Assert.NotNull(c.FinalTitle);
            // quem para por lesão grave (ou escolhe a saúde) pode parar antes dos 18
            Assert.InRange(c.Age, c.EndReason != null ? 15 : 18, 38);
            Assert.All(Attributes.Keys, k => Assert.InRange(c.Attributes[k], 1, 99));
            Assert.InRange(c.Overall, 0, 99);
            Assert.True(c.Money >= 0);
            Assert.NotNull(ViewBuilder.Build(c).Final);
            Assert.NotNull(ViewBuilder.History(c));
            titles.Add(c.FinalTitle!);
        }
        Assert.True(titles.Distinct().Count() >= 2, "resultados de carreira variados");
    }

    [Fact]
    public void Nova_carreira_comeca_no_calendario_da_base()
    {
        var c = GameEngine.NewCareer("p", "  Zico  ", "Flamengo", "Brasil", "Meia", new GameRandom(1));
        Assert.Equal("Zico", c.Name);
        Assert.Equal("Base do Flamengo", c.Club);
        Assert.True(c.Base);
        Assert.Equal(GamePhase.Calendar, c.Phase);
        Assert.Equal(1, c.Season);
        Assert.Equal(c.Overall, new GameEngine(c).Ovr());
    }

    [Fact]
    public void Acao_invalida_na_fase_e_rejeitada()
    {
        var c = GameEngine.NewCareer("p", "X", "Flamengo", "Brasil", "Meia", new GameRandom(1));
        var engine = new GameEngine(c, new GameRandom(2));
        Assert.Throws<GameRuleException>(() => engine.Act(new GameAction(ActionType.Retire)));
        Assert.Throws<GameRuleException>(() => engine.Act(new GameAction(ActionType.Choose, 0)));
        Assert.Throws<GameRuleException>(() => engine.Act(new GameAction(ActionType.SearchClub)));
    }

    [Theory]
    [InlineData("Treino de finalização", "chu")]
    [InlineData("Driblar o goleiro", "dri")]
    [InlineData("Lançar o atacante", "pas")]
    [InlineData("Dar o bote no tempo certo", "def")]
    [InlineData("Arrancadas curtas", "rit")]
    [InlineData("Academia pesada", "fis")]
    [InlineData("Jogar pelo time", null)]
    public void Atributo_pelo_texto(string text, string? expected) =>
        Assert.Equal(expected, GameEngine.AttributeFromText(text));

    [Fact]
    public void Catalogo_tem_ids_unicos_e_opcoes() 
    {
        Assert.Equal(Events.All.Count, Events.All.Select(e => e.Id).Distinct().Count());
        Assert.All(Events.All, e => Assert.InRange(e.Options.Count, 1, 7));
        foreach (var id in GameData.Personal.Concat(GameData.Training).Concat(GameData.ArcStarts))
            Assert.Contains(Events.All, e => e.Id == id);
    }

    [Fact]
    public void Meta_traz_pesos_de_cada_posicao()
    {
        var meta = ViewBuilder.Meta();
        Assert.Equal(GameData.Positions.Length, meta.PositionInfo.Count);
        foreach (var p in meta.PositionInfo)
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Description));
            Assert.InRange(p.Weights.Sum(w => w.Percent), 99, 101);
            Assert.True(p.Weights.Zip(p.Weights.Skip(1)).All(x => x.First.Percent >= x.Second.Percent), "ordenado do maior peso");
        }
        Assert.Equal("chu", meta.PositionInfo.First(p => p.Name == "Atacante").Weights[0].Key);
        Assert.Equal("def", meta.PositionInfo.First(p => p.Name == "Zagueiro").Weights[0].Key);
    }
}
