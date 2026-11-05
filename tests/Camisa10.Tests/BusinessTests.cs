using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Empresas e investimentos: abrir, expandir, vender, lucros por temporada e as decisões.</summary>
public class BusinessTests(ITestOutputHelper output)
{
    private static (Career, GameEngine) New(int seed, double money = 50, int fame = 40)
    {
        var rng = new GameRandom(seed);
        var c = GameEngine.NewCareer("p", "Rafael Lima", "Flamengo", "Brasil", "Atacante", rng);
        c.Base = false;
        c.Age = 25;
        c.Season = 3;
        c.ClubId = World.ClubByName("Flamengo")!.Id;
        c.Club = "Flamengo";
        c.Fame = fame;
        c.Money = money;
        var e = new GameEngine(c, rng);
        e.EnsureWorld();
        return (c, e);
    }

    private static void Act(GameEngine e, ActionType type, string id, int? index = null) => e.Act(new GameAction(type, index, null, id));

    private static void Choose(Career c, GameEngine e, string id, int option)
    {
        c.Phase = GamePhase.Event;
        c.Current = Events.Every.First(x => x.Id == id).ToEvent();
        e.Act(new GameAction(ActionType.Choose, option));
    }

    [Fact]
    public void Abrir_expandir_e_vender_empresas()
    {
        var (c, e) = New(1);
        Act(e, ActionType.OpenCompany, "hamburgueria");
        Act(e, ActionType.OpenCompany, "academia");
        Act(e, ActionType.OpenCompany, "hamburgueria"); // pode ter mais de uma do mesmo tipo
        Assert.Equal(3, c.Companies.Count);
        Assert.Equal(42, c.Money, 1);
        Assert.Contains(c.Companies, x => x.Name == "Lima Burger");
        Assert.Equal(2, c.Companies.Select(x => x.Name).Distinct().Count(n => n.Contains("Burger")));

        var burger = c.Companies[0];
        Act(e, ActionType.ExpandCompany, burger.Id);
        Assert.Equal(2, burger.Level);
        Assert.Contains(burger.History, h => h.Note!.Contains("2ª loja"));

        var before = c.Money;
        Act(e, ActionType.SellCompany, burger.Id);
        Assert.Equal("vendida", burger.Status);
        Assert.True(c.Money > before);
        Assert.Throws<GameRuleException>(() => Act(e, ActionType.ExpandCompany, burger.Id));
        Act(e, ActionType.OpenCompany, "imoveis"); // ainda dá (R$ 10 mi)
        Assert.Equal(3, c.Companies.Count(x => x.Status == "ativa"));
    }

    [Fact]
    public void Sem_dinheiro_nao_abre()
    {
        var (c, e) = New(2, money: 1);
        var ex = Assert.Throws<GameRuleException>(() => Act(e, ActionType.OpenCompany, "hamburgueria"));
        Assert.Contains("Faltam", ex.Message);
        Assert.Empty(c.Companies);
    }

    /// <summary>Lucro médio por temporada sobre o investido, em muitas temporadas.</summary>
    private static (double Mean, double Worst, double Best) Yield(string kind, int fame)
    {
        var rates = new List<double>();
        for (var seed = 1; seed <= 300; seed++)
        {
            var (c, e) = New(seed, 50, fame);
            Act(e, ActionType.OpenCompany, kind);
            e.SeasonBusiness();
            rates.Add(c.Companies[0].LastProfit / c.Companies[0].Invested);
        }
        return (rates.Average(), rates.Min(), rates.Max());
    }

    [Fact]
    public void Lucros_seguem_o_risco_e_a_fama()
    {
        var burger = Yield("hamburgueria", 40);
        var famous = Yield("hamburgueria", 90);
        var flat = Yield("imoveis", 40);
        var startup = Yield("startup", 40);
        output.WriteLine($"hamburgueria {burger}, famoso {famous}, prédio {flat}, startup {startup}");
        Assert.InRange(burger.Mean, .14, .22);
        Assert.True(famous.Mean > burger.Mean * 1.15, "a fama ajuda a marca");
        Assert.True(flat.Worst > 0, "o prédio de aluguel não dá prejuízo");
        Assert.True(burger.Worst < 0, "a hamburgueria pode ter temporada ruim");
    }

