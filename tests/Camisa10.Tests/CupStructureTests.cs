using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;

namespace Camisa10.Tests;

public class CupStructureTests
{
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
    public void Paulistao_tem_grupo_de_quatro_paulistas_e_copa_do_brasil_tem_a_chave_inteira()
    {
        var (s, _) = Pro("Palmeiras");
        var state = s.Competitions.Single(c => c.Kind == "state");
        Assert.Equal(4, state.Group.Count);
        Assert.Equal("Palmeiras", state.Group[0].Club);
        Assert.All(state.Group, r => Assert.Equal("BR", World.Club(r.ClubId)!.CountryIso));
        Assert.Equal(2, state.KoRounds); // semifinal e final

        var cup = s.Competitions.Single(c => c.Kind == "cup");
        Assert.Empty(cup.Group);
        var first = cup.Bracket.Single();
        Assert.Equal("Terceira fase", first.Name);
        var teams = first.Ties.SelectMany(t => new[] { t.HomeId, t.AwayId }).Where(x => x != null).ToList();
        Assert.Equal(teams.Count, teams.Distinct().Count());
        Assert.Single(first.Ties, t => t.Mine);
        Assert.NotNull(first.Ties.Single(t => t.Mine).AwayId); // o jogador nunca pega folga

        var view = ViewBuilder.Build(s).Calendar!.Cups;
        var cv = view.Single(c => c.Kind == "cup");
        Assert.Equal(5, cv.Bracket.Count); // terceira fase, oitavas, quartas, semi e final
        Assert.Equal("Final", cv.Bracket[^1].Name);
        Assert.Single(cv.Bracket[^1].Ties);
        Assert.True(cv.Bracket[0].Current);
        // antes da fase de grupos, a chave da Libertadores já mostra as fases (8, 4, 2 e 1 confrontos)
        var lib = view.Single(c => c.Kind == "continental");
        Assert.Equal(new[] { 8, 4, 2, 1 }, lib.Bracket.Select(b => b.Ties.Count));
    }

    [Fact]
    public void Em_toda_carreira_grupos_e_chaves_fecham_com_os_jogos_do_jogador()
    {
        var checkedCups = 0;
        var nationals = 0;
        for (var seed = 1; seed <= 4; seed++)
        {
            GameSimulationTests.PlayToEnd(seed, "Atacante", "Palmeiras", s =>
            {
                // no jogo de copa ao vivo, o adversário é o do grupo ou da chave
                if (s.Phase == GamePhase.Event && s.Current is { Focus: "cup", CompetitionId: { } kind } && s.Match is { Final: false } m)
                {
                    var c = s.Competitions.Single(x => x.Kind == kind);
                    var opp = c.Played < c.GroupGames
                        ? c.Group.Select(r => r.Club).Skip(1)
                        : c.Bracket[c.Played - c.GroupGames].Ties.Where(t => t.Mine).SelectMany(t => new[] { t.Home, t.Away! });
                    Assert.Contains(m.Opponent, opp);
                }
                foreach (var c in s.Competitions.Where(x => x.Kind != "league"))
                {
                    Assert.All(c.Group, r => Assert.Equal(r.Won * 3 + r.Drawn, r.Points));
                    Assert.Equal(c.Group.Sum(r => r.GoalsFor), c.Group.Sum(r => r.GoalsAgainst));
                    for (var k = 1; k < c.Bracket.Count; k++)
                    {
                        var winners = c.Bracket[k - 1].Ties.Select(t => t.WinnerId).ToHashSet();
                        Assert.All(c.Bracket[k].Ties, t => Assert.Contains(t.HomeId, winners));
                    }
                    if (c.Out || c.Champion)
                    {
                        // a chave termina com um campeão
                        Assert.Equal(c.KoRounds, c.Bracket.Count);
                        Assert.NotNull(c.Bracket[^1].Ties.Single().WinnerId);
                        if (c.Champion) Assert.True(c.Bracket[^1].Ties.Single().Mine);
                        checkedCups++;
                        if (c.Kind == "national") nationals++;
                    }
                }
            });
        }
        Assert.True(checkedCups > 20, $"copas fechadas: {checkedCups}");
        Assert.True(nationals > 0, "torneio da seleção com grupo e chave");
    }

    [Fact]
    public void Quem_fica_fora_dos_dois_primeiros_do_grupo_e_eliminado()
    {
        var (s, e) = Pro("Palmeiras");
        var lib = s.Competitions.Single(c => c.Kind == "continental");
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
        Assert.Equal(3, lib.Group.Single(r => r.ClubId == s.ClubId).Lost);
        Assert.Equal(lib.KoRounds, lib.Bracket.Count);
        Assert.DoesNotContain(lib.Bracket[0].Ties, t => t.Mine);
    }
}
