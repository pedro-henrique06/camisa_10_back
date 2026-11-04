using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;

namespace Camisa10.Tests;

public class DuelTests
{
    private static Duel NewDuel(int seed = 1)
    {
        var a = new Attributes { Chu = 80, Dri = 70, Pas = 60, Def = 40, Fis = 60, Rit = 70 };
        var b = new Attributes { Chu = 50, Dri = 50, Pas = 50, Def = 75, Fis = 70, Rit = 60 };
        var d = new Duel { Id = "d1", Home = new DuelSide { PlayerId = "pa", Name = "Ana", Club = "Palmeiras", Attributes = a },
            Away = new DuelSide { PlayerId = "pb", Name = "Beto", Club = "Corinthians", Attributes = b } };
        DuelEngine.Setup(d, new GameRandom(seed));
        return d;
    }

    [Fact]
    public void Seis_lances_alternando_quem_ataca_e_placar_bate_com_os_gols()
    {
        var d = NewDuel();
        var e = new DuelEngine(d, new GameRandom(7));
        Assert.Equal(6, d.Moments.Count);
        Assert.Equal(new[] { "home", "away", "home", "away", "home", "away" }, d.Moments.Select(m => m.Attacker));
        for (var i = 0; i < 6; i++)
        {
            var atk = d.Moments[i].Attacker;
            e.Choose(atk, i % 3);
            Assert.False(d.Moments[i].Resolved); // só resolve com as duas escolhas
            e.Choose(atk == "home" ? "away" : "home", (i + 1) % 3);
            Assert.True(d.Moments[i].Resolved);
        }
        Assert.True(d.Finished);
        Assert.Equal(d.Moments.Count(m => m.Goal && m.Attacker == "home"), d.Home.Goals);
        Assert.Equal(d.Moments.Count(m => m.Goal && m.Attacker == "away"), d.Away.Goals);
        Assert.Equal(d.Home.Goals, d.Home.MyGoals + d.Home.MyAssists);
        Assert.Throws<GameRuleException>(() => e.Choose("home", 0));
        var o = e.OutcomeFor("away");
        Assert.Equal((d.Away.Goals, d.Home.Goals), (o.For, o.Against));
    }

    [Fact]
    public void Defensor_que_adivinha_ganha_bonus_e_nao_escolhe_duas_vezes()
    {
        var d = NewDuel();
        var e = new DuelEngine(d, new GameRandom(3));
        e.Choose("home", 0);                 // chute
        Assert.Throws<GameRuleException>(() => e.Choose("home", 1));
        e.Choose("away", 0);                 // fecha o ângulo: leu o chute
        var m = d.Moments[0];
        Assert.True(m.Read);
        Assert.Equal(m.DefenseRoll + (75 - 50) / 5 + 4, m.DefenseTotal);
        Assert.Equal(m.AttackRoll + 6, m.AttackTotal); // chute 80 → +6, sem bônus de engano
    }

    [Fact]
    public void Decidir_pelo_amigo_so_depois_do_tempo_e_de_escolher()
    {
        var d = NewDuel();
        var e = new DuelEngine(d, new GameRandom(5));
        Assert.Throws<GameRuleException>(() => e.AutoPick("home", DateTime.UtcNow.AddMinutes(5))); // ainda não escolheu
        e.Choose("home", 2);
        Assert.Throws<GameRuleException>(() => e.AutoPick("home", d.MomentStartedAt.AddSeconds(10)));
        e.AutoPick("home", d.MomentStartedAt.AddSeconds(DuelEngine.AutoPickSeconds + 1));
        Assert.True(d.Moments[0].Resolved);
        Assert.Equal("away", d.Moments[0].AutoPicked);
        var view = DuelViews.Build(d, "away", DateTime.UtcNow);
        Assert.Equal("attack", view.Turn!.Role); // o lance 2 é do visitante
        Assert.Single(view.History);
    }

    [Fact]
    public void Classico_contra_amigo_vira_duelo_e_o_placar_entra_na_tabela()
    {
        // duas carreiras no mesmo mundo, no profissional, em clubes da mesma liga
        var idA = World.ClubByName("Palmeiras")!.Id;
        var idB = World.ClubByName("Corinthians")!.Id;
        Career Pro(string club, string id, string friendCareer, string friendClub)
        {
            var c = GameEngine.NewCareer(id, id, club, "Brasil", "Atacante", new GameRandom(1));
            c.WorldSeed = 99; c.MesaId = "m1"; c.Base = false; c.Age = 22; c.Season = 1; c.SeasonDay = 0;
            c.ClubId = World.ClubByName(club)!.Id; c.Club = club; c.LeagueName = null; c.Competitions.Clear();
            // o sorteio da temporada já sabe do amigo na mesma liga
            new GameEngine(c) { Mesa = new MesaContext("m1", new[] { new MesaFriend(friendCareer, "x", "Amigo", friendClub, null, "") }) }.EnsureWorld();
            return c;
        }
        var a = Pro("Palmeiras", "playerA1", "careerB", idB);
        var b = Pro("Corinthians", "playerB1", "careerA", idA);
        Assert.Equal(GameEngine.TableHash(a), GameEngine.TableHash(b));
        var ctx = new MesaContext("m1", new[] { new MesaFriend(b.Id, b.PlayerId, "Beto", b.ClubId, b.LeagueTableKey, GameEngine.TableHash(b)) });
        var e = new GameEngine(a, new GameRandom(4)) { Mesa = ctx };
        var picker = new Random(2);
        for (var i = 0; i < 5000 && a.Phase != GamePhase.Duel && a.Season == 1; i++)
        {
            var act = a.Phase switch
            {
                GamePhase.Event => new GameAction(ActionType.Choose, picker.Next(a.Current!.Options.Count)),
                GamePhase.Penalty => new GameAction(ActionType.Choose, 3, 75),
                GamePhase.Market => new GameAction(ActionType.Stay),
                GamePhase.Swap => new GameAction(ActionType.Choose, 1),
                _ => new GameAction(ActionType.Continue)
            };
            e.Act(act);
        }
        Assert.Equal(GamePhase.Duel, a.Phase);
        // o clássico da mesa cai no meio da temporada (rodada 7 ÷ 2 = 3, a 4ª)
        Assert.Equal(3, a.PendingDuel!.Round);
        var p = a.PendingDuel!;
        Assert.Equal("Corinthians", p.OpponentClub);
        Assert.Equal(GameEngine.DuelId(GameEngine.DuelKey("m1", a.Season, a.SeasonDay, a.Id, b.Id)), p.DuelId);
        Assert.NotNull(ViewBuilder.Build(a).Duel);
        var played = a.LeagueTable.First(r => r.ClubId == a.ClubId).Played;

        e.FinishDuel(new DuelOutcome(p.DuelId!, "league", p.Competition, p.Round, p.Home, "Beto", "Corinthians", 3, 1, 2, 1, 4, 2));
        Assert.Equal(GamePhase.Result, a.Phase);
        var row = a.LeagueTable.First(r => r.ClubId == a.ClubId);
        Assert.Equal(played + 1, row.Played);
        Assert.Equal(3, a.SeasonMatches[^1].For);
        Assert.Equal(2, a.SeasonMatches[^1].Goals);
        Assert.Null(a.PendingDuel);
        e.Act(new GameAction(ActionType.Continue)); // segue o jogo normalmente
    }
}
