using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.Views;
using Camisa10.Domain.World;
using Xunit.Abstractions;

namespace Camisa10.Tests;

/// <summary>Cada carreira é diferente: origem, reputação, arcos com consequências e epílogo.</summary>
public class StoryTests(ITestOutputHelper output)
{
    private static (Career, GameEngine) Pro(int seed)
    {
        var rng = new GameRandom(seed);
        var c = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", rng);
        c.Base = false;
        c.Age = 24;
        c.Season = 3;
        c.ClubId = World.ClubByName("Flamengo")!.Id;
        c.Club = "Flamengo";
        c.Queue.Clear();
        c.Phase = GamePhase.Calendar;
        c.Current = null;
        var e = new GameEngine(c, rng);
        e.EnsureWorld();
        return (c, e);
    }

    private static void Choose(Career c, GameEngine e, string id, int option)
    {
        c.Phase = GamePhase.Event;
        c.Current = Events.Every.First(x => x.Id == id).ToEvent();
        e.Act(new GameAction(ActionType.Choose, option));
    }

    [Fact]
    public void Origem_sorteada_abre_a_carreira()
    {
        var origins = new HashSet<string>();
        for (var seed = 1; seed <= 80; seed++)
        {
            var c = GameEngine.NewCareer("p", "Teste", "Flamengo", "Brasil", "Atacante", new GameRandom(seed));
            origins.Add(c.Origin!);
            Assert.Equal("o_" + c.Origin, c.Current!.EventId);
            Assert.Equal(GamePhase.Event, c.Phase);
        }
        Assert.Equal(Story.Origins.Count, origins.Count);
        var rich = Story.Find("rico")!;
        var career = new Career();
        rich.Setup(career);
        Assert.Equal(25, career.Money);
    }

    [Fact]
    public void Reputacao_vira_rotulos_e_muda_as_propostas()
    {
        var (c, _) = Pro(1);
        c.Rep = new Reputation();
        var neutral = Story.SalaryFactor(c);
        Story.Apply(c.Rep, "carater:7,disciplina:6,solidario:-5");
        var tags = Story.Tags(c).Select(t => t.Label).ToList();
        Assert.Contains("Exemplo de caráter", tags);
        Assert.Contains("Profissional exemplar", tags);
        Assert.Contains("Mão de vaca", tags);
        Assert.True(Story.SalaryFactor(c) > neutral);
        Story.Apply(c.Rep, "polemica:10,disciplina:-14");
        Assert.True(Story.BigClubsWary(c));
        Assert.True(Story.SalaryFactor(c) < neutral);
        Assert.Contains("Polêmico", Story.Tags(c).Select(t => t.Label));
    }

    [Fact]
    public void Aposta_aceita_volta_meses_depois_e_pode_banir()
    {
        var banned = 0;
        for (var seed = 1; seed <= 20; seed++)
        {
            var (c, e) = Pro(seed);
            c.Money = 5;
            var honesty = c.Rep.Character;
            Choose(c, e, "a_aposta1", 0);
            Assert.Contains(c.Scheduled, x => x.EventId == "a_aposta2" && x.At >= c.Day + 80);
            Assert.Equal(honesty - 5, c.Rep.Character);
            c.Scheduled.Clear();
            Choose(c, e, "a_aposta2", 1); // negar
            if (c.EndReason == "ban")
            {
                for (var k = 0; k < 5 && c.Status == CareerStatus.Active; k++) e.Act(new GameAction(ActionType.Continue));
                Assert.Equal(CareerStatus.Finished, c.Status);
            }
            if (c.Status == CareerStatus.Finished)
            {
                banned++;
                Assert.Equal("Banido do futebol", c.FinalTitle);
                Assert.Contains(c.Epilogue, l => l.Contains("Banido"));
            }
        }
        output.WriteLine($"banidos {banned} de 20");
        Assert.InRange(banned, 4, 18);
    }

    [Fact]
    public void Amigo_de_infancia_tem_destinos_diferentes()
    {
        var (c, e) = Pro(3);
        c.Money = 5;
        Choose(c, e, "a_amigo1", 2); // se afastar
        Assert.Contains(c.Scheduled, x => x.EventId == "a_amigo_preso");
        Assert.NotNull(c.Buddy ?? "x");
        var (c2, e2) = Pro(4);
        c2.Money = 5;
        Choose(c2, e2, "a_amigo1", 0); // ajudar
        Assert.Contains(c2.Scheduled, x => x.EventId == "a_amigo2");
        Assert.True(c2.Rep.Charity >= 3);
    }

