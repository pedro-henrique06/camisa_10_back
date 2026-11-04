using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>"Oferecer-se a um clube": o clube aceita, faz contraproposta ou recusa.</summary>
public class PitchTests(ITestOutputHelper output)
{
    /// <summary>Jogador no profissional, com o mercado aberto pelo botão "Procurar clube".</summary>
    private static (Career, GameEngine) InMarket(int seed, int overall, int age, int fame = 20, int titles = 0, string club = "Ceará")
    {
        var rng = new GameRandom(seed);
        var c = GameEngine.NewCareer("p", "Teste", club, "Brasil", "Atacante", rng);
        c.Base = false;
        c.Age = age;
        c.Season = 1;
        c.SeasonDay = 1;
        c.ClubId = World.ClubByName(club)!.Id;
        c.Club = club;
        c.Overall = overall;
        c.Fame = fame;
        c.Titles = titles;
        c.ReleaseClause = 0;
        var engine = new GameEngine(c, rng);
        engine.EnsureWorld();
        c.Phase = GamePhase.Market;
        c.MarketMode = "bt";
        c.ReturnPhase = GamePhase.Calendar;
        return (c, engine);
    }

    private static Dictionary<string, int> Verdicts(string target, int overall, int age, int fame = 20, int titles = 0, int ask = 1)
    {
        var count = new Dictionary<string, int> { ["accept"] = 0, ["counter"] = 0, ["reject"] = 0 };
        for (var seed = 1; seed <= 120; seed++)
        {
            var (c, e) = InMarket(seed, overall, age, fame, titles);
            e.Act(new GameAction(ActionType.Offer, ask, ClubId: World.ClubByName(target)!.Id));
            count[c.LastPitch!.Verdict]++;
        }
        return count;
    }

    private void Show(string label, Dictionary<string, int> v) =>
        output.WriteLine($"{label,-45} aceita {v["accept"],3} · contraproposta {v["counter"],3} · recusa {v["reject"],3}");

    [Fact]
    public void Clubes_grandes_sao_bem_mais_criticos_que_os_pequenos()
    {
        var smallMid = Verdicts("Juventude", 70, 24);
        var bigMid = Verdicts("Real Madrid", 70, 24);
        var bigGood = Verdicts("Real Madrid", 86, 25, fame: 50, titles: 3);
        var bigStar = Verdicts("Real Madrid", 92, 26, fame: 80, titles: 8);
        var bigOld = Verdicts("Real Madrid", 88, 34, fame: 70, titles: 6);
        var smallCheap = Verdicts("Juventude", 66, 24, ask: 0);
        var smallGreedy = Verdicts("Juventude", 66, 24, ask: 2);
        Show("Overall 70 → Juventude", smallMid);
        Show("Overall 70 → Real Madrid", bigMid);
        Show("Overall 86, 3 títulos → Real Madrid", bigGood);
        Show("Overall 92, craque → Real Madrid", bigStar);
        Show("Overall 88, 34 anos → Real Madrid", bigOld);
        Show("Overall 66 pedindo menos → Juventude", smallCheap);
        Show("Overall 66 pedindo mais → Juventude", smallGreedy);

        // um clube pequeno quase sempre quer um jogador do nível dele
        Assert.True(smallMid["reject"] < 25, "clube pequeno aceita um jogador no nível dele");
        // um gigante não quer um jogador mediano
        Assert.True(bigMid["reject"] >= 115, "gigante recusa jogador mediano");
        // mesmo um bom jogador ouve muito "não" e contraproposta de um gigante
        Assert.True(bigGood["accept"] < 60, "gigante é exigente até com jogador bom");
        Assert.True(bigGood["reject"] + bigGood["counter"] > 60);
        // o craque consegue
        Assert.True(bigStar["accept"] + bigStar["counter"] > 90, "craque convence o gigante");
        // veterano tem mais dificuldade
        Assert.True(bigOld["accept"] < bigStar["accept"]);
        // pedir menos ajuda, pedir mais atrapalha
        Assert.True(smallCheap["accept"] > smallGreedy["accept"]);
        Assert.True(smallGreedy["reject"] > smallCheap["reject"]);
    }

    [Fact]
    public void Aceite_e_contraproposta_viram_proposta_para_assinar_e_recusa_explica_o_motivo()
    {
        var seen = new HashSet<string>();
        for (var seed = 1; seed <= 200 && seen.Count < 3; seed++)
        {
            var (c, e) = InMarket(seed, 80, 26, fame: 40, titles: 1);
            var before = c.MarketOffers.Count;
            e.Act(new GameAction(ActionType.Offer, 1, ClubId: World.ClubByName("Palmeiras")!.Id));
            var p = c.LastPitch!;
            seen.Add(p.Verdict);
            Assert.False(string.IsNullOrWhiteSpace(p.Text));
            var view = ViewBuilder.Build(c).Market!;
            Assert.Equal(p.Verdict, view.Pitch!.Verdict);
            Assert.Equal(GameEngine.PitchesPerWindow - 1, view.PitchesLeft);
            if (p.Verdict == "reject")
            {
                Assert.Equal(before, c.MarketOffers.Count);
                Assert.Contains("recusa", p.Text);
                continue;
            }
            var offer = c.MarketOffers[0];
            Assert.True(offer.Pitched);
            Assert.Equal("Palmeiras", offer.Club);
            Assert.True(view.Offers[0].Pitched);
            if (p.Verdict == "counter")
            {
                Assert.Equal(0, offer.SigningBonus);
                Assert.True(offer.Contract < 3);
            }
            // assina: vai para o clube com o contrato combinado
            e.Act(new GameAction(ActionType.Choose, 0));
            Assert.Equal("Palmeiras", c.Club);
            Assert.Equal(offer.Contract, c.Contract);
        }
        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void Nao_da_para_insistir_no_mesmo_clube_nem_procurar_mais_de_tres_por_janela()
    {
        var (c, e) = InMarket(5, 75, 25);
        var id = World.ClubByName("Juventude")!.Id;
        e.Act(new GameAction(ActionType.Offer, 1, ClubId: id));
        Assert.Throws<GameRuleException>(() => e.Act(new GameAction(ActionType.Offer, 1, ClubId: id)));
        e.Act(new GameAction(ActionType.Offer, 1, ClubId: World.ClubByName("Sport")!.Id));
        e.Act(new GameAction(ActionType.Offer, 1, ClubId: World.ClubByName("Vitória")!.Id));
        Assert.Equal(0, e.PitchesLeft());
        Assert.Throws<GameRuleException>(() => e.Act(new GameAction(ActionType.Offer, 1, ClubId: World.ClubByName("Bahia")!.Id)));
        // o próprio clube não dá
        var (c2, e2) = InMarket(6, 75, 25);
        Assert.Throws<GameRuleException>(() => e2.Act(new GameAction(ActionType.Offer, 1, ClubId: c2.ClubId)));
        // fora do mercado também não
        c2.Phase = GamePhase.Calendar;
        Assert.Throws<GameRuleException>(() => e2.Act(new GameAction(ActionType.Offer, 1, ClubId: id)));
    }
}
