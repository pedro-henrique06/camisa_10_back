using Camisa10.Domain.Game;
using Camisa10.Domain.Model;

namespace Camisa10.Tests;

public class PenaltyTests
{
    private static Career PenaltyCareer(int seed)
    {
        var c = GameEngine.NewCareer("p", "X", "Flamengo", "Brasil", "Atacante", new GameRandom(seed));
        c.Current = new GameEvent { EventId = "pen", Title = "Pênalti!", IsPenalty = true, IsMatch = true };
        c.Phase = GamePhase.Penalty;
        return c;
    }

    private static List<PenaltyShot> Shots(int zone, int power, int n = 600)
    {
        var list = new List<PenaltyShot>();
        for (var seed = 0; seed < n; seed++)
        {
            var c = PenaltyCareer(seed);
            new GameEngine(c, new GameRandom(seed + 1000)).Act(new GameAction(ActionType.Choose, zone, power));
            Assert.Equal(GamePhase.Result, c.Phase);
            var r = c.LastResult!;
            Assert.NotNull(r.Penalty);
            Assert.Equal(r.Success, r.Penalty!.Outcome == "goal");
            list.Add(r.Penalty);
        }
        return list;
    }

    [Theory]
    [InlineData(0, 72)]
    [InlineData(4, 30)]
    [InlineData(2, 100)]
    [InlineData(5, 95)]
    public void Desfecho_e_goleiro_sao_coerentes(int zone, int power)
    {
        foreach (var s in Shots(zone, power, 200))
        {
            Assert.Equal(zone, s.Zone);
            Assert.Equal(power, s.Power);
            Assert.InRange(s.KeeperZone, 0, 5);
            if (s.Outcome == "save") Assert.Equal(zone, s.KeeperZone);
            if (s.Outcome == "goal") Assert.NotEqual(zone % 3, s.KeeperZone % 3);
            if (s.Outcome == "out") Assert.True(power > GameEngine.PenaltyPowerMax);
        }
    }

    [Fact]
    public void Forca_ideal_converte_mais_que_fraca_ou_forte_demais()
    {
        double Rate(int zone, int power) => Shots(zone, power).Count(s => s.Outcome == "goal") / 600.0;
        var ideal = Rate(0, 72);
        Assert.True(ideal > Rate(0, 20) + .15, "fraca demais deve converter bem menos");
        Assert.True(ideal > Rate(0, 100) + .15, "forte demais no canto alto deve converter bem menos");
        Assert.Contains(Shots(0, 100), s => s.Outcome == "out");
        Assert.DoesNotContain(Shots(0, 72), s => s.Outcome == "out");
    }

    [Fact]
    public void Sem_forca_usa_a_ideal()
    {
        var c = PenaltyCareer(1);
        new GameEngine(c, new GameRandom(2)).Act(new GameAction(ActionType.Choose, 3));
        Assert.Equal(72, c.LastResult!.Penalty!.Power);
    }
}