    [Fact]
    public void Trocar_de_clube_toda_hora_vira_mercenario()
    {
        var (c, _) = Pro(5);
        c.Rep = new Reputation { Loyalty = -5 };
        Assert.Contains("Mercenário", Story.Tags(c).Select(t => t.Label));
        Assert.True(Events.Every.First(x => x.Id == "p_mercenario").Condition(c));
    }

    private static bool OffField(string id) => GameData.Personal.Contains(id) || Events.LifeIds.Contains(id) || Events.RomanceIds.Contains(id)
        || Events.BusinessIds.Contains(id) || Events.StoryIds.Contains(id);

    [Fact]
    public void Cada_carreira_tem_uma_historia_diferente()
    {
        var sets = new List<HashSet<string>>();
        var epilogues = new HashSet<string>();
        var tags = new HashSet<string>();
        var origins = new HashSet<string>();
        var arcs = new HashSet<string>();
        var reps = Enumerable.Range(0, 6).Select(_ => new List<int>()).ToList();
        for (var seed = 1; seed <= 16; seed++)
        {
            var seen = new HashSet<string>();
            GameEvent? last = null;
            var (c, _) = GameSimulationTests.PlayToEnd(seed, "Atacante", "Flamengo", s =>
            {
                if (s.Phase == GamePhase.Event && s.Current?.EventId is { } id && !ReferenceEquals(s.Current, last))
                {
                    last = s.Current;
                    if (OffField(id)) seen.Add(id);
                    Assert.DoesNotContain("{", s.Current.Title + s.Current.Text + string.Join("", s.Current.Options.Select(o => o.Label)));
                }
            });
            sets.Add(seen);
            origins.Add(c.Origin!);
            var r = c.Rep;
            foreach (var (axis, v) in new[] { r.Character, r.Loyalty, r.Controversy, r.Charity, r.Ambition, r.Discipline }.Select((v, i) => (i, v))) reps[axis].Add(v);
            foreach (var l in c.Epilogue) epilogues.Add(l);
            foreach (var t in Story.Tags(c)) tags.Add(t.Label);
            foreach (var id in seen.Where(i => i.StartsWith("a_") || i.StartsWith("p_"))) arcs.Add(id);
            Assert.NotEmpty(c.Epilogue);
            Assert.NotNull(ViewBuilder.Build(c).Hud.Story);
            output.WriteLine($"carreira {seed}: {Story.Find(c.Origin)!.Name}, {seen.Count} eventos, {c.FinalTitle} · {string.Join(", ", Story.Tags(c).Select(t => t.Label))}");
            output.WriteLine("   " + string.Join(" / ", c.Epilogue));
        }
        var sims = new List<double>();
        for (var i = 0; i < sets.Count; i++)
            for (var j = i + 1; j < sets.Count; j++)
                sims.Add(sets[i].Intersect(sets[j]).Count() / (double)sets[i].Union(sets[j]).Count());
        output.WriteLine($"eventos fora de campo diferentes vistos: {sets.SelectMany(x => x).Distinct().Count()}");
        output.WriteLine($"parecença média entre duas carreiras: {sims.Average():P0}; origens {origins.Count}, rótulos {tags.Count}, frases de epílogo {epilogues.Count}, arcos e eventos de reputação vistos {arcs.Count}");
        Assert.True(sims.Average() < .4, "duas carreiras não podem ser quase iguais");
        Assert.True(origins.Count >= 6);
        // reputações diferentes: o teste escolhe ao acaso, então medimos se os eixos variam de uma carreira para outra
        var spreads = reps.Select(axis => axis.Max() - axis.Min()).ToList();
        output.WriteLine("variação de cada eixo entre as carreiras: " + string.Join(", ", spreads));
        Assert.True(spreads.Count(x => x >= 5) >= 4, "a reputação muda de carreira para carreira");
        Assert.True(epilogues.Count >= 25, "finais diferentes");
        Assert.True(arcs.Count >= 20, "os arcos aparecem");
    }
}
