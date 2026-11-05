using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Casa do jogador: equipamentos que seguram a evolução com a idade, e as lesões novas.</summary>
public class EquipmentTests(ITestOutputHelper output)
{
    private static (Career, GameEngine) Pro(int seed, int age, params string[] items)
    {
        var rng = new GameRandom(seed);
        var c = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", rng);
        c.Base = false;
        c.Age = age;
        c.Season = 1;
        c.ClubId = World.ClubByName("Flamengo")!.Id;
        c.Club = "Flamengo";
        c.Potential = 99;
        c.Attributes.Fis = 70;
        c.Money = 100;
        c.Equipment.AddRange(items);
        var e = new GameEngine(c, rng);
        e.EnsureWorld();
        return (c, e);
    }

    /// <summary>Média de pontos de físico ganhos num treino de força, em muitas tentativas.</summary>
    private static double FisGain(int age, params string[] items)
    {
        var total = 0;
        const int n = 600;
        for (var seed = 1; seed <= n; seed++)
        {
            var (c, e) = Pro(seed, age, items);
            c.Phase = GamePhase.Event;
            c.Current = Events.Every.First(x => x.Id == "tfoco").ToEvent();
            var before = c.Attributes.Fis;
            e.Act(new GameAction(ActionType.Choose, 0)); // força e resistência
            total += c.Attributes.Fis - before;
        }
        return total / (double)n;
    }

    [Fact]
    public void Jovem_evolui_rapido_velho_devagar_e_os_equipamentos_ajudam_mais_os_velhos()
    {
        var young = FisGain(19);
        var old = FisGain(33);
        var oldEquipped = FisGain(33, "pesos", "estacao");
        var youngEquipped = FisGain(19, "pesos", "estacao");
        output.WriteLine($"19 anos: {young:0.00} (com academia {youngEquipped:0.00}) · 33 anos: {old:0.00} (com academia {oldEquipped:0.00})");
        Assert.True(young > old * 2, "o jovem evolui bem mais rápido");
        Assert.True(oldEquipped > old * 1.5, "a academia em casa ajuda muito o veterano");
        Assert.True(oldEquipped - old > youngEquipped - young, "o efeito é maior depois dos 30");
    }

    [Fact]
    public void Comprar_desconta_o_dinheiro_e_respeita_os_requisitos()
    {
        var (c, e) = Pro(1, 25);
        c.Money = 7;
        Assert.Throws<GameRuleException>(() => e.Act(new GameAction(ActionType.Buy, ClubId: "estacao"))); // precisa dos halteres
        e.Act(new GameAction(ActionType.Buy, ClubId: "pesos"));
        Assert.Equal(6, c.Money);
        Assert.Throws<GameRuleException>(() => e.Act(new GameAction(ActionType.Buy, ClubId: "pesos"))); // já tem
        e.Act(new GameAction(ActionType.Buy, ClubId: "estacao"));
        Assert.Equal(0, c.Money);
        Assert.Throws<GameRuleException>(() => e.Act(new GameAction(ActionType.Buy, ClubId: "esteira"))); // sem dinheiro
        var home = ViewBuilder.Build(c).Hud.Home!;
        Assert.True(home.Items.First(i => i.Id == "estacao").Owned);
        Assert.False(home.Items.First(i => i.Id == "esteira").Affordable);
    }

    [Fact]
    public void Camara_hiperbarica_e_chef_alongam_a_carreira()
    {
        foreach (var (items, active) in new[] { (Array.Empty<string>(), false), (new[] { "hiperbarica", "chef" }, true) })
        {
            var (c, e) = Pro(2, 37, items);
            c.Phase = GamePhase.SeasonSummary;
            c.Overall = 70;
            e.Act(new GameAction(ActionType.Continue)); // vira 38 anos
            Assert.Equal(active, c.Status == CareerStatus.Active);
        }
    }

    [Fact]
    public void Lesoes_novas_aparecem_e_algumas_podem_encerrar_a_carreira()
    {
        var titles = new HashSet<string>();
        var ending = 0;
        for (var seed = 1; seed <= 400; seed++)
        {
            var (c, e) = Pro(seed, 30);
            var ev = e.SeriousInjury();
            titles.Add(ev.Title);
            if (ev.Options.Any(o => o.Success?.End != null || o.Fail?.End != null)) ending++;
        }
        output.WriteLine(string.Join(", ", titles));
        Assert.True(titles.Count >= 9, $"só {titles.Count} tipos de lesão grave");
        Assert.Contains("Hérnia de disco", titles);
        Assert.Contains("Arritmia no coração", titles);
        Assert.Contains("Concussão", titles);
        Assert.InRange(ending, 20, 160);

        // a terceira concussão: os médicos recomendam parar
        var (cc, ce) = Pro(5, 28);
        cc.Concussions = 2;
        GameEvent? third = null;
        for (var i = 0; i < 300 && third == null; i++)
        {
            var x = ce.SeriousInjury();
            if (x.EventId == "lg_conc3") third = x;
            cc.Concussions = 2;
        }
        Assert.NotNull(third);
        Assert.Contains(third!.Options, o => o.Success?.End == "sau");
    }
}