    [Fact]
    public void Lucro_entra_no_dinheiro_e_fica_no_historico()
    {
        var (c, e) = New(3);
        Act(e, ActionType.OpenCompany, "posto");
        var before = c.Money;
        for (var i = 0; i < 4; i++) { c.Age++; e.SeasonBusiness(); }
        var posto = c.Companies[0];
        Assert.Equal(5, posto.History.Count); // inauguração + 4 temporadas
        Assert.Equal(c.Money - before, posto.TotalProfit, 0);
        var view = ViewBuilder.Build(c).Hud.Business!;
        Assert.Single(view.Companies);
        Assert.True(view.NetWorth > c.Money);
        Assert.Equal(8, view.Catalog.Count);
    }

    [Fact]
    public void Investimentos_rendem_e_podem_ser_resgatados()
    {
        var (c, e) = New(4, money: 30);
        Act(e, ActionType.Invest, "renda", 100); // R$ 10 mi
        Act(e, ActionType.Invest, "cripto", 50);
        Assert.Equal(15, c.Money, 1);
        var returns = new List<double>();
        for (var i = 0; i < 6; i++) { c.Age++; e.SeasonBusiness(); returns.Add(c.Investments.First(x => x.Kind == "cripto").LastReturn); }
        var renda = c.Investments.First(x => x.Kind == "renda");
        Assert.All(renda.History, h => Assert.InRange(h.Return, .08, .12));
        Assert.True(renda.Value > 16);
        output.WriteLine("cripto: " + string.Join(", ", returns.Select(r => $"{r:P0}")));
        var money = c.Money;
        Act(e, ActionType.Withdraw, "renda", 50);
        Assert.Equal(money + 5, c.Money, 1);
        Act(e, ActionType.Withdraw, "renda");
        Assert.Equal(0, renda.Value);
        Assert.Throws<GameRuleException>(() => Act(e, ActionType.Invest, "acoes", 10_000));
    }

    [Fact]
    public void Decisoes_de_negocio_mexem_nas_empresas()
    {
        var (c, e) = New(5);
        Act(e, ActionType.OpenCompany, "restaurante");
        var r = c.Companies[0];
        r.Value = 2; // em crise
        c.FocusCompany = r.Id; // o evento escolhe a empresa quando aparece
        Choose(c, e, "b_crise", 0);
        Assert.True(r.Invested > 5 && r.Boost > 0);

        r.Value = 10;
        var money = c.Money;
        Choose(c, e, "b_proposta", 0);
        Assert.Equal("vendida", r.Status);
        Assert.Equal(money + 16, c.Money, 1);

        Act(e, ActionType.Invest, "acoes", 100);
        Choose(c, e, "b_bolsa", 1); // segurar
        Assert.True(c.Investments.First(x => x.Kind == "acoes").Rebound);

        Choose(c, e, "b_ideia", 0);
        Assert.Contains(c.Companies, x => x.Kind == "hamburgueria" && x.Status == "ativa");
    }

    [Fact]
    public void Carreiras_completas_com_negocios()
    {
        var withBusiness = 0;
        for (var seed = 1; seed <= 10; seed++)
        {
            var (c, _) = GameSimulationTests.PlayToEnd(seed, "Atacante");
            if (c.Companies.Count > 0) withBusiness++;
            output.WriteLine($"carreira {seed}: R$ {c.Money} mi, {c.Companies.Count} empresas ({string.Join(", ", c.Companies.Select(x => $"{x.Name} {x.Status} lucro {x.TotalProfit}"))}), " +
                $"investido {c.Investments.Sum(i => i.Value)}");
            Assert.All(c.Companies, x => Assert.True(x.Value >= 0));
        }
        Assert.True(withBusiness >= 5, "boa parte das carreiras abre alguma empresa");
    }
}
