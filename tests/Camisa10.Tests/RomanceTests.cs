using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Vida amorosa (ficar, namorar, noivar, casar, separar) e as revistas de fofoca.</summary>
public class RomanceTests(ITestOutputHelper output)
{
    private static (Career, GameEngine) New(int seed)
    {
        var rng = new GameRandom(seed);
        var c = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", rng);
        c.Base = false;
        c.Age = 24;
        c.Season = 3;
        c.ClubId = World.ClubByName("Flamengo")!.Id;
        c.Club = "Flamengo";
        c.Fame = 40;
        c.Money = 100;
        var e = new GameEngine(c, rng);
        e.EnsureWorld();
        return (c, e);
    }

    /// <summary>Mostra o evento e escolhe a opção.</summary>
    private static void Choose(Career c, GameEngine e, string id, int option)
    {
        c.Phase = GamePhase.Event;
        c.Current = Events.Every.First(x => x.Id == id).ToEvent();
        e.Act(new GameAction(ActionType.Choose, option));
    }

    [Fact]
    public void Progressao_do_ficando_ao_casamento_fica_registrada()
    {
        var (c, e) = New(3);
        c.Love.Partner = "Júlia";
        c.Love.PartnerJob = "arquiteta";
        Choose(c, e, "r_conhece", 1); // amigo apresenta: sempre fica
        Assert.Equal("ficando", c.Love.Stage);
        Choose(c, e, "r_namoro", 1); // assume nas redes: sempre namora
        Assert.Equal("namorando", c.Love.Stage);
        c.Love.Affection = 90;
        c.Season++;
        Choose(c, e, "r_pedido", 1);
        if (c.Love.Stage != "noivo") c.Love.Stage = "noivo"; // 10% de chance de pedir mais tempo
        Choose(c, e, "r_casamento", 1);
        Assert.Equal("casado", c.Love.Stage);
        Assert.True(c.Has("casou"));

        var icons = c.Love.Milestones.Select(m => m.Icon).ToList();
        Assert.Contains("✨", icons);
        Assert.Contains("❤️", icons);
        Assert.Contains("💒", icons);
        Assert.Contains(c.Love.Milestones, m => m.Text.Contains("Júlia"));
        // famoso, cada passo vira notícia nas revistas
        Assert.Contains(c.Gossip, g => g.Headline.Contains("namoro com Júlia") || g.Headline.Contains("confirma romance"));
        Assert.Contains(c.Gossip, g => g.Headline.Contains("se casam") || g.Headline.Contains("Casamento de Teste"));
        Assert.All(c.Gossip, g => Assert.DoesNotContain("{", g.Headline));

        var love = ViewBuilder.Build(c).Hud.Love!;
        Assert.Equal("Casado", love.StageLabel);
        Assert.Equal("Júlia", love.Partner);
        Assert.NotEmpty(ViewBuilder.Build(c).Hud.Gossip!);
    }

    [Fact]
    public void Traicao_flagrada_vira_escandalo_nas_revistas()
    {
        var caught = 0;
        for (var seed = 1; seed <= 30; seed++)
        {
            var (c, e) = New(seed);
            c.Love.Stage = "namorando";
            c.Love.Partner = "Bianca";
            c.Love.Affection = 70;
            Choose(c, e, "r_tentacao", 2); // trair
            Assert.Equal(1, c.Love.Cheats);
            Assert.True(c.Has("traiu"));
            if (c.Gossip.Any(g => g.Kind == "escandalo" && g.Headline.StartsWith("FLAGRA")))
            {
                caught++;
                Assert.True(c.Love.Affection <= 30, "o flagra derruba o afeto");
                Assert.Contains(c.Scheduled, x => x.EventId == "r_flagra");
            }
            else Assert.Contains(c.Scheduled, x => x.EventId == "r_culpa");
        }
        output.WriteLine($"flagrado em {caught} de 30");
        Assert.InRange(caught, 5, 25);
    }

    [Fact]
    public void Escandalo_sai_mesmo_sem_fama_e_romance_comum_nao()
    {
        var (c, e) = New(5);
        c.Fame = 5;
        c.Love.Partner = "Luana";
        Choose(c, e, "r_conhece", 1);
        Choose(c, e, "r_namoro", 1);
        Assert.Empty(c.Gossip);
        Choose(c, e, "r_termino", 1); // aceitar o fim
        Assert.Equal("separado", c.Love.Stage);
        Assert.Contains(c.Gossip, g => g.Kind == "escandalo");
    }

    [Fact]
    public void Divorcio_leva_30_por_cento_do_patrimonio()
    {
        var (c, e) = New(9);
        c.Love.Stage = "casado";
        c.Love.Partner = "Helena";
        c.Love.Affection = 60;
        c.Money = 100;
        Choose(c, e, "r_flagra", 2); // assumir e terminar
        Assert.Equal("separado", c.Love.Stage);
        Assert.InRange(c.Money, 69, 71);
        Assert.Contains(c.Love.Milestones, m => m.Icon == "💔" && m.Text.Contains("Divórcio"));
    }

    [Fact]
    public void Afeto_baixo_marca_o_pedido_de_termino()
    {
        var (c, e) = New(11);
        c.Love.Stage = "namorando";
        c.Love.Partner = "Sofia";
        c.Love.Affection = 25;
        Choose(c, e, "r_aniv", 2); // esquecer a data
        Assert.True(c.Love.Affection < 20);
        Assert.Contains(c.Scheduled, x => x.EventId == "r_termino");
    }

    [Fact]
    public void Filhos_sem_limite_no_maximo_um_parto_por_temporada()
    {
        var (c, e) = New(21);
        c.Love.Stage = "casado";
        c.Love.Partner = "Clara";
        c.Love.Affection = 90;
        var plan = Events.Every.First(x => x.Id == "r_bebe_plano");
        for (var season = 4; season <= 10; season++)
        {
            c.Season = season;
            c.Love.Affection = 90;
            var tries = 0;
            while (!c.Love.Expecting)
            {
                Assert.True(plan.Condition(c));
                Assert.True(++tries < 30);
                Choose(c, e, "r_bebe_plano", 0);
            }
            Assert.False(plan.Condition(c), "grávida não planeja outro");
            Choose(c, e, "r_gravidez", 1);
            Choose(c, e, "r_nasceu", 0);
            Assert.Contains(c.Scheduled, x => x.EventId == "r_nome");
            c.Scheduled.Clear();
            Choose(c, e, "r_nome", 0);
            Assert.False(c.Love.Expecting);
            Assert.False(plan.Condition(c), "um parto por temporada");
        }
        c.Season = 11;
        Choose(c, e, "r_bebe_plano", 1); // e ainda adotar mais um
        Choose(c, e, "r_nome", 3);
        output.WriteLine(string.Join(", ", c.Love.Children.Select(k => $"{k.Name}{(k.Adopted ? " (adotado)" : "")}")));
        Assert.True(c.Love.Children.Count >= 8, "sete partos e uma adoção, sem limite");
        Assert.Equal(c.Love.Children.Count, c.Love.Children.Select(k => k.Name).Distinct().Count());
        Assert.All(c.Love.Children, k => Assert.Equal("Clara", k.Mother));
        Assert.True(c.Has("pai"));
        Assert.Contains(c.Love.Milestones, m => m.Icon == "👶" && !m.Text.Contains("{"));
        Assert.Contains(c.Gossip, g => g.Headline.StartsWith("Nasceu!") || g.Headline.Contains("corre para o estádio"));
        var view = ViewBuilder.Build(c).Hud.Love!;
        Assert.Equal(c.Love.Children.Count, view.Children!.Count);
    }

    [Fact]
    public void Carreiras_completas_tem_vida_amorosa()
    {
        var married = 0;
        var withRomance = 0;
        var namings = 0;
        var kidEvents = new HashSet<string>();
        for (var seed = 1; seed <= 10; seed++)
        {
            GameEvent? last = null;
            var (c, _) = GameSimulationTests.PlayToEnd(seed, "Atacante", "Flamengo", s =>
            {
                if (s.Phase != GamePhase.Event || s.Current == null || ReferenceEquals(s.Current, last)) return;
                last = s.Current;
                if (s.Current.EventId == "r_nome")
                {
                    namings++;
                    var labels = s.Current.Options.Select(o => o.Label).ToList();
                    Assert.Equal(4, labels.Distinct().Count());
                    Assert.All(labels, l => Assert.DoesNotContain("{", l));
                }
                if (s.Current.EventId?.StartsWith("r_filho_") == true)
                {
                    kidEvents.Add(s.Current.EventId);
                    Assert.DoesNotContain("{", s.Current.Title + s.Current.Text);
                    Assert.Contains(s.Love.Children, k => (s.Current.Title + s.Current.Text).Contains(k.Name));
                }
            });
            if (c.Love.Milestones.Count > 0) withRomance++;
            if (c.Love.Milestones.Any(m => m.Icon == "💒")) married++;
            output.WriteLine($"carreira {seed}: {c.Love.Stage}, {c.Love.Children.Count} filhos, {c.Love.Milestones.Count} marcos, {c.Gossip.Count} fofocas, traições {c.Love.Cheats} · " +
                string.Join(" → ", c.Love.Milestones.Select(m => $"{m.Icon}{m.Age}")));
        }
        Assert.True(withRomance >= 8, "quase toda carreira tem alguma história de amor");
        Assert.True(married >= 2, "alguns jogadores chegam a casar");
        output.WriteLine($"escolhas de nome: {namings}, eventos dos filhos: {string.Join(", ", kidEvents)}");
        Assert.True(namings >= 5, "o jogador escolhe o nome dos filhos");
        Assert.True(kidEvents.Count >= 3, "os filhos crescem e trazem decisões");
    }
}
